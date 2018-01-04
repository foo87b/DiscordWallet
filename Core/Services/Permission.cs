using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace DiscordWallet.Core.Services
{
    public class Permission
    {
        [DataContract]
        internal class GuildChannelResult
        {
            [DataMember] public bool Result { get; set; }
            [DataMember] public string Error { get; set; }
            [DataMember] public List<GuildChannelEntry> Data { get; set; }
        }

        [DataContract]
        internal class GuildChannelEntry
        {
            [DataMember] public ulong Guild { get; set; }
            [DataMember] public ulong Channel { get; set; }
            [DataMember] public string Module { get; set; }
            [DataMember] public string Command { get; set; }
            [DataMember] public bool Execute { get; set; }
        }
        
        private Uri Uri { get; }
        private GuildChannelResult Result;
        
        public Permission(Uri uri)
        {
            Uri = uri;

            UpdateAsync().Wait();
        }
        
        public async Task<bool> UpdateAsync()
        {
            var request = WebRequest.Create(Uri);
            var response = await request.GetResponseAsync();

            using (var stream = response.GetResponseStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(GuildChannelResult));
                var result = serializer.ReadObject(stream) as GuildChannelResult;
                
                if (result?.Result ?? false)
                {
                    Result = result;

                    return true;
                }
            }

            return false;
        }
        
        public bool GetExecutable(IGuildChannel channel, string module, string command)
        {
            try
            {
                var entries = Result.Data.Where(e => e.Module == module && e.Command == command);

                return GetExecutableInEntries(entries, channel.GuildId, channel.Id);
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        private bool GetExecutableInEntries(IEnumerable<GuildChannelEntry> entries, ulong guild, ulong channel)
        {
            var entry = entries.FirstOrDefault(e => e.Guild == guild && e.Channel == channel);

            if (entry == null && guild == 0 && channel == 0)
            {
                throw new KeyNotFoundException();
            }
            else if (channel == 0)
            {
                guild = 0;
            }
            else
            {
                channel = 0;
            }

            return entry?.Execute ?? GetExecutableInEntries(entries, guild, channel);
        }
    }
}
