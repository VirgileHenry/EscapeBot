using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using EscapeBot.Constants;

namespace EscapeBot.Utilities
{
    public static class FileUtilities
    {
        public static void CloneDirectory(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        public static void SetGameInfo(ulong serverId, string info, string value)
        {
            if (File.Exists(Bot.dataPath + $"Servers/{serverId}/gameInfo.txt"))
            {
                string[] lines = File.ReadAllLines(Bot.dataPath + $"Servers/{serverId}/gameInfo.txt");

                for(int i = 0; i < lines.Length; i++)
                {
                    if (lines[i] != "" && lines[i][0] != BotConstants.commentFileSymbol)
                    {
                        string[] data = lines[i].Split(":");

                        if(data[0] == info)
                        {
                            lines[i] = $"{data[0]}:{value}";
                        }
                    }
                }

                File.WriteAllLines(Bot.dataPath + $"Servers/{serverId}/gameInfo.txt", lines);
            }
        }

        public static string GetGameInfo(ulong serverId, string info)
        {
            if (File.Exists(Bot.dataPath + $"Servers/{serverId}/gameInfo.txt"))
            {
                string[] lines = File.ReadAllLines(Bot.dataPath + $"Servers/{serverId}/gameInfo.txt");

                foreach (string line in lines)
                {
                    if (line != "" && line[0] != BotConstants.commentFileSymbol)
                    {
                        string[] data = line.Split(":");

                        if (data[0] == info)
                        {
                            return data[1];
                        }
                    }
                }
            }

            return "";
        }

        public static void SetGameStatus(ulong guildId, gameStatus status)
        {
            SetGameInfo(guildId, "gameStatus", status.ToString());
        }

        public static gameStatus GetGameStatus(ulong guildId)
        {
            string statusStr = GetGameInfo(guildId, "gameStatus");

            if(!Enum.TryParse(statusStr, out gameStatus status))
            {
                Logs.WriteLog($"Unable to read game status : {statusStr}. for safety reasons, setting it back to 'empty'.");
                SetGameStatus(guildId, gameStatus.empty);
                return gameStatus.empty;
            }

            return status;
        }
    }
    
}
