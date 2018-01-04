using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordWallet.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordWallet.Core
{
    public class DiscordBot
    {
        public static readonly Emoji REACTION_DENIED   = new Emoji("\U0001f6ab"); // U+1F6AB is :no_entry_sign:
        public static readonly Emoji REACTION_ERROR    = new Emoji("\u26a0");     // U+26A0  is :warning:
        public static readonly Emoji REACTION_PROGRESS = new Emoji("\u23f3");     // U+23F3  is :hourglass_flowing_sand:
        public static readonly Emoji REACTION_UNKNOWN  = new Emoji("\u2753");     // U+2753  is :question:

        public bool Initialized => ServiceProvider != null;

        private Task CommandTask;
        private CancellationTokenSource TokenSource;
        private IServiceProvider ServiceProvider;
        private Dictionary<string, CommandService> CommandList = new Dictionary<string, CommandService>();
        private BlockingCollection<(CommandService service, ICommandContext context, string command)> CommandQueue = new BlockingCollection<(CommandService service, ICommandContext context, string command)>();

        private string Token { get; }
        private Logger Logger { get; }
        private Permission Permission { get; } = null;
        private IServiceCollection ServiceCollection { get; } = new ServiceCollection();
        private DiscordSocketClient Discord { get; } = new DiscordSocketClient(new DiscordSocketConfig()
        {
            AlwaysDownloadUsers = true,
            DefaultRetryMode = RetryMode.AlwaysRetry,
        });
        
        public DiscordBot(string token, string permissionUri = null)
        {
            Token = token;
            Logger = new Logger(Discord);

            AddService(Discord);
            AddService(Logger);

            if (!String.IsNullOrEmpty(permissionUri))
            {
                Logger.WriteLine("DiscordBot: Permission Service: Loading...");

                Permission = new Permission(new Uri(permissionUri));
                AddService(Permission);
            }

            Discord.MessageReceived += OnMessageReceived;
        }

        public DiscordBot AddService<T>() where T : class
        {
            if (Initialized)
            {
                throw new InvalidOperationException();
            }

            ServiceCollection.AddSingleton<T>();

            return this;
        }

        public DiscordBot AddService<T>(T service) where T : class
        {
            if (Initialized)
            {
                throw new InvalidOperationException();
            }

            ServiceCollection.AddSingleton(service);

            return this;
        }
        
        public CommandService AddCommand(string prefix)
        {
            if (!CommandList.TryGetValue(prefix, out var command))
            {
                CommandList.Add(prefix, new CommandService());
            }
            
            return CommandList[prefix];
        }

        public async Task Start()
        {
            if (Discord.ConnectionState == ConnectionState.Connected || Discord.ConnectionState == ConnectionState.Connecting)
            {
                throw new InvalidOperationException();
            }

            if (!Initialized)
            {
                ServiceProvider = ServiceCollection.BuildServiceProvider();
            }

            if (Discord.LoginState != LoginState.LoggedIn)
            {
                await Discord.LoginAsync(TokenType.Bot, Token);
            }

            TokenSource = new CancellationTokenSource();
            CommandTask = new Task<Task>(ProcessQueueAsync, TokenSource.Token);
            CommandTask.Start();

            await Discord.StartAsync();

            Logger.WriteLine($"DiscordBot: Started.");
        }

        public async Task Stop()
        {
            if (Discord.ConnectionState == ConnectionState.Disconnected || Discord.ConnectionState == ConnectionState.Disconnecting)
            {
                throw new InvalidOperationException();
            }

            await Discord.StopAsync();

            TokenSource.Cancel();
            CommandTask.Wait();

            if (Discord.LoginState == LoginState.LoggedIn)
            {
                await Discord.LogoutAsync();
            }

            Logger.WriteLine($"DiscordBot: Stopped.");
        }

        private async Task OnMessageReceived(SocketMessage arg)
        {
            var pos = int.MinValue;
            var message = arg as SocketUserMessage;
            (var prefix, var service) = CommandList.FirstOrDefault(kv => message?.HasStringPrefix(kv.Key, ref pos) ?? false);

            if (pos >= 0)
            {
                var input = Regex.Replace(message.Content.Substring(pos).Trim(), @"\p{Zs}", " ");
                var context = new CommandContext(Discord, message);

                await ProcessCommand(service, context, input);
            }
        }

        private async Task ProcessCommand(CommandService service, CommandContext context, string input)
        {
            var result = service.Search(context, input);

            if (result.IsSuccess)
            {
                var module = result.Commands[0].Command.Module.Name;
                var command = result.Commands[0].Command.Name;
                var execute = Permission?.GetExecutable((IGuildChannel)context.Channel, module, command) ?? true;

                if (execute)
                {
                    await context.Message.AddReactionAsync(REACTION_PROGRESS);
                    CommandQueue.Add((service, context, input));
                }
                else
                {
                    context.Message.AddReactionAsync(REACTION_DENIED);
                }
            }
            else
            {
                context.Message.AddReactionAsync(REACTION_UNKNOWN);
            }
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                while (!TokenSource.Token.IsCancellationRequested)
                {
                    (var service, var context, var command) = CommandQueue.Take(TokenSource.Token);

                    var result = await service.ExecuteAsync(context, command, ServiceProvider);
                    if (result.Error == CommandError.Exception)
                    {
                        await context.Message.AddReactionAsync(REACTION_ERROR);
                        await context.Channel.SendMessageAsync($"{context.User.Mention} システムエラーが発生しました、モデレーターにご連絡ください。```{result.ErrorReason}```");
                    }
                    else if (!result.IsSuccess)
                    {
                        await context.Message.AddReactionAsync(REACTION_UNKNOWN);
                        await context.Channel.SendMessageAsync($"{context.User.Mention} コマンドを実行できませんでした、入力内容をご確認ください。 ({result.Error})```{command}```");
                    }

                    var type = result.IsSuccess ? "Success" : "Failure";
                    Logger.WriteLine($"DiscordBot: {type}: #{context.Channel.Name} [{context.User.Username}#{context.User.Discriminator}] `{command}` {result.ErrorReason}");
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
