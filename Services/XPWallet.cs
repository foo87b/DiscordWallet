using Discord;
using DiscordWallet.Utilities.XPCoin;
using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace DiscordWallet.Services
{
    public class XPWallet
    {
        public const int CONFIRMATION = 6;
        
        private static RPCClient RPCClient { get; }

        private HashSet<ulong> AccountList { get; } = new HashSet<ulong>();

        static XPWallet()
        {
            RPCClient = new RPCClient(new RPCCredentialString()
            {
                Server = Environment.GetEnvironmentVariable("WALLET_XP_RPC_SERVER"),
                UserPassword = new NetworkCredential()
                {
                    UserName = Environment.GetEnvironmentVariable("WALLET_XP_RPC_USERNAME"),
                    Password = Environment.GetEnvironmentVariable("WALLET_XP_RPC_PASSWORD"),
                },
            }, XPCoin.Network);
        }

        public bool HasAccount(IUser user)
        {
            return AccountList.Contains(user.Id);
        }

        public async Task<XPWalletAccount> GetAccount(IUser user, bool sync = false)
        {
            var key = XPWalletAccountKey.Create(user);

            if (!HasAccount(user))
            {
                await CreateAccount(key);
            }

            var account = new XPWalletAccount(this, key);

            if (sync)
            {
                await account.Sync();
            }

            return account;
        }

        public async Task<UnspentCoin[]> GetUnspentCoins(BitcoinAddress address, int minconf = 0, int maxconf = 9999999)
        {
            return await RPCClient.ListUnspentAsync(minconf, maxconf, address);
        }

        public async Task<XPTransaction> SendTransaction(Transaction tx, IEnumerable<UnspentCoin> coins, Action<XPTransaction> signer)
        {
            var time = DateTimeOffset.UtcNow;
            var baseTx = new XPTransaction(tx) { Time = time };
            var truncatedTx = new XPTransaction(tx) { Time = time };

            // truncate value (1XP = 1000000satoshi)
            truncatedTx.Outputs.ForEach(o => o.Value = Money.Satoshis(o.Value.ToDecimal(MoneyUnit.Satoshi) / 100m));

            // double check transaction (fail safe)
            var decodedTx = new XPTransaction(await RPCClient.DecodeRawTransactionAsync(truncatedTx.ToBytes()));
            if (baseTx.GetHash() != decodedTx.GetHash())
            {
                throw new InvalidOperationException("生成されたトランザクションにエラーがあります。 (ダブルチェックエラー)");
            }
            
            signer.Invoke(truncatedTx);
            
            var check = truncatedTx.Check();
            if (check != TransactionCheckResult.Success)
            {
                throw new InvalidOperationException($"生成されたトランザクションにエラーがあります。 (チェックエラー: {check})");
            }

            // truncate value (1XP = 1000000satoshi)
            var spents = coins.Select(c => new Coin(c.OutPoint, new TxOut(Money.Satoshis(c.Amount.ToDecimal(MoneyUnit.Satoshi) / 100m), c.ScriptPubKey)));

            var fee = XPCoin.CalculateFee(truncatedTx, coins);
            var pay = truncatedTx.GetFee(spents.ToArray());
            if (fee > Money.Zero && pay != fee)
            {
                throw new InvalidOperationException($"生成されたトランザクションにエラーがあります。 (手数料不一致: fee={fee}, pay={pay})");
            }
                 
            await RPCClient.SendRawTransactionAsync(truncatedTx.ToBytes());

            return truncatedTx;
        }

        private async Task CreateAccount(XPWalletAccountKey key)
        {
            var addresses = RPCClient.GetAddressesByAccount(key.Label);

            if (addresses.Count() == 0)
            {
                await RPCClient.SendCommandAsync(RPCOperations.setaccount, key.Address.ToString(), key.Label);
                await RPCClient.ImportAddressAsync(key.Address, key.Label, false);
            }
            else if (addresses.Count() > 1 || !addresses.Any(a => a == key.Address))
            {
                throw new InvalidOperationException("サーバー側のアカウント情報が不正です。");
            }

            AccountList.Add(key.User.Id);
        }
    }
}
