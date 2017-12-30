using NBitcoin;
using System;
using System.IO;
using System.Linq;

namespace DiscordWallet.Utilities.XPCoin
{
    public class XPTransaction : Transaction
    {
        public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;

        private SigHash SigHash { get; set; } = SigHash.Undefined;

        public XPTransaction(Transaction tx)
        {
            base.ReadWrite(new BitcoinStream(tx.ToBytes()));
        }

        public XPTransaction(XPTransaction tx)
        {
            this.ReadWrite(new BitcoinStream(tx.ToBytes()));
        }

        public override void ReadWrite(BitcoinStream stream)
        {
            using (var memory = new MemoryStream())
            {
                var transaction = new BitcoinStream(memory, stream.Serializing)
                {
                    ProtocolVersion = stream.ProtocolVersion,
                    TransactionOptions = stream.TransactionOptions,
                };

                if (stream.Serializing)
                {
                    base.ReadWrite(transaction);

                    var raw = memory.ToArray();
                    var time = BitConverter.GetBytes(Convert.ToUInt32(Time.ToUnixTimeSeconds()));
                    
                    // convert to XP format (see XPCoin CTransaction class)
                    stream.ReadWrite(ref raw, 0, 4);              // int nVersion
                    stream.ReadWrite(ref time, 0, 4);             // uint32_t nTime
                    stream.ReadWrite(ref raw, 4, raw.Length - 4); // std::vector<CTxIn> vin; std::vector<CTxOut> vout; uint32_t nLockTime;

                    // write signable binary (see GetSignatureHash() method)
                    if (SigHash != SigHash.Undefined)
                    {
                        stream.ReadWrite((uint)SigHash);
                    }
                }
                else
                {
                    var header = stream.Inner.ReadBytes(8);
                    memory.Write(header, 0, 4);
                    stream.Inner.CopyTo(memory);
                    
                    memory.Position = 0;
                    base.ReadWrite(transaction);
                    Time = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToUInt32(header, 4));
                }
            }
        }

        public new TransactionCheckResult Check()
        {
            if (IsCoinBase)
            {
                throw new NotSupportedException();
            }

            if (Inputs.Count == 0)
            {
                return TransactionCheckResult.NoInput;
            }

            if (Outputs.Count == 0)
            {
                return TransactionCheckResult.NoOutput;
            }

            if (this.GetSerializedSize() > XPCoin.MAX_BLOCK_SIZE)
            {
                return TransactionCheckResult.TransactionTooLarge;
            }

            if (Outputs.Any(o => o.Value < 0))
            {
                return TransactionCheckResult.NegativeOutput;
            }

            if (Outputs.Any(o => o.Value > XPCoin.MAX_MONEY))
            {
                return TransactionCheckResult.OutputTooLarge;
            }

            if (MoneyExtensions.Sum(Outputs.Select(o => o.Value)) > XPCoin.MAX_MONEY)
            {
                return TransactionCheckResult.OutputTotalTooLarge;
            }

            if (Inputs.Count != Inputs.Select(i => i.PrevOut).Distinct().Count())
            {
                return TransactionCheckResult.DuplicateInputs;
            }

            if (Inputs.Any(i => i.PrevOut.IsNull))
            {
                return TransactionCheckResult.NullInputPrevOut;
            }

            return TransactionCheckResult.Success;
        }

        public new uint256 GetWitHash()
        {
            throw new NotSupportedException();
        }

        public new int GetVirtualSize()
        {
            throw new NotSupportedException();
        }

        public new uint256 GetSignatureHash(ICoin coin, SigHash sigHash = SigHash.All)
        {
            if (coin.GetHashVersion() == HashVersion.Witness)
            {
                throw new NotSupportedException();
            }

            var index = Inputs.AsIndexedInputs().First(t => t.PrevOut == coin.Outpoint).Index;
            var script = coin.GetScriptCode().Clone();
            var sigTx = new XPTransaction(this) { SigHash = sigHash };
            
            sigTx.Inputs.ForEach(t => t.ScriptSig = Script.Empty);
            sigTx.Inputs[index].ScriptSig = script;

            var hashType = sigTx.SigHash & (SigHash)0x1F;
            switch (hashType)
            {
                case SigHash.None:
                    throw new NotImplementedException();

                case SigHash.Single:
                    throw new NotImplementedException();
            }

            if ((hashType & SigHash.AnyoneCanPay) != 0)
            {
                throw new NotImplementedException();
            }

            return sigTx.GetHash();
        }

        public new TransactionSignature SignInput(ISecret secret, ICoin coin, SigHash sigHash = SigHash.All)
        {
            return SignInput(secret.PrivateKey, coin, sigHash);
        }

        public new TransactionSignature SignInput(Key key, ICoin coin, SigHash sigHash = SigHash.All)
        {
            return key.Sign(GetSignatureHash(coin, sigHash), sigHash);
        }

        public new void Sign(Key[] keys, ICoin[] coins)
        {
            throw new NotImplementedException();
        }

        public new void Sign(ISecret[] secrets, ICoin[] coins)
        {
            Sign(secrets.Select(s => s.PrivateKey).ToArray(), coins);
        }
        
        public new void Sign(ISecret secret, ICoin[] coins)
        {
            Sign(new[] { secret }, coins);
        }
        
        public new void Sign(ISecret[] secrets, ICoin coin)
        {
            Sign(secrets, new[] { coin });
        }
        
        public new void Sign(ISecret secret, ICoin coin)
        {
            Sign(new[] { secret }, new[] { coin });
        }

        public new void Sign(Key key, ICoin[] coins)
        {
            Sign(new[] { key }, coins);
        }

        public new void Sign(Key key, ICoin coin)
        {
            Sign(new[] { key }, new[] { coin });
        }

        public new void Sign(Key[] keys, ICoin coin)
        {
            Sign(keys, new[] { coin });
        }
    }
}
