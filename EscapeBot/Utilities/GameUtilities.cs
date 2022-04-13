using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.IO;
using EscapeBot.Constants;

namespace EscapeBot.Utilities
{
    public static class GameUtilities
    {
        public static async Task CreatePlayer(DiscordMember member, DiscordGuild guild, DiscordChannel answerCategory, DiscordRole startRole)
        {
            // assert player does not exist
            if(Directory.Exists(Bot.dataPath + $"Servers/{guild}/Players/{member.Id}"))
            {
                await member.SendMessageAsync(BotConstants.botMessagesDict[botMessages.alreadyPlaying]);
                return;
            }
            
            
            //give the start role to the player
            await member.GrantRoleAsync(startRole);
            //create the player a answer channel
            DiscordOverwriteBuilder permissionsBuilder = new DiscordOverwriteBuilder(member);
            permissionsBuilder.Allow(DSharpPlus.Permissions.AccessChannels);
            permissionsBuilder.Allow(DSharpPlus.Permissions.SendMessages);
            DiscordChannel answerChannel = await guild.CreateChannelAsync(BotConstants.gameSymbol + $"answer-channel-of-{member.DisplayName}",
                DSharpPlus.ChannelType.Text, answerCategory,
                overwrites: new DiscordOverwriteBuilder[1] { permissionsBuilder });
            //create the player folder
            Directory.CreateDirectory(Bot.dataPath + $"Servers/{guild.Id}/Players/{member.Id}");
            FileUtilities.CloneDirectory(Bot.dataPath + $"Servers/{guild.Id}/Players/.Template", Bot.dataPath + $"Servers/{guild.Id}/Players/{member.Id}");
            //write the room channel to the player data
            File.WriteAllText(Bot.dataPath + $"Servers/{guild.Id}/Players/{member.Id}/channel.txt", answerChannel.Id.ToString());
            //write the first event
            File.WriteAllText(Bot.dataPath + $"Servers/{guild.Id}/Players/{member.Id}/historic.txt", $"{DateTimeOffset.Now} : Player started entered the game");
        }

        public static async Task RevealDailyRiddle(ulong guildId)
        {
            // get and update the daily riddle(s) in the file
            // send it to the proper channel
            //start by setting the game on "computing" state

            string[] rooms = File.ReadAllLines(Bot.dataPath + $"Servers/{guildId}/GameData/roomOrder.txt");
            for(int i = 0; i < rooms.Length; i++)
            {
                string room = rooms[i];
                string[] roomInfo = room.Split(":");
                if(!bool.TryParse(roomInfo[1], out bool isRoomOpen))
                {
                    Logs.WriteLog($"Unable to reveal daily room : {roomInfo[1]} is not parsable to bool !");
                    return;
                }

                if(!isRoomOpen)
                {                   
                    ulong channelId = DiscordUtilities.GetRoomChannelId(guildId, roomInfo[0]);
                    DiscordChannel roomChannel = await Bot.Client.GetChannelAsync(channelId).ConfigureAwait(true);

                    string path = Bot.dataPath + $"Servers/{guildId}/GameData/Rooms/{roomInfo[0]}";

                    // check room exists
                    if (!File.Exists(path + "/channelId.txt"))
                    {
                        Logs.WriteLog($"Unable to update room content : room '{roomInfo[0]}' does not exists !", false, roomChannel);
                        return;
                    }

                    // reveal the room
                    await DiscordUtilities.DisplayFolderInChannel(path + "/enigme/", roomChannel);

                    //write the room reveal time
                    File.WriteAllText(Bot.dataPath + $"Servers/{guildId}/GameData/Rooms/{roomInfo[0]}/revealTime.txt", DateTimeOffset.Now.ToString());


                    // first room locked : open it and write it down, then skip
                    rooms[i] = $"{roomInfo[0]}:{true}";
                    //write the data down
                    File.WriteAllLines(Bot.dataPath + $"Servers/{guildId}/GameData/roomOrder.txt", rooms);

                    return;
                }
                
            }
        }

        public static async Task EreaseDiscordGameElements(ulong guildId, DiscordChannel gameCategory)
        {
            //delete rooms channels
            foreach (DiscordChannel channel in gameCategory.Children)
            {
                await channel.DeleteAsync();
            }
            //delete answer channels
            ulong categoryId = DiscordUtilities.GetPlayerAnswerCategoryId(guildId);
            if (categoryId == 0)
            {
                Logs.WriteLog("Answer category have not been set (at data/Servers/<serverName>/gameInfo.txt).");
                return;
            }
            DiscordChannel answerCategory = await Bot.Client.GetChannelAsync(categoryId);
            foreach (DiscordChannel channel in answerCategory.Children)
            {
                await channel.DeleteAsync();
            }
            //delete roles
            DiscordGuild guild = await Bot.Client.GetGuildAsync(guildId);
            foreach (KeyValuePair<ulong, DiscordRole> role in guild.Roles)
            {
                if (role.Value.Name[0] == BotConstants.gameSymbol)
                {
                    await role.Value.DeleteAsync();
                }
            }
        }

