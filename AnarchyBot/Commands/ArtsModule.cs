using AnarchyBot.Models;
using AnarchyBot.Services;
using Arglib;
using Arglib.Posix;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DSharpPlus;
using System.IO;

namespace AnarchyBot.Commands
{
    [Group("art")]
    [Description("Commands related to artworks")]
    public class ArtsModule : BaseCommandModule
    {
        private OptionIdentifier _authorOption = new OptionIdentifier('a', "author");
        private OptionIdentifier _authorProfileOption = new OptionIdentifier('p', "profile");
        private OptionIdentifier _fromOption = new OptionIdentifier('f', "from");
        private OptionIdentifier _sourceOption = new OptionIdentifier('s', "source");
        private OptionIdentifier _tagsOption = new OptionIdentifier("tags");
        private OptionIdentifier _titleOption = new OptionIdentifier('t', "title");
        private OptionIdentifier _uploadOption = new OptionIdentifier('u', "upload");

        private bool CanPostArts(DiscordMember member)
        {
            bool isAdmin = member.Roles.Any(x => (x.Permissions & Permissions.Administrator) == Permissions.Administrator);
            bool hasArterRole = member.Roles.Any(x => x.Name == "Arter");

            return isAdmin || hasArterRole;
        }

        private async Task<string> UploadAsync(DiscordGuild guild, string url, IArguments args, bool uploadToDatabase)
        {
            if(Uri.TryCreate(url, UriKind.Absolute, out Uri uri) == false)
            {
                return null;
            }

            var context = new AnarchyBotContext();
            ArtServiceInformation info = await context.ServiceInformations.SingleOrDefaultAsync(x => x.GuildId == guild.Id);
            if(info == null)
            {
                return null;
            }
            DiscordChannel channel = guild.GetChannel(info.ChannelId);

            if(channel == null)
            {
                return null;
            }

            HttpClient httpClient = new HttpClient();
            Stream fileStream = await httpClient.GetStreamAsync(url);
            DiscordMessage newMessage = await channel.SendFileAsync(url.Split('/').Last(), fileStream);

            string source = newMessage.Attachments.FirstOrDefault().ProxyUrl;

            if (uploadToDatabase)
            {
                var artService = new ArtService();

                Art newArt = await artService.CreateArtAsync(source);

                if (args.HasOption(_authorOption))
                {
                    var artistService = new ArtistService();
                    string authorName = args.GetOption(_authorOption).Values[0];
                    Artist author = await artistService.GetByNicknameAsync(authorName);

                    if (author == null)
                    {
                        author = await artistService.CreateArtistAsync(authorName);
                    }
                    await artService.SetAuthorAsync(newArt, author);

                    if (args.HasOption(_authorProfileOption))
                    {
                        string link = args.GetOption(_authorProfileOption).Values[0].Trim();
                        ArtistSocial social = (await artistService.GetSocials(author)).FirstOrDefault(x => x.Link == link);

                        if (social == null)
                        {
                            await artistService.AddArtistSocial(author, link);
                        }
                    }
                }

                if (args.HasOption(_tagsOption))
                {
                    string[] requestedTags = args.GetOption(_tagsOption).Values[0].Split(';');
                    var tagService = new TagService();
                    IEnumerable<Tag> realtags = await tagService.GetTagsFromNamesAsync(requestedTags, true);
                    await artService.SetTagsAsync(newArt, realtags);
                }
            }

            return source;
        }

