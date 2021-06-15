namespace AnarchyBot
{
    public class Program
    {
        public static async System.Threading.Tasks.Task Main(string[] args)
        {
            var bot = new BotMain();
            await bot.RunAsync();
        }
    }
}