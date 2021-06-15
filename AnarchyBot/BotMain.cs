using AnarchyBot.Commands;
using AnarchyBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace AnarchyBot
{
    public class AnarchyBotConfiguration
    {
        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public string DiscordToken { get; set; }
    }

    public class BotMain
    {
        public static AnarchyBotConfiguration BotConfiguration { get; private set; }

        public DiscordClient Client { get; private set; }
        public DiscordConfiguration Configuration { get; }
        public string Prefix { get; set; } = "a!";

        public BotMain()
        {
            var configFile = new FileInfo("config.json");
            if(configFile.Exists == false)
            {
                Console.WriteLine("Configuration file is required");
                Environment.Exit(0);
                return;
            }

            using (StreamReader configStream = configFile.OpenText()) 
            {
                string json = configStream.ReadToEnd();
                BotConfiguration = JsonConvert.DeserializeObject<AnarchyBotConfiguration>(json);
            }

            Configuration = new DiscordConfiguration
            {
                Token = BotConfiguration.DiscordToken,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Debug
            };
        }

        private IServiceProvider GetServiceProvider()
        {
            //Dependency injection provider
            var collection = new ServiceCollection();

            collection.AddSingleton<Random>();
            collection.AddSingleton<ChatBotService>();

            return collection.BuildServiceProvider();
        }

        private async Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            Console.WriteLine($"AnarchyBot is ready");
            await Task.CompletedTask;
        }

        private async Task OnCommandErrored(CommandsNextExtension cnext, CommandErrorEventArgs e)
        {
            if (e.Exception is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                return;

            e.Context.Client.Logger.LogError(e.Exception, "Exception occurred during {0}'s invocation of '{1}'", e.Context.User.Username, e.Context.Command.QualifiedName);

            var exs = new List<Exception>();
            if (e.Exception is AggregateException ae)
                exs.AddRange(ae.InnerExceptions);
            else
                exs.Add(e.Exception);

            foreach (var ex in exs)
            {
                if (ex is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                    return;

                var embed = new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = "An exception occurred when executing a command",
                    Description = $"`{e.Exception.GetType()}` occurred when executing `{e.Command.QualifiedName}`.",
                    Timestamp = DateTime.UtcNow
                };
                embed.WithFooter(Client.CurrentUser.Username, Client.CurrentUser.AvatarUrl)
                    .AddField("Message", ex.Message);
                await e.Context.RespondAsync(embed: embed.Build());
            }
        }

        private async Task OnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            Console.WriteLine($"{e.Context.Guild.Name}/#{e.Context.Channel.Name.TrimStart("Channel".ToCharArray())}/{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'");
            await Task.CompletedTask;
        }

        private async Task OnGuildAvailable(DiscordClient client, GuildCreateEventArgs e)
        {
            client.Logger.LogInformation("Guild available: '{0}' " + e.Guild.Id, e.Guild.Name);
            await Task.CompletedTask;
        }

        private async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            Console.WriteLine($"{e.Guild.Name}/#{e.Channel.Name.TrimStart("Channel".ToCharArray())}/{e.Author.Username}: {e.Message.Content}");
            await Task.CompletedTask;
        }

        public async Task RunAsync()
        {
            //Creating Bot client
            Client = new DiscordClient(Configuration);
            Client.Ready += OnClientReady;
            Client.MessageCreated += OnMessageCreated;
            Client.GuildAvailable += OnGuildAvailable;

            //Configuring Commands
            CommandsNextExtension commandsProvider = Client.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { Prefix },
                Services = GetServiceProvider(),
                CaseSensitive = false,
                EnableMentionPrefix = false
            });
#if DEBUG
            commandsProvider.CommandErrored += OnCommandErrored;
            commandsProvider.CommandExecuted += OnCommandExecuted;
#endif

            commandsProvider.RegisterCommands<AnarchyModule>();
            commandsProvider.RegisterCommands<ArtsModule>();
            commandsProvider.RegisterCommands<AottgChatBotModule>();

            //Configuring interactivity
            InteractivityExtension interact = Client.UseInteractivity(new InteractivityConfiguration()
            {
            });

            await Client.ConnectAsync(new DiscordActivity("Creating instability", ActivityType.Playing));

            //Infinite await
            await Task.Delay(-1);
        }
    }
}