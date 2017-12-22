using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordWallet.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordWallet.Core
{
    public class DiscordBot
    {
        public bool Initialized => ServiceProvider != null;

        private IServiceProvider ServiceProvider;
        private Dictionary<string, CommandService> CommandList = new Dictionary<string, CommandService>();

        private string Token { get; }
        private Logger Logger { get; }
        private IServiceCollection ServiceCollection { get; } = new ServiceCollection();
        private DiscordSocketClient Discord { get; } = new DiscordSocketClient(new DiscordSocketConfig()
        {
            AlwaysDownloadUsers = true,
            DefaultRetryMode = RetryMode.AlwaysRetry,
        });
        
        public DiscordBot(string token)
        {
            Token = token;
            Logger = new Logger(Discord);

            AddService(Discord);
            AddService(Logger);

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
        
        public CommandService SetupCommand(string prefix)
        {
            if (Initialized)
            {
                throw new InvalidOperationException();
            }

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

            if (Discord.LoginState == LoginState.LoggedIn)
            {
                await Discord.LogoutAsync();
            }

            Logger.WriteLine($"DiscordBot: Stopped.");
        }

        private async Task OnMessageReceived(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;

            if (message == null)
            {
                return;
            }
            
            CommandList.AsParallel().ForAll(async kv =>
            {
                var pos = 0;
                var command = (message.HasStringPrefix(kv.Key, ref pos) || message.HasMentionPrefix(Discord.CurrentUser, ref pos))
                    ? Regex.Replace(message.Content.Substring(pos), @"\p{Zs}", " ")
                    : string.Empty;

                if (command == string.Empty)
                {
                    return;
                }

                var context = new CommandContext(Discord, message);
                var result = await kv.Value.ExecuteAsync(context, command, ServiceProvider);

                var type = result.IsSuccess ? "Success" : "Failure";
                Logger.WriteLine($"DiscordBot: {type}: #{message.Channel.Name} [{message.Author.Username}#{message.Author.Discriminator}] `{command}` {result.ErrorReason}");
            });
        }
    }
}
