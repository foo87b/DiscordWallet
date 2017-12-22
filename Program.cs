using DiscordWallet.Core;
using System;
using System.Threading.Tasks;

namespace DiscordWallet
{
    class Program
    {
        static DiscordBot DiscordBot;

        static void Main(string[] args)
        {
            DotNetEnv.Env.Load();

            DiscordBot = new DiscordBot(Environment.GetEnvironmentVariable("DISCORD_TOKEN"));
            
            DiscordBot.Start().Wait();

            Task.Delay(-1).Wait();
        }
    }
}
