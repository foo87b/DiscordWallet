using Discord;
using Discord.Commands;
using DiscordWallet.Services;
using DiscordWallet.Utilities.XPCoin;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordWallet.Modules
{
    [Group("xp")]
    public class XPWalletModule : ModuleBase
    {
        public const decimal TRANSFER_MIN = 0.01m;
        public const decimal TRANSFER_MAX = 10000000000m;
        public const string COMMAND_BALANCE  = "balance";
        public const string COMMAND_DEPOSIT  = "deposit";
        public const string COMMAND_HELP     = "help";
        public const string COMMAND_RAIN     = "rain";
        public const string COMMAND_STORM    = "storm";
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
                await ReplySuccess(String.Join("\n", new[]
                {
                    $"```asciidoc",
                    $"= コマンド一覧",
                    $"!xp {COMMAND_BALANCE}  :: 現在の残高を表示します。",
                    $"!xp {COMMAND_DEPOSIT}  :: 預入先となるアドレスを表示します。",
                    $"!xp {COMMAND_WITHDRAW} :: 指定した量のXPを外部アドレスへ送付します。",
                    $"!xp {COMMAND_TIP}      :: 指定した量のXPをDiscord上のユーザーへ送付します。",
                    $"!xp {COMMAND_RAIN}     :: Discordのチャンネルに居る特定の状態のユーザーへ指定した量のXPを分配します。 (量÷人数)",
                    $"!xp {COMMAND_STORM}    :: Discordのチャンネルに居る特定の状態のユーザーへ指定した量のXPを配布します。 (量×人数)",
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

                case COMMAND_STORM:
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
            await Context.Message.AddReactionAsync(REACTION_PROGRESS);

            try
            {
                var account = await Wallet.GetAccount(Context.User, true);

                await ReplySuccess("eXperience Points 残高のご案内です。", DefaultEmbed(account, new EmbedBuilder()
                {
                    Color = Color.DarkPurple,
                    Title = "eXperience Points: 残高",
                    Description = String.Join("\n", new[]
                    {
                        $"{account.User.Mention}さんの残高は下記となります。",
                        $"`{COMMAND_TIP}`や`{COMMAND_RAIN}`で利用可能なのは検証済の分のみとなりますのでご注意ください。",
                    }),
                    Fields = new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder().WithName("残高").WithValue($"{XPCoin.ToString(account.TotalBalance)} XP"),
                        new EmbedFieldBuilder().WithIsInline(true).WithName("検証済").WithValue($"{XPCoin.ToString(account.ConfirmedBalance)} XP"),
                        new EmbedFieldBuilder().WithIsInline(true).WithName("検証中").WithValue($"{XPCoin.ToString(account.PendingBalance)} XP"),
                        new EmbedFieldBuilder().WithIsInline(true).WithName("未検証").WithValue($"{XPCoin.ToString(account.UnconfirmedBalance)} XP"),
                    },
                }));
            }
            catch (Exception e)
            {
                await ReplyError(e);

                throw e;
            }
        }

        [Command(COMMAND_DEPOSIT)]
        public async Task CommandDepositAsync()
        {
            await Context.Message.AddReactionAsync(REACTION_PROGRESS);

            try
            {
                var account = await Wallet.GetAccount(Context.User);

                await ReplySuccess("eXperience Points 預入先のご案内です。", DefaultEmbed(account, new EmbedBuilder()
                {
                    Color = Color.DarkBlue,
                    Title = "eXperience Points: 預入先アドレス",
                    ThumbnailUrl = account.User.GetAvatarUrl(),
                    Description = String.Join("\n", new[]
                    {
                        $"```{account.Address}```",
                        $"上記は{account.User.Mention}さん専用の預入先のアドレスとなります。",
                        $"預入後は {XPWallet.CONFIRMATION} confirmation にて利用が可能となります。",
                    }),
                }));
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
            await Context.Message.AddReactionAsync(REACTION_PROGRESS);
            
            var account = await Wallet.GetAccount(Context.User, true);
            var embed = DefaultEmbed(account, new EmbedBuilder()
            {
                Color = Color.Red,
                Title = $"eXperience Points: {COMMAND_WITHDRAW}",
            });
            
            try
            {
                var destination = BitcoinAddress.Create(address, XPCoin.Network);
                var tx = await TransferTo(account, destination, amount, embed);

                if (tx != null)
                {
                    embed.Color = Color.DarkerGrey;
                    embed.Description = String.Join("\n", new[]
                    {
                        $"{account.User.Mention}さんが指定されたアドレスへ送付が完了しました。",
                        $"今回の取引の詳細は下記となりますので確認をお願いします。",
                    });

                    await ReplySuccess($"eXperience Points 送付が完了しました。", EmbedSendFields(account, destination, amount, embed, tx));
                }
                else
                {
                    await ReplyFailure($"eXperience Points 送付に失敗しました。", EmbedSendFields(account, destination, amount, embed));
                }
            }
            catch (FormatException)
            {
                embed.Description = String.Join("\n", new[]
                {
                    $"{account.User.Mention}さんが指定された宛先への送付が出来ませんでした。",
                    $"__**送付先のアドレス**__に間違いがないか確認をお願いします。",
                });

                await ReplyFailure($"eXperience Points 送付に失敗しました。", EmbedSendFields(account, null, address, amount, embed));
            }
            catch (Exception e)
            {
                await ReplyError(e);

                throw e;
            }
        }

        [Command(COMMAND_TIP)]
        public async Task CommandTipAsync(IUser user, decimal amount)
        {
            throw new NotImplementedException();
        }

        [Command(COMMAND_RAIN)]
        public async Task CommandRainAsync(decimal amount, string mode = null)
        {
            throw new NotImplementedException();
        }

        [Command(COMMAND_STORM)]
        public async Task CommandStormAsync(decimal amount, string mode = null)
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

        private async Task<XPTransaction> TransferTo(XPWalletAccount account, BitcoinAddress destination, decimal amount, EmbedBuilder embed)
        {
            try
            {
                if (amount < TRANSFER_MIN || amount > TRANSFER_MAX)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return await account.SendTo(destination, XPCoin.ToMoney(amount));
            }
            catch (InvalidCastException)
            {
                embed.Description = String.Join("\n", new[]
                {
                    $"{account.User.Mention}さんが指定された宛先への送付が出来ませんでした。",
                    $"送付可能な量は__**小数点第六位**__までとなります。",
                });
            }
            catch (ArgumentOutOfRangeException)
            {
                embed.Description = String.Join("\n", new[]
                {
                    $"{account.User.Mention}さんが指定された宛先への送付が出来ませんでした。",
                    $"送付可能な量は __**`{TRANSFER_MIN} XP` から `{TRANSFER_MAX} XP`**__ までとなります。",
                });
            }
            catch (NotEnoughFundsException)
            {
                embed.Description = String.Join("\n", new[]
                {
                    $"{account.User.Mention}さんが指定された宛先への送付が出来ませんでした。",
                    $"__**送付可能な残高が不足**__ しております。",
                });
            }

            return null;
        }

        private EmbedBuilder DefaultEmbed(XPWalletAccount account, EmbedBuilder embed = null)
        {
            return (embed ?? new EmbedBuilder())
                .WithAuthor(new EmbedAuthorBuilder()
                {
                    Url = XPCoin.GetExplorerURL(account.Address),
                    Name = $"{account.User.Username}#{account.User.Discriminator}",
                    IconUrl = account.User.GetAvatarUrl(),
                })
                .WithFooter(new EmbedFooterBuilder()
                {
                    Text = "XPJPWallet",
                    IconUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                })
                .WithCurrentTimestamp();
        }
        
        private EmbedBuilder EmbedSendFields(XPWalletAccount from, BitcoinAddress to, decimal amount, EmbedBuilder embed, XPTransaction tx = null) => EmbedSendFields(from, to, to.ToString(), amount, embed, tx);
        private EmbedBuilder EmbedSendFields(XPWalletAccount from, BitcoinAddress to, string label, decimal amount, EmbedBuilder embed, XPTransaction tx = null)
        {
            embed.AddField("残高 (検証済の分)", $"{XPCoin.ToString(from.ConfirmedBalance)} XP");
            embed.AddField("送付元", $"[{from.User.Username}#{from.User.Discriminator}]({XPCoin.GetExplorerURL(from.Address)})", true);
            embed.AddField("送付先", to != null ? $"[{label}]({XPCoin.GetExplorerURL(to)})" : label, true);
            embed.AddField("送付量", $"{XPCoin.ToString(amount)} XP", true);

            if (tx != null)
            {
                embed.AddField("結果", "成功", true);
                embed.AddField("トランザクション", $"[{tx.GetHash()}]({XPCoin.GetExplorerURL(tx.GetHash())})");
            }
            else
            {
                embed.AddField("結果", "失敗", true);
            }

            return embed;
        }

        private async Task ReplySuccess(string message, Embed embed = null)
        {
            await Task.WhenAll(new List<Task>()
            {
                Context.Message.AddReactionAsync(REACTION_SUCCESS),
                ReplyAsync($"{Context.User.Mention} {message}", false, embed)
            });
        }

        private async Task ReplyFailure(string message, Embed embed = null)
        {
            await Task.WhenAll(new List<Task>()
            {
                Context.Message.AddReactionAsync(REACTION_FAILURE),
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
