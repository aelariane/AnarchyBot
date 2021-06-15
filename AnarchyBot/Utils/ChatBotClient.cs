using AottgBotLib;
using AottgBotLib.Handlers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnarchyBot.Utils
{
    public class ChatBotClient : BotClient
    {
        private List<RoomInfo> _cachedRooms;
        private DiscordClient _ownerClient;

        public IReadOnlyList<RoomInfo> CachedRooms
        {
            get
            {
                if (_cachedRooms == null)
                {
                    _cachedRooms = new List<RoomInfo>(RoomList);
                }
                return _cachedRooms;
            }
        }

        public DiscordChannel DiscordChannel { get; }
        public DiscordGuild Guild { get; set; }
        public RoomCallbacks InRoomCallbacks { get; }
        public DateTime LastActivity { get; private set; } = DateTime.UtcNow;
        public Action<Player, string, string> OnChatMessageReceived { get; set; } = (pl, snd, cnt) => { };
        public DiscordUser Owner { get; }
        public bool UseDefaultReceiveBehaviour { get; set; } = true;

        ~ChatBotClient()
        {
            if (_ownerClient != null)
            {
                _ownerClient.MessageCreated -= HandleDiscordMessage;
            }
        }

        public ChatBotClient(
            DiscordUser creator,
            DiscordChannel createdAtChannel,
            string name) : base(name)
        {
            Owner = creator;
            DiscordChannel = createdAtChannel;
            InRoomCallbacks = new RoomCallbacks();
            AddCallbackTarget(InRoomCallbacks);

            this.RPCHandler.AddCallback("Chat", ReceiveChatMessage);
        }

        private async Task HandleDiscordMessage(DiscordClient client, MessageCreateEventArgs args)
        {
            try
            {
                if (args.Channel.Id == DiscordChannel.Id && State != ClientState.Disconnected && State == ClientState.Joined)
                {
                    if (args.Author == client.CurrentUser || args.Message.Content.StartsWith("a!") || args.Author.IsBot)
                    {
                        return;
                    }

                    DiscordMessage message = args.Message;
                    string content = message.Content;

                    foreach (var user in message.MentionedUsers)
                    {
                        content = content.Replace(user.Mention, "@" + user.Username);
                    }

                    await SendMessage(args.Author, content);
                }
            }
            catch
            {
                await DiscordChannel.SendMessageAsync("Error occured while trying to send message to Aottg");
            }
        }

        private void ReceiveChatMessage(RPCArguments arguments)
        {
            if (CheckIfNeedDisconnect())
            {
                InactivityDisconnect();
            }
            if (arguments.Arguments.Length != 2 || arguments.CallInfo.ViewID != 2)
            {
                return;
            }
            string sender = arguments.Arguments[1].ToString().Trim().RemoveAll();
            string messageContent = arguments.Arguments[0].ToString().Trim().RemoveAll();
            if (UseDefaultReceiveBehaviour)
            {
                string content = messageContent;
                if (sender.Length > 0)
                {
                    content = sender + ": " + messageContent;
                }
                content = $"[{arguments.CallInfo.Sender.ActorNumber}] {content}";
                DiscordChannel.SendMessageAsync(content);
            }
            else
            {
                OnChatMessageReceived(arguments.CallInfo.Sender, sender, messageContent);
            }
        }

        public bool CheckIfNeedDisconnect()
        {
            return (DateTime.UtcNow - LastActivity) > TimeSpan.FromMinutes(3);
        }

        public void ClearRoomsCache()
        {
            _cachedRooms = null;
        }

        public void InactivityDisconnect()
        {
            if (DiscordChannel != null)
            {
                var channel = DiscordChannel;
                Task.Run(async () => await channel.SendMessageAsync("Client has been disconnected due to inactivity"));
            }
            Disconnect();
        }

        public async Task SendMessage(DiscordUser sender, string content)
        {
            UpdateActivity();

            try
            {
                string senderName = sender.Username;
                string color = "ffffff";
                if (Guild != null)
                {
                    var member = await Guild.GetMemberAsync(sender.Id);
                    if (member.Nickname != null && member.Nickname.Length > 0)
                    {
                        senderName = member.Nickname;
                    }
                    if (member.Roles.Count() > 0)
                    {
                        DiscordColor roleColor = member.Roles.OrderByDescending(x => x.Position).FirstOrDefault().Color;
                        color = roleColor.R.ToString("X2") + roleColor.G.ToString("X2") + roleColor.B.ToString("X2");
                    }
                }

                this.SendRPC(2, "Chat", new string[] { $"<color=#{color}>{senderName}</color>: <b>{content}</b>", "" }, PhotonTargets.Others);
            }
            catch
            {
                await DiscordChannel.SendMessageAsync("Error happened ;(");
            }
        }

        public void StartListen(DiscordClient client)
        {
            _ownerClient = client;
            client.MessageCreated += HandleDiscordMessage;
        }

        public void UpdateActivity()
        {
            LastActivity = DateTime.UtcNow;
        }

        public sealed class RoomCallbacks : IInRoomCallbacks
        {
            public Action<Player> OnPlayerJoined { get; set; } = (pl) => { };
            public Action<Player> OnPlayerLeft { get; set; } = (pl) => { };

            public void OnMasterClientSwitched(Player newMasterClient)
            {
            }

            public void OnPlayerEnteredRoom(Player newPlayer)
            {
                OnPlayerJoined?.Invoke(newPlayer);
            }

            public void OnPlayerLeftRoom(Player otherPlayer)
            {
                OnPlayerLeft?.Invoke(otherPlayer);
            }

            public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
            {
            }

            public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
            {
            }
        }
    }
}