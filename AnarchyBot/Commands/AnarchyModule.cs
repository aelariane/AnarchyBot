using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace AnarchyBot.Commands
{
    public class AnarchyModule : BaseCommandModule
    {
        [Command("github")]
        [Description("Gets link to Anarchy Mod repository on Github")]
        public async Task GetGithubLink(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("https://github.com/aelariane/Anarchy");
        }

        [Command("link")]
        [Description("Gets Anarchy Mod launcher link")]
        public async Task GetLauncherLink(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Get the mod here: https://www.dropbox.com/s/6xdlszjdc6c6a45/Anarchy.exe?dl=1");
        }

        [Command("link32")]
        [Description("Gets Anarchy Mod launcher link for 32 bit system")]
        public async Task GetLauncherLink32(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Get the mod here: https://www.dropbox.com/s/4323lbsi4nvxunq/Anarchy32.exe?dl=1");
        }
    }
}