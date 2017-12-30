using Discord;
using NBitcoin;
using NBitcoin.BuilderExtensions;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordWallet.Utilities.XPCoin
{

    public class XPWalletAccountKey
    {
        private static ExtKey MasterKey = ExtKey.Parse(Environment.GetEnvironmentVariable("WALLET_MASTER_PRIVATE_KEY"), XPCoin.Network);

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

        public Action<XPTransaction> GetP2PKHSigner(IEnumerable<UnspentCoin> coins) => GetP2PKHSigner(coins.Select(c => c.AsCoin()));
        public Action<XPTransaction> GetP2PKHSigner(IEnumerable<ICoin> coins)
        {
            return tx =>
            {
                var key = GetKey();
                var builder = new P2PKHBuilderExtension();

                for (var i = 0; i < tx.Inputs.Count; i++)
                {
                    var coin = coins.FirstOrDefault(c => c.Outpoint == tx.Inputs[i].PrevOut);

                    if (coin != null)
                    {
                        tx.Inputs[i].ScriptSig = PayToPubkeyHashTemplate.Instance.GenerateScriptSig(tx.SignInput(key, coin), key.PubKey);
                    }
                }
            };
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
            return GetExtKey(index).ScriptPubKey.GetDestinationAddress(XPCoin.Network);
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