        public static void WriteMemberHistoric(DiscordGuild guild, DiscordMember member, string write)
        {
            if(File.Exists($"{Bot.dataPath}Servers/{guild.Id}/Players/{member.Id}/historic.txt"))
            {
                File.AppendAllLines($"{Bot.dataPath}Servers/{guild.Id}/Players/{member.Id}/historic.txt",
                    new string[1] { write });
            }
            else
            {
                Logs.WriteLog("Unable to write player stats : no historic file found.");
            }
        }

        public static void WriteMemberActionTime(string path, DateTimeOffset time)
        {
            //check player don't have a better time than this one 
            if(!File.Exists(path))
            {
                File.WriteAllText(path, time.ToString());
            }
        }


        public static void ComputeMemberPoints(ulong guildId)
        {
            Dictionary<ulong, int> scores = new Dictionary<ulong, int>();
            
            //for each room, for each riddle, create the scoreboard
            string roomsPath = Bot.dataPath + $"Servers/{guildId}/GameData/Rooms/";
            foreach(string roomDir in Directory.GetDirectories(roomsPath, "*", SearchOption.TopDirectoryOnly))
            {
                string roomName = roomDir.Split("/")[roomDir.Split("/").Length - 1];

                if (roomName == ".Template")
                {
                    continue;
                }

                //loop for every riddle
                string riddlePath = $"{roomDir}/scoreboard/";
                foreach (string riddleDir in Directory.GetDirectories(riddlePath, "*", SearchOption.TopDirectoryOnly))
                {
                    string riddleName = riddleDir.Split("/")[riddleDir.Split("/").Length - 1];

                    //get every player that solve it and sort the array
                    List<ulong> players = new List<ulong>();

                    foreach(string player in Directory.GetDirectories(Bot.dataPath + $"Servers/{guildId}/Players/"))
                    {
                        string pathToRiddle = player + $"/RiddleResolveTimes/{roomName.ToLower()}-{riddleName.ToLower()}.txt";

                        if (File.Exists(pathToRiddle))
                        {
                            string playerId = player.Split("/")[player.Split("/").Length - 1];

                            if(!ulong.TryParse(playerId, out ulong id))
                            {
                                Logs.WriteLog($"Unable to parse player id to ulong : {playerId}. Won't be able to add player points !");
                                continue;
                            }

                            if(!players.Contains(id))
                            {
                                players.Add(id);
                                //should always be true, just to be sure no doubles
                            }
                        }
                    }

                    //now we sort the player array with their resolve time

                    players = SortPlayersByTime(players, guildId, roomName, riddleName);


                    //add points to the players
                    for(int i = 0; i < players.Count; i++)
                    {
                        string resolveTimePath = Bot.dataPath + $"Servers/{guildId}/Players/{players[i]}/RiddleResolveTimes/{roomName.ToLower()}-{riddleName.ToLower()}.txt";

                        int scoreToAdd;
                        if(i < 15)
                        {
                            scoreToAdd = BotConstants.riddlePoints[i];
                        }
                        else
                        {
                            scoreToAdd = 50;
                        }

                        //check for clues
                        string cluePath = Bot.dataPath + $"Servers/{guildId}/Players/{players[i]}/Clues/{roomName.ToLower()}-{riddleName.ToLower()}.txt";
                        if (File.Exists(cluePath))
                        {
                            //check player asked for clue before solving it
                            if (!DateTimeOffset.TryParse(File.ReadAllText(cluePath), out DateTimeOffset clueTime))
                            {
                                Logs.WriteLog($"Unable to read date time of clue for player {File.ReadAllText(cluePath)}");
                            }
                            if (!DateTimeOffset.TryParse(File.ReadAllText(resolveTimePath), out DateTimeOffset resolveTime))
                            {
                                Logs.WriteLog($"Unable to read date time of resolve time for player {File.ReadAllText(resolveTimePath)}");
                            }

                            if(DateTimeOffset.Compare(clueTime, resolveTime) < 0)
                            {
                                //player solved riddle after clue
                                scoreToAdd /= 2;
                            }
                        }
                        //check for sos
                        string sosPath = Bot.dataPath + $"Servers/{guildId}/Players/{players[i]}/sos/{roomName.ToLower()}-{riddleName.ToLower()}.txt";
                        if (File.Exists(sosPath))
                        {
                            //check player asked for sos before solving it
                            if (!DateTimeOffset.TryParse(File.ReadAllText(sosPath), out DateTimeOffset sosTime))
                            {
                                Logs.WriteLog($"Unable to read date time of clue for player {File.ReadAllText(sosPath)}");
                            }
                            if (!DateTimeOffset.TryParse(File.ReadAllText(resolveTimePath), out DateTimeOffset resolveTime))
                            {
                                Logs.WriteLog($"Unable to read date time of resolve time for player {File.ReadAllText(resolveTimePath)}");
                            }

                            if (DateTimeOffset.Compare(sosTime, resolveTime) < 0)
                            {
                                //player solved riddle after clue
                                scoreToAdd = 0;
                            }
                        }

                        if(scores.ContainsKey(players[i]))
                        {
                            scores[players[i]] += scoreToAdd;
                        }
                        else
                        {
                            scores.Add(players[i], scoreToAdd);
                        }

                    }

                }
            }

            //write down the scores
            List<string> lines = new List<string>();

            foreach(KeyValuePair<ulong, int> playerScore in scores)
            {
                lines.Add($"{playerScore.Key}:{playerScore.Value}");
            }

            File.WriteAllLines(Bot.dataPath + $"Servers/{guildId}/Players/score.txt", lines.ToArray());
        }

