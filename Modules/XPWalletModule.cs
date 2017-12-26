using Discord;
using Discord.Commands;
using DiscordWallet.Services;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordWallet.Modules
{
    [Group("xp")]
    public class XPWalletModule : ModuleBase
    {
        public const string COMMAND_BALANCE  = "balance";
        public const string COMMAND_DEPOSIT  = "deposit";
        public const string COMMAND_HELP     = "help";
        public const string COMMAND_RAIN     = "rain";
        public const string COMMAND_TIP      = "tip";
        public const string COMMAND_WITHDRAW = "withdraw";

        public static readonly Emoji REACTION_DENIED   = new Emoji("\U0001f6ab"); // U+1F6AB is :no_entry_sign:
        public static readonly Emoji REACTION_ERROR    = new Emoji("\u26a0");     // U+26A0  is :warning:
        public static readonly Emoji REACTION_FAILURE  = new Emoji("\u274e");     // U+274E  is :negative_squared_cross_mark:
        public static readonly Emoji REACTION_PROGRESS = new Emoji("\u23f3");     // U+23F3  is :hourglass_flowing_sand:
        public static readonly Emoji REACTION_SUCCESS  = new Emoji("\u2705");     // U+2705  is :white_check_mark:
        public static readonly Emoji REACTION_UNKNOWN  = new Emoji("\u2753");     // U+2753  is :question:

        public XPWallet Wallet { get; set; }

        [Command(COMMAND_HELP)]
        public async Task CommandHelpAsync(string command = null)
        {
            await Context.Message.AddReactionAsync(REACTION_PROGRESS);

            if (String.IsNullOrWhiteSpace(command))
            {
                await ReplySuccess(String.Join("\n", new string[]
                {
                    $"```asciidoc",
                    $"= コマンド一覧",
                    $"!xp {COMMAND_BALANCE}  :: 現在の残高を表示します。",
                    $"!xp {COMMAND_DEPOSIT}  :: 預入先となるアドレスを表示します。",
                    $"!xp {COMMAND_WITHDRAW} :: 指定した量のXPを外部アドレスへ送付します。",
                    $"!xp {COMMAND_TIP}      :: 指定した量のXPをDiscord上のユーザーへ送付します。",
                    $"!xp {COMMAND_RAIN}     :: Discordのチャンネルに居るオンライン状態のユーザーへ指定した量のXPを分配します。",
                    $"!xp {COMMAND_HELP}     :: 詳しく知りたいコマンドを指定することでより詳細なヘルプを表示します。",
                    $"",
                    $"[!xp help <コマンド> にて詳細なヘルプを表示します。]",
                    $"引数や制限については各コマンドのヘルプをご参照ください。",
                    $"",
                    $"= XPJPWalletについて",
                    $"eXperience Points (XP) を管理出来るDiscord上のウォレットです。",
                    $"サポートしている機能はコマンド一覧をご確認ください。",
                    $"",
                    $"= 預入について",
                    $"当ウォレットへ預入を行った場合、{XPWallet.CONFIRMATION} confirmation にて利用が可能となります。",
                    $"アドレスを間違えて預入されますと、こちらではどうすることも出来ませんのでお気を付けください。",
                    $"",
                    $"= 鋳造について",
                    $"2017年12月現在、鋳造はサポートしておりませんので残高は増えません。",
                    $"将来的なサポートへ向け開発を行っておりますので、続報をお待ちください。",
                    $"```",
                }));

                return;
            }

            switch (command)
            {
                case COMMAND_BALANCE:
                    throw new NotImplementedException();

                case COMMAND_DEPOSIT:
                    throw new NotImplementedException();

                case COMMAND_HELP:
                    throw new NotImplementedException();

                case COMMAND_RAIN:
                    throw new NotImplementedException();

                case COMMAND_TIP:
                    throw new NotImplementedException();

                case COMMAND_WITHDRAW:
                    throw new NotImplementedException();

                default:
                    throw new NotImplementedException();
            }
        }

        [Command(COMMAND_BALANCE)]
        public async Task CommandBalanceAsync()
        {
            throw new NotImplementedException();
        }

        [Command(COMMAND_DEPOSIT)]
        public async Task CommandDepositAsync()
        {
            await Context.Message.AddReactionAsync(REACTION_PROGRESS);

            try
            {
                var account = await Wallet.GetAccount(Context.User);
                var embed = new EmbedBuilder()
                {
                    Color = Color.DarkBlue,
                    Title = "eXperience Points: 預入先アドレス",
                    Description = $"```{account.Address}```上記は{Context.User.Mention}さん専用の預入先のアドレスとなります。\n預入後は {XPWallet.CONFIRMATION} confirmation にて利用が可能となります。",
                    ThumbnailUrl = Context.User.GetAvatarUrl(),
                    Author = new EmbedAuthorBuilder()
                        .WithUrl(GetExplorerURL(account.Address))
                        .WithName($"{Context.User.Username}#{Context.User.Discriminator}")
                        .WithIconUrl(Context.User.GetAvatarUrl()),
                };

                await ReplySuccess("eXperience Points 預入先のご案内です。", embed);
            }
            catch (Exception e)
            {
                await ReplyError(e);

                throw e;
            }
        }

        [Command(COMMAND_WITHDRAW)]
        public async Task CommandWithdrawAsync(string address, decimal amount)
        {
            throw new NotImplementedException();
        }

        [Command(COMMAND_TIP)]
        public async Task CommandTipAsync(IUser user, decimal amount)
        {
            throw new NotImplementedException();
        }

        [Command(COMMAND_RAIN)]
        public async Task CommandRainAsync(decimal amount)
        {
            throw new NotImplementedException();
        }

        [Command, Priority(-1)]
        public async Task CommandAsync(params string[] args)
        {
            await Task.WhenAll(new List<Task>()
            {
                Context.Message.AddReactionAsync(REACTION_UNKNOWN),
                ReplyAsync($"{Context.User.Mention} コマンドを認識できませんでした。"),
            });
        }

        private string GetExplorerURL(BitcoinAddress address)
        {
            return $"https://chainz.cryptoid.info/xp/search.dws?q={address}";
        }

        private async Task ReplySuccess(string message, EmbedBuilder embed = null)
        {
            if (embed != null)
            {
                embed.Timestamp = DateTime.UtcNow;
                embed.Footer = new EmbedFooterBuilder()
                    .WithText("XPJPWallet")
                    .WithIconUrl(Context.Client.CurrentUser.GetAvatarUrl());
            }

            await Task.WhenAll(new List<Task>()
            {
                Context.Message.AddReactionAsync(REACTION_SUCCESS),
                ReplyAsync($"{Context.User.Mention} {message}", false, embed)
            });
        }

        private async Task ReplyError(Exception e)
        {
            await Task.WhenAll(new List<Task>()
            {
                Context.Message.AddReactionAsync(REACTION_ERROR),
                ReplyAsync($"{Context.User.Mention} エラーが発生しました、モデレーターにご連絡ください。```{e.Message}```"),
            });
        }
    }
}
