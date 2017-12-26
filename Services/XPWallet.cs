using Discord;
using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
    }

    public class XPWalletAccountKey
    {
        private static ExtKey MasterKey = ExtKey.Parse(Environment.GetEnvironmentVariable("WALLET_MASTER_PRIVATE_KEY"), XPWallet.Network);

        public IUser User { get; }
        public string Label { get; }
        public KeyPath KeyPath { get; }
        public BitcoinAddress Address { get; }

        public static XPWalletAccountKey Create(IUser user)
        {
            return new XPWalletAccountKey(user);
        }

        private XPWalletAccountKey(IUser user)
        {
            User = user;
            Label = GetLabel();
            KeyPath = GetKeyPath();
            Address = GetAddress();
        }

        private string GetLabel(int index = 0)
        {
            var i = GetKeyPath(index).Indexes;

            // remove hardened flags
            i[0] &= 0x7FFFFFFF;
            i[1] &= 0x7FFFFFFF;
            i[2] &= 0x7FFFFFFF;
            i[3] &= 0x7FFFFFFF;
            i[4] &= 0x7FFFFFFF;
            i[5] &= 0x7FFFFFFF;

            return $"wallet:{i[0]}:{i[1]}:{i[2]:D5}:{i[3]:D8}:{i[4]:D8}:{i[5]}";
        }

        private BitcoinAddress GetAddress(int index = 0)
        {
            return GetExtKey(index).ScriptPubKey.GetDestinationAddress(XPWallet.Network);
        }

        private Key GetKey(int index = 0)
        {
            return GetExtKey(index).PrivateKey;
        }

        private ExtKey GetExtKey(int index = 0)
        {
            return MasterKey.Derive(GetKeyPath(index));
        }

        private KeyPath GetKeyPath(int index = 0)
        {
            return new KeyPath(new uint[]
            {
                // always hardened key
                0x80000000 | 0, // currency index
                0x80000000 | 0, // service index
                0x80000000 | Convert.ToUInt32(User.Id >> 48 & 0x0000FFFF), // account index 1: 16bit
                0x80000000 | Convert.ToUInt32(User.Id >> 24 & 0x00FFFFFF), // account index 2: 24bit
                0x80000000 | Convert.ToUInt32(User.Id >>  0 & 0x00FFFFFF), // account index 3: 24bit
                0x80000000 | Convert.ToUInt32(index), // key index
            });
        }
    }
}
