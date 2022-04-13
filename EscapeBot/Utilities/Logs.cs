using System;
using System.IO;
using DSharpPlus.Entities;

namespace EscapeBot.Utilities
{
    public static class Logs
    {
        //holds and manage the logs file to be able to see what happens
        private static string path = Bot.dataPath + "Logs/Logs.txt";
        private static string errorIdPath = Bot.dataPath + "Logs/LogErrorId.txt";
        private static int errorId;
        public static void Initialize()
        {
            if (!File.Exists(path))
            {
                //if the file doesn't exist, create it
                using (File.Create(path))
                {
                    Console.WriteLine("creating new log file");
                }
            }

            if (!File.Exists(errorIdPath))
            {
                using (File.Create(path))
                {
                    Console.WriteLine("creating new error id file");
                    File.AppendAllText(errorIdPath, "0");
                    errorId = 0;
                }
            }
            else
            {
                string data = File.ReadAllText(errorIdPath);

                if (!int.TryParse(data, out errorId))
                {
                    Console.WriteLine("Unable to read log error id : setting to 0");
                    errorId = 0;
                }
            }
        }

        public static int GetErrorId()
        {
            errorId++;

            File.WriteAllText(errorIdPath, errorId.ToString());

            return errorId;
        }

        public static void WriteLog(string log, bool displayInConsole = false, DiscordChannel sendLogTo = null)
        {
            File.AppendAllText(path, $"\n\nError id: {GetErrorId()}, date: {DateTimeOffset.Now}\n{log}");
            if (displayInConsole)
            {
                Console.WriteLine(log);
            }
            if(sendLogTo != null)
            {
                sendLogTo.SendMessageAsync(log).ConfigureAwait(false);
            }
        }

        public static void ClearLogs()
        {
            errorId = 0;

            File.WriteAllText(errorIdPath, errorId.ToString());
            File.WriteAllText(path, "");
        }

    }
}