        public static List<ulong> SortPlayersByTime(List<ulong> players, ulong guildId, string roomName, string riddleName)
        {
            //insert sort
            for (int i = 1; i < players.Count; i++)
            {
                //store i player as x player
                // x <- T[i]
                //get data on x to compare to others
                ulong x = players[i];
                string pathToXPlayer = Bot.dataPath + $"Servers/{guildId}/Players/{players[i]}/RiddleResolveTimes/{roomName.ToLower()}-{riddleName.ToLower()}.txt";
                if (!DateTimeOffset.TryParse(File.ReadAllText(pathToXPlayer), out DateTimeOffset xplayerTime))
                {
                    Logs.WriteLog($"Unable to read riddle resolve time from player {players[i]}");
                    continue;
                }

                //initialize j at i
                int j = i;

                //get time of player at ---> j-1 <---
                string pathToJPlayer = Bot.dataPath + $"Servers/{guildId}/Players/{players[j - 1]}/RiddleResolveTimes/{roomName.ToLower()}-{riddleName.ToLower()}.txt";
                if (!DateTimeOffset.TryParse(File.ReadAllText(pathToJPlayer), out DateTimeOffset jplayerTime))
                {
                    Logs.WriteLog($"Unable to read riddle resolve time from player {players[j - 1]}");
                    continue;
                }

                int comparator = DateTimeOffset.Compare(jplayerTime, xplayerTime);

                //compare < 0 => jplayerTime est avant xplayerTime
                while (j > 0 && comparator > 0)
                {
                    players[j] = players[j - 1];
                    j -= 1;

                    //because we are loading j-1 player data (to compare), we must assert we are not at the begining
                    if(j == 0) { continue; }
                    //update time for j player
                    pathToJPlayer = Bot.dataPath + $"Servers/{guildId}/Players/{players[j-1]}/RiddleResolveTimes/{roomName.ToLower()}-{riddleName.ToLower()}.txt";
                    if (!DateTimeOffset.TryParse(File.ReadAllText(pathToJPlayer), out jplayerTime))
                    {
                        Logs.WriteLog($"Unable to read riddle resolve time from player {players[j-1]}");
                        break;
                    }

                    comparator = DateTimeOffset.Compare(jplayerTime, xplayerTime);
                }

                players[j] = x;
            }

            return players;
        }

