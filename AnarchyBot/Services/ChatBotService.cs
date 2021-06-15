using AnarchyBot.Utils;
using DSharpPlus.Entities;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace AnarchyBot.Services
{
    public class ChatBotService
    {
        private List<ChatBotClient> _bots = new List<ChatBotClient>();
        private object _syncer = new object();
        private Timer _timer;

        public int BotsCount => _bots.Count;

        ~ChatBotService()
        {
            _timer.Stop();
        }

        public ChatBotService()
        {
            _timer = new Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
            _timer.Elapsed += (sender, args) => RemoveDisconnectedBots();
            _timer.Start();
        }

        public bool CanCreateBot(DiscordUser user, DiscordChannel channel)
        {
            lock (_syncer)
            {
                return FindByOwner(user) == null && FindByChannel(channel) == null;
            }
        }

        public ChatBotClient CreateNew(DiscordUser user, DiscordChannel channel, string botName = "AnarchyBot")
        {
            var client = new ChatBotClient(user, channel, botName);
            lock (_syncer)
            {
                _bots.Add(client);
            }
            client.IsUsingPhotonServer = true;
            client.AppId = string.Empty;
            return client;
        }

        public ChatBotClient FindByChannel(DiscordChannel channel)
        {
            RemoveDisconnectedBots();
            ulong channelId = channel.Id;
            lock (_syncer)
            {
                return _bots.FirstOrDefault(x => x.DiscordChannel.Id == channelId);
            }
        }

        public ChatBotClient FindByOwner(DiscordUser user)
        {
            RemoveDisconnectedBots();
            ulong userId = user.Id;
            lock (_syncer)
            {
                return _bots.FirstOrDefault(x => x.Owner.Id == userId);
            }
        }

        public void RemoveDisconnectedBots()
        {
            lock (_syncer)
            {
                foreach (var bot in _bots)
                {
                    if (bot == null)
                    {
                        continue;
                    }
                    if (bot.CheckIfNeedDisconnect() && bot.State != ClientState.Disconnected)
                    {
                        bot.InactivityDisconnect();
                    }
                }
                _bots = _bots
                    .Where(x => x != null && x.State != ClientState.Disconnected)
                    .ToList();
            }
        }
    }
}