using Discord.WebSocket;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordWallet.Services
{
    public class XPWallet
    {
        public static Network Network { get; private set; }
        
        public XPWallet(DiscordSocketClient discord)
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
    }
}
