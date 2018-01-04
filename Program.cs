using DiscordWallet.Core;
using DiscordWallet.Utilities.XPCoin;
using System;
using System.Security;
using System.Threading.Tasks;

namespace DiscordWallet
{
    class Program
    {
        static DiscordBot DiscordBot;

        static void Main(string[] args)
        {
            DotNetEnv.Env.Load();

            DiscordBot = new DiscordBot(
                Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
                Environment.GetEnvironmentVariable("SERVICE_PERMISSION_API")
                );
            DiscordBot.AddService<Services.XPWallet>();
            DiscordBot.AddCommand("!xp").AddModuleAsync<Modules.XPWalletModule>();

            if (SetupWallet())
            {
                DiscordBot.Start().Wait();
                Task.Delay(-1).Wait();
            }
            else
            {
                Console.WriteLine("Press Any Key To Exit...");
                Console.ReadKey();
            }
        }

        private static bool SetupWallet()
        {
            try
            {
                var extPubKey = Environment.GetEnvironmentVariable("WALLET_EXTENDED_PUBLIC_KEY");
                var privateKey = Environment.GetEnvironmentVariable("WALLET_PRIVATE_KEY");

                if (String.IsNullOrEmpty(extPubKey))
                {
                    var wif = Environment.GetEnvironmentVariable("WALLET_EXTENDED_PRIVATE_KEY");

                    XPWalletAccountKey.Setup(wif);
                }
                else if (privateKey.IndexOf('6') == 0)
                {
                    XPWalletAccountKey.Setup(extPubKey, privateKey, GetPassword());
                }
                else
                {
                    XPWalletAccountKey.Setup(extPubKey, privateKey);
                }

                return XPWalletAccountKey.Ready;
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}");
            }

            return false;
        }

        private static SecureString GetPassword()
        {
            var password = new SecureString();

            Console.Write("Password: ");

            while (true)
            {
                var input = Console.ReadKey(true);

                if (input.Key == ConsoleKey.Enter)
                {
                    password.MakeReadOnly();
                    Console.WriteLine();

                    return password;
                }
                else if (input.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.RemoveAt(password.Length - 1);
                }
                else if (input.KeyChar != 0)
                {
                    password.AppendChar(input.KeyChar);
                }
            }
        }
    }
}