        [Command("rnd")]
        [Description("Gets random art")]
        public async Task GetRandomArt(CommandContext ctx, params string[] arguments)
        {
            IArguments args = new PosixParser().FromString(ctx.Message.Content);

            var artService = new ArtService();
            var artistService = new ArtistService();
            var tagService = new TagService();
            Artist author = null;
            IEnumerable<string> tags = null;

            if(args.OptionsCount > 0)
            {
                if (args.HasOption(_authorOption))
                {
                    author = await artistService.GetByNicknameAsync(args.GetOption(_authorOption).Values[0]);
                }

                if (args.HasOption(_tagsOption))
                {
                    tags = args.GetOption(_tagsOption).Values[0].Split(';');
                }
            }

            Art resultArt = await artService.GetRandomArtAsync(author, tags);
            if(resultArt == null)
            {
                await ctx.Channel.SendMessageAsync("There are not arts yet ;(");
                return;
            }

            author = resultArt.Author;
            if(resultArt.Tags != null && resultArt.Tags.Count > 0)
            {
                tags = resultArt.Tags.Select(x => "`" + x.Tag.Name + "`");
            }

            var embedBuilder = new DiscordEmbedBuilder();
            embedBuilder.Color = new DiscordColor("#008888");
            embedBuilder.Timestamp = DateTimeOffset.UtcNow;

            if (author != null)
            {
                embedBuilder.AddField("Author", author.NickName);
                if (author.Profiles.Count > 0)
                {
                    embedBuilder.AddField("Profile", author.Profiles[new Random().Next(0, author.Profiles.Count)].Link);
                }
            }
            
            if (tags != null)
            {
                embedBuilder.AddField("Tags", string.Join(", ", tags));
            }

            embedBuilder.WithImageUrl(resultArt.Source);
            await ctx.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        [Command("tags")]
        [Description("Returns availible tags")]
        public async Task GetTags(CommandContext ctx, params string[] arguments)
        {
            var tagService = new TagService();

            if (await tagService.CountAsync() == 0)
            {
                await ctx.Channel.SendMessageAsync("There are no tags yet ;(");
                return;
            }

            var context = new AnarchyBotContext();
            IEnumerable<Tag> tags = await tagService.GeTagListAsync();

            var embedBuilder = new DiscordEmbedBuilder();
            embedBuilder.Color = new DiscordColor("#008888");
            embedBuilder.Timestamp = DateTimeOffset.UtcNow;
            embedBuilder.AddField("Tags", string.Join(", ", tags.Select(x => "`" + x.Name + "`")));

            await ctx.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        [Command("publish")]
        [Description(@"Publishes artwork and in beautiful view
                       Arguments:
                       -t, --title: Title of message
                       -a --author: Specifies author of artwork
                       -p, --profile: Adds link to author's social network profile
                       -f, --from: Specifies title/game/film/etc.. the character(s) on artworks belong to")]
        public async Task PublishArt([RemainingText] CommandContext ctx, params string[] arguments)
        {
            IArguments args = new PosixParser().FromString(ctx.Message.Content);

            if (args.HasOption(_sourceOption) == false && ctx.Message.Attachments.Count < 1)
            {
                await ctx.Channel.SendMessageAsync("Source is missing");
                return;
            }
            var embedBuilder = new DiscordEmbedBuilder();

            embedBuilder.Color = new DiscordColor("#008888");
            embedBuilder.Timestamp = DateTimeOffset.UtcNow;

            if (args.HasOption(_titleOption))
            {
                embedBuilder.WithTitle(string.Join(' ', args.GetOption(_titleOption).Values));
            }

            if (args.HasOption(_fromOption))
            {
                embedBuilder.AddField("From", string.Join(' ', args.GetOption(_fromOption).Values), true);
            }

            if (args.HasOption(_authorOption))
            {
                embedBuilder.AddField("Author", string.Join(' ', args.GetOption(_authorOption).Values), true);
            }

            if (args.HasOption(_authorProfileOption))
            {
                embedBuilder.AddField("Profile", string.Join(' ', args.GetOption(_authorProfileOption).Values));
            }

            if (args.HasOption(_tagsOption))
            {
                string[] values = args.GetOption(_tagsOption).Values[0].Split(';')
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .ToArray();

                embedBuilder.AddField("Tags", string.Join(", ", values));
            }

            string source = args.HasOption(_sourceOption) ?
                args.GetOption(_sourceOption).Values[0]
                : ctx.Message.Attachments.First().ProxyUrl;

            bool upload = args.HasOption(_uploadOption) && CanPostArts(ctx.Member);
            string newUrl = await UploadAsync(ctx.Guild, source, args, upload);

            if(newUrl == null)
            {
                await ctx.Channel.SendMessageAsync("Operation cannot be proceed. Make sure the server has dedicated channel for the bot.");
                return;
            }

            embedBuilder.WithImageUrl(newUrl);
            DiscordMessage message = await ctx.Channel.SendMessageAsync(embed: embedBuilder.Build());

            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsdown:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":heart:"));


            await ctx.Message.DeleteAsync();
        }

        [Command("setchannel")]
        [Description("Sets service channel for bot. (It should be hidden by anyone but the bot)")]
        public async Task SetArtsChannel(CommandContext ctx, params string[] arguments)
        {
            if(ctx.Member.Roles.Any(x => (x.Permissions & DSharpPlus.Permissions.Administrator) == DSharpPlus.Permissions.Administrator) == false)
            {
                await ctx.Channel.SendMessageAsync("Only Administrator can use this command");
                return;
            } 

            DiscordChannel channel = ctx.Message.MentionedChannels.FirstOrDefault();
            if(channel == null)
            {
                await ctx.Channel.SendMessageAsync("Mention a channel");
                return;
            }
            try
            {
                using var context = new AnarchyBotContext();
                ArtServiceInformation info = await context.ServiceInformations.FirstOrDefaultAsync(x => x.GuildId == ctx.Guild.Id);
                if (info == null)
                {
                    info = new ArtServiceInformation() { GuildId = ctx.Guild.Id, ChannelId = channel.Id };
                    context.ServiceInformations.Add(info);
                    await context.SaveChangesAsync();
                }
                else
                {
                    info.ChannelId = ctx.Channel.Id;
                    info = context.Entry(info).Entity;
                    await context.SaveChangesAsync();
                }
                await ctx.Channel.SendMessageAsync("Channel added successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
            
        }

        [Command("upload")]
        [Description("Uploads artwork")]
        public async Task UploadArtAsync(CommandContext ctx, params string[] myContent)
        {
            if (CanPostArts(ctx.Member) == false)
            {
                await ctx.Channel.SendMessageAsync("Only owner can upload arts");
                return;
            }

            IArguments args = new PosixParser().FromString(ctx.Message.Content);

            if (args.HasOption(_sourceOption) == false && ctx.Message.Attachments.Count < 1)
            {
                await ctx.Channel.SendMessageAsync("Source is missing");
                return;
            }

            string source = args.HasOption(_sourceOption) ? 
                args.GetOption(_sourceOption).Values[0] 
                : ctx.Message.Attachments.First().ProxyUrl;

            string url = await UploadAsync(ctx.Guild, source, args, true);

            if(url == null)
            {
                await ctx.Channel.SendMessageAsync("Operation cannot be proceed. Make sure the server has dedicated channel for the bot.");
            }

            await ctx.Message.DeleteAsync();
        }
    }
}