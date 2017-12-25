using Discord;
using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DiscordWallet.Services
{
    public class XPWallet
    {
        public static Network Network { get; }
        
        private static RPCClient RPCClient { get; }
        
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
    }

    public class XPWalletAccountKey
    {
        private static ExtKey MasterKey = ExtKey.Parse(Environment.GetEnvironmentVariable("WALLET_MASTER_PRIVATE_KEY"), XPWallet.Network);

        public IUser User { get; }
        public string Label => GetLabel();
        public KeyPath KeyPath => GetKeyPath();
        public BitcoinAddress Address => GetAddress();

        public static XPWalletAccountKey Create(IUser user)
        {
            return new XPWalletAccountKey(user);
        }

        private XPWalletAccountKey(IUser user)
        {
            User = user;
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
