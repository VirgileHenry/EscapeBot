using System.IO;
using System.Collections.Generic;
using DSharpPlus.Entities;
using EscapeBot.Constants;
using System.Threading.Tasks;
using System;

namespace EscapeBot.Utilities
{
    public static class DiscordUtilities
    {
        public static ulong GetGameCategoryId(ulong serverId)
        {
            if (Directory.Exists(Bot.dataPath + $"Servers/{serverId}"))
            {
                string[] data = File.ReadAllLines(Bot.dataPath + $"Servers/{serverId}/gameInfo.txt");
                foreach (string line in data)
                {
                    if (line != "" && line[0] != BotConstants.commentFileSymbol)
                    {
                        //usefull line to read
                        string[] lineData = line.Split(":");
                        if (lineData[0] == "gameCategoryId")
                        {
                            //we find the usefull info
                            if (!ulong.TryParse(lineData[1], out ulong channelId))
                            {
                                Logs.WriteLog($"Unable to parse to ulong : {lineData[1]}, at {Bot.dataPath}Servers/{serverId}/gameInfo.txt");
                            }
                            return channelId;
                        }
                    }
                }
                return 0;
            }
            else
            {
                return 0;
            }
        }

        public static ulong GetPlayerAnswerCategoryId(ulong serverId)
        {
            if (Directory.Exists(Bot.dataPath + $"Servers/{serverId}"))
            {
                string[] data = File.ReadAllLines(Bot.dataPath + $"Servers/{serverId}/gameInfo.txt");
                foreach (string line in data)
                {
                    if (line != "" && line[0] != '#')
                    {
                        //usefull line to read
                        string[] lineData = line.Split(":");
                        if (lineData[0] == "playerAnswerCategoryId")
                        {
                            //we find the usefull info
                            if (!ulong.TryParse(lineData[1], out ulong channelId))
                            {
                                Logs.WriteLog($"Unable to parse to ulong : {lineData[1]}, at {Bot.dataPath}Servers/{serverId}/gameInfo.txt");
                            }
                            return channelId;
                        }
                    }
                }
                return 0;
            }
            else
            {
                return 0;
            }
        }

        public static ulong GetPublicCommandChannelId(ulong serverId)
        {
            if (Directory.Exists(Bot.dataPath + $"Servers/{serverId}"))
            {
                string[] data = File.ReadAllLines(Bot.dataPath + $"Servers/{serverId}/gameInfo.txt");
                foreach (string line in data)
                {
                    if (line != "" && line[0] != '#')
                    {
                        //usefull line to read
                        string[] lineData = line.Split(":");
                        if (lineData[0] == "publicCommandChannelId")
                        {
                            //we find the usefull info
                            if (!ulong.TryParse(lineData[1], out ulong channelId))
                            {
                                Logs.WriteLog($"Unable to parse to ulong : {lineData[1]}, at {Bot.dataPath}Servers/{serverId}/gameInfo.txt");
                            }
                            return channelId;
                        }
                    }
                }
                return 0;
            }
            else
            {
                return 0;
            }
        }

        public static ulong GetPlayerAnswerChannelId(ulong serverId, ulong memberId)
        {
            if (Directory.Exists(Bot.dataPath + $"Servers/{serverId}/Players/{memberId}"))
            {
                string data = File.ReadAllText(Bot.dataPath + $"Servers/{serverId}/Players/{memberId}/channel.txt");
                ulong channelId = 0;
                //we find the usefull info
                if (!ulong.TryParse(data, out channelId))
                {
                    Logs.WriteLog($"Unable to parse to ulong : {data}, at {Bot.dataPath}Servers/{serverId}/Players/{memberId}/channel.txt");
                }
                return channelId;
            }
            else
            {
                return 0;
            }
        }

        public static ulong GetRoleId(string roleName, ulong guildId)
        {
            ulong roleId = 0;
            foreach (string line in File.ReadAllLines(Bot.dataPath + $"Servers/{guildId}/GameData/roomsRoles.txt"))
            {
                string[] data = line.Split(":");
                if (data[1] == roleName)
                {
                    roleId = ulong.Parse(data[2]);
                }
            }
            return roleId;
        }

        public static ulong[] GetAllGuilds()
        {
            List<ulong> result = new List<ulong>();
            
            foreach (string dirPath in Directory.GetDirectories(Bot.dataPath + "Servers", "*", SearchOption.TopDirectoryOnly))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                
                if(dirInfo.Name != ".Template")
                {
                    if(!ulong.TryParse(dirInfo.Name, out ulong guildId))
                    {
                        Logs.WriteLog($"Unable to interpret folder name '{dirInfo.Name}' as guild id (ulong).");
                    }
                    else
                    {
                        result.Add(guildId);
                    }
                }
            }

            return result.ToArray();
        }

        public static async Task DisplayFolderInChannel(string path, DiscordChannel channel)
        {
            if(!Directory.Exists(path))
            {
                Logs.WriteLog($"'enigme' directory does not exists at path {path}", true);
                return;
            }
            
            Dictionary<int, string> contentPaths = new Dictionary<int, string>();

            int minKey = int.MaxValue;
            int maxKey = int.MinValue;

            //we start by making the dictionnary with order and path, then send it

            foreach (string filePath in Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly))
            {               
                if(!int.TryParse(Path.GetFileNameWithoutExtension(filePath), out int fileOrder))
                {
                    Logs.WriteLog($"Unable to send content to channel : {filePath} is not parsable to int !");
                    continue;
                }
                if(contentPaths.ContainsKey(fileOrder))
                {
                    Logs.WriteLog($"Unable to determine order for context at {filePath} : order already exists");
                }
                else
                {
                    contentPaths.Add(fileOrder, filePath);
                }

                if(fileOrder < minKey)
                {
                    minKey = fileOrder;
                }
                if(fileOrder > maxKey)
                {
                    maxKey = fileOrder;
                }
            }

            for(int i = minKey; i <= maxKey; i++)
            {
                if(contentPaths.ContainsKey(i))
                {
                    string extension = Path.GetExtension(contentPaths[i]);

                    DiscordMessageBuilder message;

                    switch (extension)
                    {
                        case ".txt":
                            string fileContent = File.ReadAllText(contentPaths[i]);
                            if(fileContent != "")
                            {
                                message = new DiscordMessageBuilder().WithContent(fileContent);
                                await message.SendAsync(channel);
                            }
                            break;
                        default:
                            using (var fs = new FileStream(contentPaths[i], FileMode.Open, FileAccess.Read))
                            {
                                message = new DiscordMessageBuilder().WithFile(fs);
                                await message.SendAsync(channel);
                            }
                            break;
                    }
                }
            }
        }

        public static ulong GetRoomChannelId(ulong guildId, string roomName)
        {
            string path = Bot.dataPath + $"Servers/{guildId}/GameData/Rooms/{roomName}/channelId.txt";

            if(!File.Exists(path))
            {
                Logs.WriteLog($"Unable to read room id : cannot access room file of room {roomName}");
                return 0;
            }

            string data = File.ReadAllText(path);

            if(!ulong.TryParse(data, out ulong result))
            {
                Logs.WriteLog($"Unable to read room id : cannot translate to ulong {data}");
                return 0;
            }

            return result;
        }

    }
}
