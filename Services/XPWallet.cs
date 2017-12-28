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

        public static Network Network { get; }
        
        private static RPCClient RPCClient { get; }

        private HashSet<ulong> AccountList { get; } = new HashSet<ulong>();

        static XPWallet()
        {
            Network = new NetworkBuilder()
                .SetName("xp")
                .SetMagic(0xe5e2f8b4)
                .SetPort(28192)
                .SetRPCPort(28191)
                .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 203 })
                .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 75 })
                .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 20 })
                .SetGenesis(new Block()) // FIXME
                .SetConsensus(new Consensus()) // FIXME
                .BuildAndRegister();
            
            RPCClient = new RPCClient(new RPCCredentialString()
            {
                Server = Environment.GetEnvironmentVariable("WALLET_XP_RPC_SERVER"),
                UserPassword = new NetworkCredential()
                {
                    UserName = Environment.GetEnvironmentVariable("WALLET_XP_RPC_USERNAME"),
                    Password = Environment.GetEnvironmentVariable("WALLET_XP_RPC_PASSWORD"),
                },
            }, Network);
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
