using Discord;
using DiscordWallet.Services;
using NBitcoin;
using NBitcoin.RPC;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordWallet.Utilities.XPCoin
{
    public class XPWalletAccount
    {
        public IUser User => Key.User;
        public string Label => Key.Label;
        public BitcoinAddress Address => Key.Address;
        public Money TotalBalance { get; private set; } = Money.Zero;
        public Money PendingBalance { get; private set; } = Money.Zero;
        public Money ConfirmedBalance { get; private set; } = Money.Zero;
        public Money UnconfirmedBalance { get; private set; } = Money.Zero;
        public IEnumerable<UnspentCoin> PendingCoins { get; private set; } = new UnspentCoin[0];
        public IEnumerable<UnspentCoin> ConfirmedCoins { get; private set; } = new UnspentCoin[0];
        public IEnumerable<UnspentCoin> UnconfirmedCoins { get; private set; } = new UnspentCoin[0];

        private XPWallet Wallet { get; }
        private XPWalletAccountKey Key { get; }

        public XPWalletAccount(XPWallet wallet, XPWalletAccountKey key)
        {
            Wallet = wallet;
            Key = key;
        }

        public async Task Sync()
        {
            await UpdateUnspentCoins();
        }

        public async Task<XPTransaction> SendTo(BitcoinAddress destination, Money amount)
        {
            var coins = PickupUnspentCoins(amount);
            var tx = new TransactionBuilder()
                .AddCoins(coins.Select(c => c.AsCoin()))
                .SetChange(Address)
                .Send(destination, amount)
                .BuildTransaction(false);
            
            var result = await Wallet.SendTransaction(tx, coins, Key.GetP2PKHSigner(coins));

            SpentCoins(coins);

            return result;
        }

        private async Task UpdateUnspentCoins()
        {
            var coins = await Wallet.GetUnspentCoins(Address);

            PendingCoins = coins.Where(c => c.Confirmations < XPWallet.CONFIRMATION && c.Confirmations > 0);
            ConfirmedCoins = coins.Where(c => c.Confirmations >= XPWallet.CONFIRMATION);
            UnconfirmedCoins = coins.Where(c => c.Confirmations == 0);

            PendingBalance = MoneyExtensions.Sum(PendingCoins.Select(c => c.Amount));
            ConfirmedBalance = MoneyExtensions.Sum(ConfirmedCoins.Select(c => c.Amount));
            UnconfirmedBalance = MoneyExtensions.Sum(UnconfirmedCoins.Select(c => c.Amount));
            TotalBalance = ConfirmedBalance + PendingBalance;
        }

        private IEnumerable<UnspentCoin> PickupUnspentCoins(Money amount)
        {
            var coins = ConfirmedCoins.Where(c => c.Amount >= amount).OrderBy(c => c.Amount);
            
            if (coins.Count() >= 1)
            {
                return coins.Take(1);
            }
            else
            {
                var total = Money.Zero;

                return ConfirmedCoins.OrderByDescending(c => c.Amount).TakeWhile(c =>
                {
                    total += c.Amount;

                    return total < amount;
                });
            }
        }

        private void SpentCoins(IEnumerable<UnspentCoin> coins)
        {
            ConfirmedCoins = ConfirmedCoins.Except(coins);
            ConfirmedBalance = MoneyExtensions.Sum(ConfirmedCoins.Select(c => c.Amount));
        }
    }
}
