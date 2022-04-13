using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using EscapeBot.Utilities;
using EscapeBot.Constants;
using System.IO;
using DSharpPlus.Entities;

namespace EscapeBot.Commands
{
    public class PlayerCommands : BaseCommandModule
    {
        [Command("play")]
        [Description("allow the player to enter the game.")]
        public async Task Play(CommandContext ctx)
        {
            try
            {
                // test if player is not already registered
                ulong playerId = ctx.Member.Id;
                bool playerAlreadyRegistered = false;

                foreach (string registeredPlayer in File.ReadAllLines(Bot.dataPath + $"/Servers/{ctx.Guild.Id}/Players/.registered.txt"))
                {
                    if (!ulong.TryParse(registeredPlayer, out ulong registeredPlayerId))
                    {
                        Logs.WriteLog($"Player is registered but can't be interpreted as id : {registeredPlayer}");
                        continue;
                    }

                    if (playerId == registeredPlayerId)
                    {
                        playerAlreadyRegistered = true;
                        break;
                    }
                }

                if (playerAlreadyRegistered)
                {
                    await ctx.Message.DeleteAsync();
                    await ctx.Member.SendMessageAsync(BotConstants.botMessagesDict[botMessages.alreadyPlaying]);
                    return;
                }


                //assert we are in the right channel
                if (BotConstants.guildsPublicCommandChannels[ctx.Guild.Id] != ctx.Channel.Id)
                {
                    //player called the command in the wrong channel
                    await ctx.Message.DeleteAsync();
                    await ctx.Member.SendMessageAsync(BotConstants.botMessagesDict[botMessages.wrongChannel]);
                    await ctx.Member.SendMessageAsync($"You tried channel {ctx.Channel.Id} but should use {BotConstants.guildsPublicCommandChannels[ctx.Guild.Id]}.");
                    return;
                }

                //check game state
                if (FileUtilities.GetGameStatus(ctx.Guild.Id) == gameStatus.playing)
                {
                    //the game is already started, create new channel answer, give basic role
                    //get the start role
                    string startRoleName = File.ReadAllText(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/startRole.txt");
                    ulong startRoleId = 0;
                    foreach (string line in File.ReadAllLines(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/roomsRoles.txt"))
                    {
                        string[] data = line.Split(":");
                        if (data[1] == startRoleName)
                        {
                            startRoleId = ulong.Parse(data[2]);
                        }
                    }
                    if (startRoleId == 0)
                    {
                        await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
                        Logs.WriteLog("Player tried to enter the game that was set as playing, but unable to find startRoleId.");
                        return;
                    }
                    //get the start role
                    DiscordRole startRole = ctx.Guild.GetRole(startRoleId);

                    //get the answer category
                    ulong categoryId = DiscordUtilities.GetPlayerAnswerCategoryId(ctx.Guild.Id);
                    if (categoryId == 0)
                    {
                        await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
                        Logs.WriteLog("Player tried to enter the game that was set as playing, but unable to find answer category.");
                        return;
                    }
                    DiscordChannel answerCategory = await Bot.Client.GetChannelAsync(categoryId);

                    await GameUtilities.CreatePlayer(ctx.Member, ctx.Guild, answerCategory, startRole);

                    //still put the player in the register data, for stats
                    File.AppendAllLines(Bot.dataPath + $"Servers/{ctx.Guild.Id}/Players/.registered.txt", new string[1] { ctx.Member.Id.ToString() });
                }
                else
                {
                    //game not ready yet, set player id in the registered players
                    File.AppendAllLines(Bot.dataPath + $"Servers/{ctx.Guild.Id}/Players/.registered.txt", new string[1] { ctx.Member.Id.ToString() });
                }

                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }


        [Command("try")]
        [Description("try to enter a code and solve a riddle.")]
        public async Task Try(CommandContext ctx,
            [Description("The room where you try the riddle : have to be a existing room you can access")] string room,
            [Description("the lock you want to open : have to be from a given set in the room description")] string riddle,
            [Description("your try at that lock.")] string answer)
        {
            try
            {
                //assert the player is in his answer channel
                ulong channelId = DiscordUtilities.GetPlayerAnswerChannelId(ctx.Guild.Id, ctx.Member.Id);
                if (ctx.Channel.Id != channelId)
                {
                    await ctx.Message.DeleteAsync();
                    await ctx.Member.SendMessageAsync(BotConstants.botMessagesDict[botMessages.wrongChannel]);
                    return;
                }

                //assert room exists
                if (!Directory.Exists(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room.ToLower()}"))
                {
                    await ctx.Message.RespondAsync(BotConstants.botMessagesDict[botMessages.invalidRoom]);
                    await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
                    return;
                }

                //assert player have the role for the room
                ulong roomRoleId = DiscordUtilities.GetRoleId(File.ReadAllText(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room}/role.txt"), ctx.Guild.Id);
                bool hasRoomRole = false;
                foreach (DiscordRole memberRole in ctx.Member.Roles)
                {
                    if (memberRole.Id == roomRoleId)
                    {
                        hasRoomRole = true;
                        break;
                    }
                }
                if (!hasRoomRole)
                {
                    await ctx.Message.RespondAsync(BotConstants.botMessagesDict[botMessages.invalidRoom]);
                    await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
                    return;
                }

                //assert riddle exists
                bool riddleExist = false;
                string[] riddles = File.ReadAllLines(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room}/code.txt");
                string[] riddleData = new string[3] { "=F*)?PnVP@W8(zs8", "A*p(e22HM`93%LrE", "%@3,W$7*7='Wws]#" };
                foreach (string line in riddles)
                {
                    riddleData = line.Split(":");
                    if (riddleData[0] == riddle)
                    {
                        riddleExist = true;
                        break;
                    }
                }
                if (!riddleExist)
                {
                    await ctx.Message.RespondAsync(BotConstants.botMessagesDict[botMessages.invalidRiddle]);
                    await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
                    return;
                }

                //if the riddle exist, the riddle data is in the riddleData variable
                //check the player gave the right answer
                if (answer.ToLower() == riddleData[1].ToLower())
                {
                    await ctx.Message.RespondAsync(BotConstants.botMessagesDict[botMessages.rightAnswer]);
                    await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
                    ulong roleId = DiscordUtilities.GetRoleId(riddleData[2], ctx.Guild.Id);
                    //give the role directly
                    DiscordRole role = ctx.Guild.GetRole(roleId);
                    await ctx.Member.GrantRoleAsync(role);
                    //set the stats in the player historic
                    string memberPath = Bot.dataPath + $"Servers/{ctx.Guild.Id}/Players/{ctx.Member.Id}/RiddleResolveTimes/{room}-{riddle}.txt";
                    GameUtilities.WriteMemberActionTime(memberPath, ctx.Message.Timestamp);
                    return;
                }
                else
                {
                    await ctx.Message.RespondAsync(BotConstants.botMessagesDict[botMessages.wrongAnswer]);
                    await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.WrongAnswer]);
                    return;
                }
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }

        [Command("clue")]
        [Description("Get a clue for the riddle, but you will get half the points for that riddle...")]
        public async Task AskClue(CommandContext ctx,
            [Description("The room where you try the riddle : have to be a existing room you can access")] string room,
            [Description("the lock you want to open : have to be from a given set in the room description")] string riddle)
        {
            try
            {
                //assert the player is in his answer channel
                ulong channelId = DiscordUtilities.GetPlayerAnswerChannelId(ctx.Guild.Id, ctx.Member.Id);
                if (ctx.Channel.Id != channelId)
                {
                    await ctx.Message.DeleteAsync();
                    await ctx.Member.SendMessageAsync(BotConstants.botMessagesDict[botMessages.wrongChannel]);
                    return;
                }

                //assert room exists
                if (!Directory.Exists(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room.ToLower()}"))
                {
                    await ctx.Message.RespondAsync(BotConstants.botMessagesDict[botMessages.invalidRoom]);
                    await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
                    return;
                }

                //assert player have the role for the room
                ulong roomRoleId = DiscordUtilities.GetRoleId(File.ReadAllText(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room}/role.txt"), ctx.Guild.Id);
                bool hasRoomRole = false;
                foreach (DiscordRole memberRole in ctx.Member.Roles)
                {
                    if (memberRole.Id == roomRoleId)
                    {
                        hasRoomRole = true;
                        break;
                    }
                }
                if (!hasRoomRole)
                {
                    await ctx.Message.RespondAsync(BotConstants.botMessagesDict[botMessages.invalidRoom]);
                    await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
                    return;
                }

                //assert riddle exists
                bool riddleExist = false;
                string[] riddles = File.ReadAllLines(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room}/code.txt");
                string[] riddleData = new string[3] { "=F*)?PnVP@W8(zs8", "A*p(e22HM`93%LrE", "%@3,W$7*7='Wws]#" };
                foreach (string line in riddles)
                {
                    riddleData = line.Split(":");
                    if (riddleData[0] == riddle)
                    {
                        riddleExist = true;
                        break;
                    }
                }

                if (riddleExist)
                {
                    //assert riddle has been send at least a day ago
                    if (!DateTimeOffset.TryParse(File.ReadAllText(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room}/revealTime.txt"), out DateTimeOffset riddleDate))
                    {
                        await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]).ConfigureAwait(false);
                        return;
                    }

                    TimeSpan ts = DateTimeOffset.Now - riddleDate;
                    if (ts.TotalMilliseconds < BotConstants.TimeInMillisBeforeClue)
                    {
                        TimeSpan remaining = riddleDate.AddMilliseconds(BotConstants.TimeInMillisBeforeClue) - DateTimeOffset.Now;
                        await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.WrongTime]).ConfigureAwait(false);
                        await ctx.Channel.SendMessageAsync(BotConstants.botMessagesDict[botMessages.clueTooSoon]);
                        await ctx.Channel.SendMessageAsync($"Clue will be available in {remaining.Days}d, {remaining.Hours}h, {remaining.Minutes}min");
                        return;
                    }

                    //we can send the clue to the player
                    string path = Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room}/indices/{riddle}/";
                    string memberPath = Bot.dataPath + $"Servers/{ctx.Guild.Id}/Players/{ctx.Member.Id}/Clues/{room}-{riddle}.txt";
                    GameUtilities.WriteMemberActionTime(memberPath, ctx.Message.Timestamp);
                    await DiscordUtilities.DisplayFolderInChannel(path, ctx.Channel).ConfigureAwait(false);
                }


                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]).ConfigureAwait(false);
            }
        }


        [Command("sos")]
        [Description("Get a really solid clue for the riddle, but you can't get points for that riddle...")]
        public async Task AskSos(CommandContext ctx,
            [Description("The room where you try the riddle : have to be a existing room you can access")] string room,
            [Description("the lock you want to open : have to be from a given set in the room description")] string riddle)
        {
            try
            {
                //assert the player is in his answer channel
                ulong channelId = DiscordUtilities.GetPlayerAnswerChannelId(ctx.Guild.Id, ctx.Member.Id);
                if (ctx.Channel.Id != channelId)
                {
                    await ctx.Message.DeleteAsync();
                    await ctx.Member.SendMessageAsync(BotConstants.botMessagesDict[botMessages.wrongChannel]);
                    return;
                }

                //assert room exists
                if (!Directory.Exists(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room.ToLower()}"))
                {
                    await ctx.Message.RespondAsync(BotConstants.botMessagesDict[botMessages.invalidRoom]);
                    await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
                    return;
                }

                //assert player have the role for the room
                ulong roomRoleId = DiscordUtilities.GetRoleId(File.ReadAllText(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room}/role.txt"), ctx.Guild.Id);
                bool hasRoomRole = false;
                foreach (DiscordRole memberRole in ctx.Member.Roles)
                {
                    if (memberRole.Id == roomRoleId)
                    {
                        hasRoomRole = true;
                        break;
                    }
                }
                if (!hasRoomRole)
                {
                    await ctx.Message.RespondAsync(BotConstants.botMessagesDict[botMessages.invalidRoom]);
                    await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
                    return;
                }

                //assert riddle exists
                bool riddleExist = false;
                string[] riddles = File.ReadAllLines(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room}/code.txt");
                string[] riddleData = new string[3] { "=F*)?PnVP@W8(zs8", "A*p(e22HM`93%LrE", "%@3,W$7*7='Wws]#" };
                foreach (string line in riddles)
                {
                    riddleData = line.Split(":");
                    if (riddleData[0] == riddle)
                    {
                        riddleExist = true;
                        break;
                    }
                }

                if (riddleExist)
                {
                    //assert riddle has been send at least two day ago
                    if (!DateTimeOffset.TryParse(File.ReadAllText(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room}/revealTime.txt"), out DateTimeOffset riddleDate))
                    {
                        await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]).ConfigureAwait(false);
                        return;
                    }

                    TimeSpan ts = DateTimeOffset.Now - riddleDate;
                    if (ts.TotalMilliseconds < BotConstants.TimeInMillisBeforeSOS)
                    {
                        TimeSpan remaining = riddleDate.AddMilliseconds(BotConstants.TimeInMillisBeforeSOS) - DateTimeOffset.Now;
                        await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.WrongTime]).ConfigureAwait(false);
                        await ctx.Channel.SendMessageAsync(BotConstants.botMessagesDict[botMessages.sosTooSoon]);
                        await ctx.Channel.SendMessageAsync($"Clue will be available in {remaining.Days}d, {remaining.Hours}h, {remaining.Minutes}min");
                        return;
                    }

                    //we can send the clue to the player
                    string path = Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{room}/sos/{riddle}/";
                    string memberPath = Bot.dataPath + $"Servers/{ctx.Guild.Id}/Players/{ctx.Member.Id}/sos/{room}-{riddle}.txt";
                    GameUtilities.WriteMemberActionTime(memberPath, ctx.Message.Timestamp);
                    await DiscordUtilities.DisplayFolderInChannel(path, ctx.Channel).ConfigureAwait(false);
                }


                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]).ConfigureAwait(false);
            }
        }

        /*
        [Command("score")]
        [Description("ask the bot for your current score !")]
        public async Task AskScore(CommandContext ctx)
        {           
            try
            {
                //assert the player is in his answer channel
                ulong channelId = DiscordUtilities.GetPlayerAnswerChannelId(ctx.Guild.Id, ctx.Member.Id);
                if (ctx.Channel.Id != channelId)
                {
                    await ctx.Message.DeleteAsync();
                    await ctx.Member.SendMessageAsync(BotConstants.botMessagesDict[botMessages.wrongChannel]);
                    return;
                }

                string[] playersScores = File.ReadAllLines(Bot.dataPath + $"Servers/{ctx.Guild.Id}/Players/score.txt");

                foreach(string playerScore in playersScores)
                {
                    string[] data = playerScore.Split(":");
                    
                    if(data[0] == ctx.Member.Id.ToString())
                    {
                        await ctx.Message.RespondAsync($"Ton score actuel (calculé à la dernière resolution) est : \n{data[1]}");
                    }
                }
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]).ConfigureAwait(false);
            }
        }
        */
    }
}
