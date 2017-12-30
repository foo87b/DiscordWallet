using NBitcoin;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordWallet.Utilities.XPCoin
{
    public class XPCoin
    {
        public const int MAX_BLOCK_SIZE = 2000000;
        public const int MAX_BLOCK_SIZE_GEN = MAX_BLOCK_SIZE / 2;
        public const ProtocolVersion PROTOCOL_VERSION = (ProtocolVersion)91000;

        public static readonly Money COIN = Money.Coins(1m);                    // COIN = 1000000
        public static readonly Money CENT = Money.Cents(1m);                    // CENT = 10000
        public static readonly Money MAX_MONEY = Money.Satoshis(long.MaxValue); // COIN * 200000000000 ( overflow: MAX_MONEY > Money.Satoshi(long.MaxValue) )
        public static readonly Money MIN_TX_FEE = Money.Coins(0.00001m);        // COIN * 0.00001

        public static Network Network { get; }
        public static Money TransactionFee { get; private set; } = Money.Zero;

        static XPCoin()
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
        }

        public static string ToString(Money value)
        {
            return ToString(value.ToDecimal(MoneyUnit.BTC));
        }

        public static string ToString(decimal value)
        {
            return value.ToString("F6");
        }

        public static Money ToMoney(decimal value)
        {
            // COIN = 1000000
            if (value % 0.000001m != 0)
            {
                throw new InvalidCastException();
            }

            return Money.Coins(value);
        }
        
        public static string GetExplorerURL(uint256 tx) => GetExplorerURL("tx", tx.ToString());
        public static string GetExplorerURL(BitcoinAddress address) => GetExplorerURL("address", address.ToString());
        public static string GetExplorerURL(string type, string param)
        {
            return $"https://chainz.cryptoid.info/xp/{type}.dws?{param}.htm";
        }

        public static bool AllowFree(IEnumerable<UnspentCoin> coins, int bytes) => AllowFree(coins.Sum(c => GetPriority(c)) / bytes);
        public static bool AllowFree(double priority)
        {
            return priority > 576000d; // COIN * 144 / 250
        }

        public static double GetPriority(UnspentCoin coin)
        {
            // truncate value (1XP = 1000000satoshi)
            return Convert.ToDouble((coin.Amount / Money.Satoshis(100)).Satoshi) * coin.Confirmations;
        }

        public static Money CalculateFee(XPTransaction tx, IEnumerable<UnspentCoin> coins = null)
        {
            return Money.Max(CalculatePayFee(tx), CalculateMinimumFee(tx, coins));
        }

        public static Money CalculatePayFee(XPTransaction tx) => CalculatePayFee(tx.GetSerializedSize());
        public static Money CalculatePayFee(int bytes)
        {
            return TransactionFee * (1 + bytes / 1000);
        }

        public static Money CalculateMinimumFee(XPTransaction tx, IEnumerable<UnspentCoin> coins = null, int blockSize = 1)
        {
            var bytes = tx.GetSerializedSize();
            var newBlockSize = blockSize + bytes;
            var free = coins.Count() > 0 ? AllowFree(coins, bytes) : false;
            var fee = free && ((blockSize == 1 && bytes < 1000) || (blockSize != 1 && newBlockSize < 27000))
                ? Money.Zero
                : MIN_TX_FEE * (1 + bytes / 1000);

            fee += tx.Outputs.Count(o => o.Value < CENT) * MIN_TX_FEE;

            if (blockSize != 1 && newBlockSize >= (MAX_BLOCK_SIZE_GEN / 2))
            {
                if (newBlockSize >= MAX_BLOCK_SIZE_GEN)
                {
                    return MAX_MONEY;
                }

                fee *= MAX_BLOCK_SIZE_GEN / (MAX_BLOCK_SIZE_GEN - newBlockSize);
            }

            return (fee >= 0 && fee <= MAX_MONEY) ? fee : MAX_MONEY;
        }
    }
}
