using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;



namespace EscapeBot.Commands
{
    public class BasicCommands : BaseCommandModule
    {
        [Command("ping")]
        [Description("Returns pong")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.Message.RespondAsync("Pong !").ConfigureAwait(false);
        }

    }
}
