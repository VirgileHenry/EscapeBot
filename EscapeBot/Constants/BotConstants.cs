using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.Entities;
using System.IO;
using EscapeBot.Utilities;

namespace EscapeBot.Constants
{

    public enum Emojis
    {
        CommandDone,
        CommandError,
        WrongAnswer,
        WrongTime,
    }

    public enum gameStatus
    {
        empty,
        initialized,
        playing,
        computing
    }

    public enum botMessages
    {
        wrongChannel,
        invalidRoom,
        invalidRiddle,
        wrongAnswer,
        rightAnswer,
        alreadyPlaying,
        clueTooSoon,
        sosTooSoon,
    }

    public static class BotConstants
    {
        public static Dictionary<Emojis, DiscordEmoji> botEmojis = new Dictionary<Emojis, DiscordEmoji>()
        {
            { Emojis.CommandDone, DiscordEmoji.FromName(Bot.Client, ":white_check_mark:") },
            { Emojis.CommandError, DiscordEmoji.FromName(Bot.Client, ":warning:") },
            { Emojis.WrongAnswer, DiscordEmoji.FromName(Bot.Client, ":x:") },
            { Emojis.WrongTime, DiscordEmoji.FromName(Bot.Client, ":hourglass_flowing_sand:") },
        };

        //the game symbol is the symbol used by the bot to create roles, channels, etc
        public static char gameSymbol = '_';
        public static char commentFileSymbol = '#';

        public static Dictionary<ulong, ulong> guildsPublicCommandChannels;
        public static Dictionary<botMessages, string> botMessagesDict;

        public static int TimeInMillisBeforeClue = 24 * 3600 * 1000;
        public static int TimeInMillisBeforeSOS = 48 * 3600 * 1000;

        public static Dictionary<int, int> riddlePoints = new Dictionary<int, int>()
        {
            { 0, 100 },{ 1, 90 },{ 2, 85 },{ 3, 80 },{ 4, 76 },
            { 5, 72 },{ 6, 69 },{ 7, 66 },{ 8, 63 },{ 9, 60 },
            { 10, 58 },{ 11, 56 },{ 12, 54 },{ 13, 52 },{ 14, 51 }
        };

        public static Dictionary<string, string> riddlesAnswers = new Dictionary<string, string>()
        {
            { "!tryantichambreportesx", "antichambre-porte" },
            { "!trycellulecadenas7628", "cellule-cadenas" },
            { "!trycourformulebibbidi-bobbidi-bda", "cour-formule" },
            { "!tryescapeascenseur-21", "escape-ascenseur" },
            { "!tryfond-marinepavechaumareys", "fond-marin-epave" },
            { "!tryforet-dalguefilet25875117", "foret-dalgues-filet" },
            { "!trygarnisondevinettephare", "garnison-devinette" },
            { "!trygarnisonremedesuperonguet3000", "garnison-remede" },
            { "!trygiamethystebracelet-antichambre", "gi-amethyste" },
            { "!trymarche-nocturnepositionverdun", "marche-nocturne-position" },
            { "!trymur-deausuffocationbranchiflore", "mur-deau-suffocation" },
            { "!trypalaisambassadedominique", "palais-ambassade" },
            { "!tryroutestationflandrinvalmy", "route-station" },
            { "!tryrue-des-financesmot-de-passe4-2-4", "rue-des-finances-mot-de-passe" },
            { "!trysalle-des-alliesoriflammemspk", "salle-des-allies-oriflamme" },
            { "!trysalle-du-tresorvitrinelicorne", "salle-du-tresor-vitrine" },
            { "!trysalonechiquierr6", "salon-echiquier" },
            { "!trytempleadresse46-avenue-felix-viallet", "temple-adresse" },
            { "!trytrone-du-tridenthymne1000-poseidon", "trone-du-trident-hymne" },
        };
        public static void Initialize()
        {
            guildsPublicCommandChannels = new Dictionary<ulong, ulong>();
            foreach(string dirPath in Directory.GetDirectories(Bot.dataPath + "Servers", "*", SearchOption.TopDirectoryOnly))
            {
                //TODO : load in memory, for each server, the dedicated player inputs channel
                string dirName = new DirectoryInfo(dirPath).Name;
                if(dirName != ".Template")
                {
                    if(!ulong.TryParse(dirName, out ulong guildId))
                    {
                        Logs.WriteLog($"Unable to interpret as ulong for guild id : {dirName}");
                    }
                    else
                    {
                        ulong channelId = DiscordUtilities.GetPublicCommandChannelId(guildId);
                        guildsPublicCommandChannels.Add(guildId, channelId);
                    }
                }
            }
            botMessagesDict = new Dictionary<botMessages, string>();
            botMessagesDict.Add(botMessages.wrongChannel, File.ReadAllText(Bot.dataPath + "Messages/wrongChannelMessage.txt"));
            botMessagesDict.Add(botMessages.invalidRoom, File.ReadAllText(Bot.dataPath + "Messages/invalidRoomMessage.txt"));
            botMessagesDict.Add(botMessages.wrongAnswer, File.ReadAllText(Bot.dataPath + "Messages/wrongAnswerMessage.txt"));
            botMessagesDict.Add(botMessages.rightAnswer, File.ReadAllText(Bot.dataPath + "Messages/rightAnswerMessage.txt"));
            botMessagesDict.Add(botMessages.invalidRiddle, File.ReadAllText(Bot.dataPath + "Messages/invalidRiddleMessage.txt"));
            botMessagesDict.Add(botMessages.alreadyPlaying, File.ReadAllText(Bot.dataPath + "Messages/playerAlreadyPlaying.txt"));
            botMessagesDict.Add(botMessages.clueTooSoon, File.ReadAllText(Bot.dataPath + "Messages/clueAskedTooSoon.txt"));
            botMessagesDict.Add(botMessages.sosTooSoon, File.ReadAllText(Bot.dataPath + "Messages/sosAskedTooSoon.txt"));

        }
    }
}
