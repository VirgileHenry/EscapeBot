using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EscapeBot.Commands;
using EscapeBot.Utilities;
using EscapeBot.Constants;

namespace EscapeBot
{
    public class Bot
    {
        //actual bot

        public static DiscordClient Client { get; private set; }
        public CommandsNextExtension Commands { get; private set; }
        public static string dataPath = "D:/dev/code/discord/EscapeBot/Data/";

        public async Task RunAsync()
        {
            //get the configuration of the bot in the json file
            string json = string.Empty;

            using (FileStream fs = File.OpenRead(dataPath + "Json/config.json"))
            using (StreamReader sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = sr.ReadToEnd();

            ConfigJson configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            //create configurations to start the bot
            var config = new DiscordConfiguration
            {
                Token = configJson.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
            };

            var commandsConfig = new CommandsNextConfiguration
            {
                StringPrefixes = new string[] { configJson.prefix },
                EnableDms = false,
                EnableMentionPrefix = true,
                DmHelp = false,
                EnableDefaultHelp = true,
                IgnoreExtraArguments = false,
                CaseSensitive = false,
            };

            //instantiate the client,
            Client = new DiscordClient(config);
            Client.Ready += OnClientReady;

            //allow to use commands
            Commands = Client.UseCommandsNext(commandsConfig);
            Commands.RegisterCommands<BasicCommands>();
            Commands.RegisterCommands<MasterCommands>();
            //Commands.RegisterCommands<PlayerCommands>();

            //init data here
            Logs.Initialize();
            BotConstants.Initialize();
            TimeManager.Initialize();

            //connect the client
            await Client.ConnectAsync();
            //hold the programs by waiting indefinitly, or it will close
            await Task.Delay(-1);
        }

        private Task OnClientReady(object sender, ReadyEventArgs e)
        {
            //do nothing
            return Task.CompletedTask;
        }



    }
}