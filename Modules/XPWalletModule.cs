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
        public static readonly Emoji REACTION_ERROR    = new Emoji("\u26a0"); // U+26A0 is :warning:
        public static readonly Emoji REACTION_FAILURE  = new Emoji("\u274e"); // U+274E is :negative_squared_cross_mark:
        public static readonly Emoji REACTION_PROGRESS = new Emoji("\u23f3"); // U+23F3 is :hourglass_flowing_sand:
        public static readonly Emoji REACTION_SUCCESS  = new Emoji("\u2705"); // U+2705 is :white_check_mark:
        public static readonly Emoji REACTION_UNKNOWN  = new Emoji("\u2753"); // U+2753 is :question:

        public XPWallet Wallet { get; set; }

        [Command("help")]
        public async Task CommandHelpAsync()
        {
            throw new NotImplementedException();
        }

        [Command("balance")]
        public async Task CommandBalanceAsync()
        {
            throw new NotImplementedException();
        }

        [Command("deposit")]
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

        [Command("withdraw")]
        public async Task CommandWithdrawAsync(string address, decimal amount)
        {
            throw new NotImplementedException();
        }

        [Command("tip")]
        public async Task CommandTipAsync(IUser user, decimal amount)
        {
            throw new NotImplementedException();
        }

        [Command("rain")]
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

        private async Task ReplySuccess(string message, EmbedBuilder embed)
        {
            embed.Timestamp = DateTime.UtcNow;
            embed.Footer = new EmbedFooterBuilder()
                .WithText("XPJPWallet")
                .WithIconUrl(Context.Client.CurrentUser.GetAvatarUrl());

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