        public static async Task DisplayLeaderBoard(ulong guildId)
        {
            DiscordGuild guild = await Bot.Client.GetGuildAsync(guildId);

            //get the discord channel
            string channelIdString = FileUtilities.GetGameInfo(guildId, "publicDisplayChannelId");

            if(!ulong.TryParse(channelIdString, out ulong channelId))
            {
                throw new Exception($"Unable to read ulong for public display channel from string : {channelIdString}");
            }

            DiscordChannel channel = guild.GetChannel(channelId);
            
            string message = "====================\n\n";

            string[] playersScores = File.ReadAllLines(Bot.dataPath + $"Servers/{guild.Id}/Players/score.txt");

            //sort the players
            //put them all in a big dictionnary, with score as keys
            Dictionary<int, List<string>> scores = new Dictionary<int, List<string>>();
            //keep track of the biggest score
            int biggestScore = 0;

            foreach(string playerScoreData in playersScores)
            {
                string[] data = playerScoreData.Split(":");
                if (!ulong.TryParse(data[0], out ulong playerId))
                {
                    throw new Exception($"Unable to read player id of type ulong from string : '{data[0]}'");
                }
                if (!int.TryParse(data[1], out int playerScore))
                {
                    throw new Exception($"Unable to read player id of type ulong from string : '{data[1]}'");
                }

                try
                {
                    DiscordMember player = await guild.GetMemberAsync(playerId);
                    if (scores.ContainsKey(playerScore))
                    {
                        scores[playerScore].Add(player.DisplayName);
                    }
                    else
                    {
                        scores.Add(playerScore, new List<string>() { player.DisplayName });
                    }
                }
                catch (Exception e)
                {
                    Logs.WriteLog($"Unable to find player in server with id {playerId}");
                }


                
                if(playerScore > biggestScore) { biggestScore = playerScore; }
            }


            // now we can display everyboy !
            int rank = 1;

            for (int i = biggestScore; i > 0; i -= 1)
            {
                if (scores.ContainsKey(i))
                {
                    
                    message += $"{rank}) ";
                    for(int j = 0; j < scores[i].Count; j++)
                    {
                        message += $"{scores[i][j]} ";

                        if (j != scores[i].Count - 1) { message += " / "; }
                    }
                    message += $": {i}\n";
                    rank++;
                }
            }

            message += "\n====================";

            await channel.SendMessageAsync(message);
        }

        public static async Task RecomputeAllRiddleResolveTime(ulong guildId)
        {
            //very heavy method to get again all riddle resolve times

            //get usefull stuff
            DiscordGuild guild = await Bot.Client.GetGuildAsync(guildId);

            //loop through every player, for each one of them get all messages in channel,
            //loop through them to look for the bot validation message
            string pathToPlayer = Bot.dataPath + $"Servers/{guildId}/Players";
            foreach(string playerDir in Directory.GetDirectories(pathToPlayer, "*", SearchOption.TopDirectoryOnly))
            {
               
                string dirName = playerDir.Split("\\")[playerDir.Split("\\").Length - 1];

                if(dirName == ".Template")
                {
                    continue;
                }

                if(!ulong.TryParse(dirName, out ulong playerId))
                {
                    throw new Exception($"Unable to interpret folder as player : cannot parse string {dirName} as ulong");
                }

                string pathToResolveTime = playerDir + "/RiddleResolveTimes/";

                //delete all files
                foreach(string filePath in Directory.GetFiles(pathToResolveTime, "*", SearchOption.TopDirectoryOnly))
                {
                    File.Delete(filePath);
                }

                //loop throught player messages to get resolve times
                DiscordChannel playerChannel = guild.GetChannel(DiscordUtilities.GetPlayerAnswerChannelId(guildId, playerId));

                if(playerChannel == null)
                {
                    continue;
                }
                
                string playerName = "";

                try
                {
                    DiscordMember member = await guild.GetMemberAsync(playerId);
                    playerName = member.DisplayName;
                }
                catch
                {
                    continue;
                }

                //get all messages
                IReadOnlyList<DiscordMessage> messages = await playerChannel.GetMessagesAsync(100);

                int messagesCount = 0;
                List<string> allMessagesToWrite = new List<string>();

                for(ulong j = 0; j < 50; j++)
                {
                    ulong lastMessageId = 0;
                    
                    for (int i = messages.Count - 1; i >= 0; i -= 1)
                    {
                        //check the message
                        string text = messages[i].Content;
                        string[] data = text.Split(" ");

                        for (int k = 0; k < data.Length; k++)
                        {
                            data[k] = data[k].ToLower();
                        }

                        string possibleKey = "";
                        foreach(string s in data)
                        {
                            possibleKey += s;
                        }

                        if (BotConstants.riddlesAnswers.ContainsKey(possibleKey))
                        {
                            string memberPath = Bot.dataPath + $"Servers/{guildId}/Players/{playerId}/RiddleResolveTimes/{BotConstants.riddlesAnswers[possibleKey]}.txt";

                            GameUtilities.WriteMemberActionTime(memberPath, messages[i].Timestamp);

                            Console.WriteLine($"====> Found player correct answer : {text} for player {playerName}");
                        }
                        

                        messagesCount++;

                        Console.WriteLine($"fetching message num {messagesCount} for player {playerName} (content : {text})");
      
                    }

                    if(messages.Count > 0)
                    {
                        lastMessageId = messages[messages.Count - 1].Id;
                        messages = await playerChannel.GetMessagesBeforeAsync(lastMessageId, 100);
                    }
                    else
                    {
                        break;
                    }

                }

                
            }

            Console.WriteLine("================ DONE ==================");
        }
    }
}
