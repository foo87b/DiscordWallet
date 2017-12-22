using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace DiscordWallet.Core.Services
{
    public class Logger
    {
        public Logger(DiscordSocketClient discord)
        {
            discord.LoggedIn += OnLoggedIn;
            discord.LoggedOut += OnLoggedOut;
            discord.Connected += OnConnected;
            discord.Disconnected += OnDisconnected;
        }

        public void WriteLine(string message)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff")}] {message}");
        }

#pragma warning disable 1998

        private async Task OnLoggedIn()
        {
            WriteLine($"DiscordBot: Logged In.");
        }

        private async Task OnLoggedOut()
        {
            WriteLine($"DiscordBot: Logged Out.");
        }

        private async Task OnConnected()
        {
            WriteLine($"DiscordBot: Connected.");
        }

        private async Task OnDisconnected(Exception e)
        {
            WriteLine($"DiscordBot: Disconnected. {e.Message}");
        }

#pragma warning restore 1998

    }
}
