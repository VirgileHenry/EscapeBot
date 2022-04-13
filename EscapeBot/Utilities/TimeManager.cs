using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using EscapeBot.Utilities;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace EscapeBot
{
    public static class TimeManager
    {

        public static Dictionary<ulong, GuildTimeManager> guilds;

        public static void Initialize()
        {
            guilds = new Dictionary<ulong, GuildTimeManager>();
            
            foreach(ulong guildId in DiscordUtilities.GetAllGuilds())
            {
                guilds.Add(guildId, new GuildTimeManager(guildId));
            }
        }


    }


    public class GuildTimeManager
    {
        private ulong guildId;
        private Timer mainTimer;
        private Timer primaryTimer;

        public GuildTimeManager(ulong guildId)
        {
            this.guildId = guildId;
            
            int nowInMilli = DateTimeOffset.Now.Hour * 60 * 60 * 1000 + DateTimeOffset.Now.Minute * 60 * 1000 + DateTimeOffset.Now.Millisecond;
            int timeUntilNextDawn = 0;

            string grantTimeInMsRaw = FileUtilities.GetGameInfo(guildId, "grantTime");
            if (!int.TryParse(grantTimeInMsRaw, out int grantTimeInMs))
            {
                Logs.WriteLog($"Unable to initalize guild '{guildId}' : can not parse to int string '{grantTimeInMsRaw}'");
            }

            if (nowInMilli < grantTimeInMs)
            {
                timeUntilNextDawn = grantTimeInMs - nowInMilli;
            }
            else
            {
                timeUntilNextDawn = grantTimeInMs - nowInMilli + 24 * 60 * 60 * 1000;
            }

            primaryTimer = new Timer(timeUntilNextDawn);
            primaryTimer.AutoReset = false;
            primaryTimer.Enabled = true;
            primaryTimer.Elapsed += new ElapsedEventHandler(StartMainTimer);

            Console.WriteLine($"Initialized primary timer for {guildId} : next dawn is in {timeUntilNextDawn}ms");
        }

        private void StartMainTimer(Object source, ElapsedEventArgs e)
        {
            CallAtDawnTime(source, e);

            mainTimer = new Timer(24 * 60 * 60 * 1000);
            mainTimer.AutoReset = true;
            mainTimer.Enabled = true;
            mainTimer.Elapsed += new ElapsedEventHandler(CallAtDawnTime);

            primaryTimer.Stop();
            primaryTimer.Dispose();
        }

        private void CallAtDawnTime(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine($"Dawn time is called : {DateTimeOffset.Now}. Resolving day...");

            GameUtilities.ComputeMemberPoints(guildId);
            GameUtilities.RevealDailyRiddle(guildId).ConfigureAwait(false);
            GameUtilities.DisplayLeaderBoard(guildId).ConfigureAwait(false);
        }

    }
}
