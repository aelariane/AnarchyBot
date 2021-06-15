using AnarchyBot.Services;
using AnarchyBot.Utils;
using AottgBotLib;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnarchyBot.Commands
{
    public class AottgChatBotModule : BaseCommandModule
    {
        //Assigns via Dependency Injection, automatically sets by CommandHandler so we don't need to care about init
        public ChatBotService BotService { private get; set; }

        [Command("connect")]
        [Description("Connects to region")]
        public async Task ConnectAsync(CommandContext ctx, string region = "eu")
        {
            if (BotService.FindByOwner(ctx.User) != null)
            {
                await ctx.Channel.SendMessageAsync("You already own a bot client.");
                return;
            }
            else if (BotService.FindByChannel(ctx.Channel) != null)
            {
                await ctx.Channel.SendMessageAsync("There already is bot instance at this channel.");
                return;
            }

            PhotonRegion pregion;
            switch (region.ToLower())
            {
                case "eu":
                    pregion = PhotonRegion.Europe;
                    break;

                case "asia":
                    pregion = PhotonRegion.Asia;
                    break;

                case "jp":
                    pregion = PhotonRegion.Japan;
                    break;

                case "us":
                    pregion = PhotonRegion.USA;
                    break;

                default:
                    await ctx.Channel.SendMessageAsync("There is no such region ;(");
                    return;
            }

            ChatBotClient client = BotService.CreateNew(ctx.User, ctx.Channel);
            client.Region = pregion;
            if (ctx.Guild != null)
            {
                client.Guild = ctx.Guild;
            }

            bool result = await client.ConnectToMasterAsync();

            if (!result)
            {
                client.Disconnect();
                BotService.RemoveDisconnectedBots();
                await ctx.Channel.SendMessageAsync("Connection failed.");
            }

            IReadOnlyList<RoomInfo> rooms = client.CachedRooms;

            int page = 1;
            int serversOnPage = 10;
            int roomNumber = 1;
            List<Page> pages = new List<Page>();

            string roomFormat = "[{0, -3}] {1,-25} | {2,-20} | ";
            for (int i = 0; i < rooms.Count; i++)
            {
                int startPosition = i * serversOnPage;
                var stringBuilder = new StringBuilder();
                IEnumerable<RoomInfo> roomPage = rooms.Skip(startPosition).Take(serversOnPage);
                if (roomPage.Count() == 0)
                {
                    break;
                }
                foreach (var room in roomPage)
                {
                    string[] roomData = room.Name.RemoveAll().Split('`');
                    if (roomData.Length != 7)
                    {
                        continue;
                    }
                    string roomName = "";
                    if (roomData[0].Length > 20)
                    {
                        roomData[0] = roomData[0].Substring(0, 17) + "...";
                    }
                    roomName = string.Format(roomFormat, roomNumber++, roomData[0], roomData[1]);
                    roomName += $"({room.PlayerCount}/{room.MaxPlayers})";

                    stringBuilder.AppendLine(roomName);
                }

                stringBuilder.AppendLine("Page: " + (page++).ToString());

                pages.Add(new Page("```" + stringBuilder.ToString() + "```"));
            }

            var emojis = new PaginationEmojis();
            emojis.Left = DiscordEmoji.FromName(ctx.Client, ":arrow_backward:");
            emojis.Right = DiscordEmoji.FromName(ctx.Client, ":arrow_forward:");
            emojis.SkipLeft = null;
            emojis.SkipRight = null;
            emojis.Stop = null;
            await ctx.Channel.SendPaginatedMessageAsync(ctx.User, pages, emojis);
        }

        [Command("dc")]
        public async Task DisconnectBotAsync(CommandContext ctx)
        {
            ChatBotClient byUser = BotService.FindByOwner(ctx.User);
            ChatBotClient byChannel = BotService.FindByChannel(ctx.Channel);
            if (byUser == byChannel)
            {
                await ctx.Channel.SendMessageAsync("Client has been disconnected");
                byUser.Disconnect();
                BotService.RemoveDisconnectedBots();
            }
        }

        [Command("join")]
        public async Task JoinRoomAsync(CommandContext ctx, int roomNumber)
        {
            ChatBotClient byUser = BotService.FindByOwner(ctx.User);
            ChatBotClient byChannel = BotService.FindByChannel(ctx.Channel);
            if ((byUser == null && byChannel == null) || byUser != byChannel)
            {
                await ctx.Channel.SendMessageAsync("You do not have any bots ready to join");
                return;
            }

            try
            {
                var room = byUser.CachedRooms[roomNumber - 1];

                byUser.StartListen(ctx.Client);
                bool result = await byUser.JoinRoomAsync(room);

                if (!result)
                {
                    throw new Exception();
                }

                await ctx.Channel.SendMessageAsync("Connected to room " + room.Name.Split('`')[0]);
            }
            catch
            {
                await ctx.Channel.SendMessageAsync("Error occured during connection. Bot has been disconnected.");
                byUser.Disconnect();
                BotService.RemoveDisconnectedBots();
            }
        }
    }
}