using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordWallet.Modules
{
    [Group("xp")]
    public class XPWalletModule : ModuleBase
    {
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
            throw new NotImplementedException();
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
            await ReactToUnknown();

            await ReplyAsync($"{Context.User.Mention} コマンドを認識できませんでした。");
        }

        private async Task ReactToUnknown()
        {
            await Context.Message.AddReactionAsync(new Emoji("\u2753")); // U+2753 is :question:
        }
    }
}
