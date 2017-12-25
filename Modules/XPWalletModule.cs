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
        public static readonly Emoji REACTION_ERROR    = new Emoji("\u26a0"); // U+26A0 is :warning:
        public static readonly Emoji REACTION_FAILURE  = new Emoji("\u274e"); // U+274E is :negative_squared_cross_mark:
        public static readonly Emoji REACTION_PROGRESS = new Emoji("\u23f3"); // U+23F3 is :hourglass_flowing_sand:
        public static readonly Emoji REACTION_SUCCESS  = new Emoji("\u2705"); // U+2705 is :white_check_mark:
        public static readonly Emoji REACTION_UNKNOWN  = new Emoji("\u2753"); // U+2753 is :question:

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
            await Task.WhenAll(new List<Task>()
            {
                Context.Message.AddReactionAsync(REACTION_UNKNOWN),
                ReplyAsync($"{Context.User.Mention} コマンドを認識できませんでした。"),
            });
        }
    }
}
