using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System;
using EscapeBot.Constants;
using EscapeBot.Utilities;


namespace EscapeBot.Commands
{
    public class MasterCommands : BaseCommandModule
    {
        [Command("createGame")]
        [Description("Set the Server as a new escape game")]
        [RequireRoles(RoleCheckMode.Any, "Game Master")]
        public async Task CreateGame(CommandContext ctx)
        {
            try
            {
                //set the current server as a new game server
                //check if the server is already recorder as a game

                if (Directory.Exists(Bot.dataPath + $"Servers/{ctx.Guild.Id}"))
                {
                    //already a game created on the server;
                    await ctx.Message.RespondAsync("Game already started on that server;\n" +
                        "use '!initGame' to set or reset the game;\n" +
                        "use 'deleteGame' to delete that game;");
                }
                else
                {
                    //create a game with the name of the server id, and copy the template game into it
                    Directory.CreateDirectory(Bot.dataPath + $"Servers/{ctx.Guild.Id}");
                    FileUtilities.CloneDirectory(Bot.dataPath + $"Servers/.Template", Bot.dataPath + $"Servers/{ctx.Guild.Id}");
                    await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
                }
            }
            catch(Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }


        [Command("initGame")]
        [Description("Create the roles, rooms and game objects, initialize the game")]
        [RequireRoles(RoleCheckMode.Any, "Game Master")]
        public async Task InitializeGame(CommandContext ctx)
        {
            try
            {
                //Initialize Game, create all the roles, channels and everything
                //check if game exists
                if (!Directory.Exists(Bot.dataPath + $"Servers/{ctx.Guild.Id}"))
                {
                    Logs.WriteLog("Unable to init game : game does not exists. try '!createGame' first !", false, ctx.Channel);
                }

                //get the game category
                ulong categoryId = DiscordUtilities.GetGameCategoryId(ctx.Guild.Id);
                if (categoryId == 0)
                {
                    Logs.WriteLog("Game category have not been set (at data/Servers/<serverName>/gameInfo.txt).", false, ctx.Channel);
                    return;
                }
                DiscordChannel gameCategory = await Bot.Client.GetChannelAsync(categoryId);

                //start by deleting every channel and roles that start with the game symbol
                await GameUtilities.EreaseDiscordGameElements(ctx.Guild.Id, gameCategory);

                //now we need to rebuild everything from the given game data
                List<string> rolesForRooms = new List<string>();
                //create every room and associated role
                string roomFolderPath = Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms";
                foreach (string roomPath in Directory.GetDirectories(roomFolderPath, "*.*", SearchOption.TopDirectoryOnly))
                {                   
                    string roomRole = File.ReadAllText(roomPath + "/role.txt");
                    string roomName = new DirectoryInfo(roomPath).Name;

                    //skip template room ofc
                    if(roomName == ".Template")
                    {
                        continue;
                    }

                    //create the role
                    DiscordRole role = await ctx.Guild.CreateRoleAsync(BotConstants.gameSymbol + roomRole, mentionable:false);
                    //set the role string
                    rolesForRooms.Add($"{roomName}:{roomRole}:{role.Id}");
                    //set up permissions
                    DiscordOverwriteBuilder permissionsBuilder = new DiscordOverwriteBuilder(role);
                    permissionsBuilder.Allow(DSharpPlus.Permissions.AccessChannels);
                    permissionsBuilder.Deny(DSharpPlus.Permissions.SendMessages);
                    //create the room
                    DiscordChannel roomChannel = await ctx.Guild.CreateChannelAsync(BotConstants.gameSymbol + roomName,
                        DSharpPlus.ChannelType.Text, gameCategory,
                        overwrites:new DiscordOverwriteBuilder[1] { permissionsBuilder });
                    await DiscordUtilities.DisplayFolderInChannel(roomPath + "/contexte/", roomChannel);

                    //write the room channel id
                    File.WriteAllText(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{roomName}/channelId.txt", roomChannel.Id.ToString());
                }

                //write in a file all roles for rooms
                File.WriteAllLines(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/roomsRoles.txt", rolesForRooms.ToArray());

                //set the game info as initialized
                FileUtilities.SetGameStatus(ctx.Guild.Id, gameStatus.initialized);

                //create the room order File ?

                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }

        [Command("updateRooms")]
        [Description("Update new rooms")]
        [RequireRoles(RoleCheckMode.Any, "Game Master")]
        public async Task UpdateRoms(CommandContext ctx)
        {
            try
            {
                //Add rooms that arn't ready yet
                //check if game exists
                if (!Directory.Exists(Bot.dataPath + $"Servers/{ctx.Guild.Id}"))
                {
                    Logs.WriteLog("Unable to init game : game does not exists. try '!createGame' first !", false, ctx.Channel);
                }

                //get the game category
                ulong categoryId = DiscordUtilities.GetGameCategoryId(ctx.Guild.Id);
                if (categoryId == 0)
                {
                    Logs.WriteLog("Game category have not been set (at data/Servers/<serverName>/gameInfo.txt).", false, ctx.Channel);
                    return;
                }
                DiscordChannel gameCategory = await Bot.Client.GetChannelAsync(categoryId);

                //now we need to rebuild everything from the given game data
                List<string> rolesForRooms = new List<string>();
                //create every room and associated role
                string roomFolderPath = Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms";
                foreach (string roomPath in Directory.GetDirectories(roomFolderPath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    string roomRole = File.ReadAllText(roomPath + "/role.txt");
                    string roomName = new DirectoryInfo(roomPath).Name;

                    //skip template room ofc
                    if (roomName == ".Template")
                    {
                        continue;
                    }

                    //skip if room already exists, we check if the room have an assigned id
                    string roomChannelIdText = File.ReadAllText(roomPath + "/channelId.txt");
                    Console.WriteLine($"[{roomChannelIdText}]");

                    if(roomChannelIdText != "")
                    {
                        continue;
                    }

                    //otherwise, create room and set up everything

                    //create the role
                    DiscordRole role = await ctx.Guild.CreateRoleAsync(BotConstants.gameSymbol + roomRole, mentionable: false);
                    //set the role string
                    rolesForRooms.Add($"{roomName}:{roomRole}:{role.Id}");
                    //set up permissions
                    DiscordOverwriteBuilder permissionsBuilder = new DiscordOverwriteBuilder(role);
                    permissionsBuilder.Allow(DSharpPlus.Permissions.AccessChannels);
                    permissionsBuilder.Deny(DSharpPlus.Permissions.SendMessages);
                    //create the room
                    DiscordChannel roomChannel = await ctx.Guild.CreateChannelAsync(BotConstants.gameSymbol + roomName,
                        DSharpPlus.ChannelType.Text, gameCategory,
                        overwrites: new DiscordOverwriteBuilder[1] { permissionsBuilder });
                    //send room data
                    await DiscordUtilities.DisplayFolderInChannel(roomPath + "/contexte/", roomChannel);

                    //write the room channel id
                    File.WriteAllText(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/Rooms/{roomName}/channelId.txt", roomChannel.Id.ToString());
                }

                //write in a file all roles for rooms
                File.AppendAllLines(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/roomsRoles.txt", rolesForRooms.ToArray());


                //create the room order File ?

                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }


        [Command("startGame")]
        [Description("Start the game; allow players to see rooms and enter commands")]
        [RequireRoles(RoleCheckMode.Any, "Game Master")]
        public async Task StartGame(CommandContext ctx)
        {
            try
            {
                //get the start role
                string startRoleName = File.ReadAllText(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/startRole.txt");
                ulong startRoleId = 0;
                foreach(string line in File.ReadAllLines(Bot.dataPath + $"Servers/{ctx.Guild.Id}/GameData/roomsRoles.txt"))
                {
                    string[] data = line.Split(":");
                    if(data[1] == startRoleName)
                    {
                        startRoleId = ulong.Parse(data[2]);
                    }
                }

                //get the start role
                DiscordRole startRole = ctx.Guild.GetRole(startRoleId);
                //get the answer category
                ulong categoryId = DiscordUtilities.GetPlayerAnswerCategoryId(ctx.Guild.Id);
                if (categoryId == 0)
                {
                    await ctx.Message.RespondAsync("Answer category have not been set (at data/Servers/<serverName>/gameInfo.txt).");
                    return;
                }
                DiscordChannel answerCategory = await Bot.Client.GetChannelAsync(categoryId);


                if (startRole == null)
                {
                    await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
                    await ctx.Message.RespondAsync("No start role or invalid start role. Check 'data/Servers/<ServerName>/GameData/startRole.txt");
                    return;
                }

                //give the start role to anybody registered
                foreach(string line in File.ReadAllLines(Bot.dataPath + $"Servers/{ctx.Guild.Id}/Players/.registered.txt"))
                {
                    if(line == "") { continue; } //skip last empty line
                    if(!ulong.TryParse(line, out ulong memberId))
                    {
                        Logs.WriteLog($"Unable to interpret as ulong : {line}. Player may not be loaded", false, ctx.Channel);
                        continue;
                    }
                    
                    DiscordMember member = await ctx.Guild.GetMemberAsync(memberId);
                    if(member != null)
                    {
                        //create a new player
                        await GameUtilities.CreatePlayer(member, ctx.Guild, answerCategory, startRole);
                    }                    
                }

                //set the game as playing
                FileUtilities.SetGameStatus(ctx.Guild.Id, gameStatus.playing);

                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }


        [Command("deleteGame")]
        [Description("Delete the game started on that Server")]
        [RequireRoles(RoleCheckMode.Any, "Game Master")]
        public async Task DeleteGame(CommandContext ctx)
        {
            try
            {
                //Delete the game
                if (Directory.Exists(Bot.dataPath + $"Servers/{ctx.Guild.Id}"))
                {
                    //earease all game data from the current guild
                    //get the game category
                    ulong categoryId = DiscordUtilities.GetGameCategoryId(ctx.Guild.Id);
                    if (categoryId == 0)
                    {
                        Logs.WriteLog("Game category have not been set (at data/Servers/<serverName>/gameInfo.txt).", false, ctx.Channel);
                        return;
                    }
                    DiscordChannel gameCategory = await Bot.Client.GetChannelAsync(categoryId);
                    await GameUtilities.EreaseDiscordGameElements(ctx.Guild.Id, gameCategory);
                    //erease diectory
                    Directory.Delete(Bot.dataPath + $"Servers/{ctx.Guild.Id}", true);
                    await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
                }
                else
                {
                    await ctx.Message.RespondAsync("There is no games on this server.");
                }
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }

        [Command("resolveDailyRiddle")]
        [Description("Force the appearence of the riddle of the day")]
        [RequireRoles(RoleCheckMode.Any, "Game Master")]
        public async Task ResolveRoles(CommandContext ctx)
        {
            try
            {
                await GameUtilities.RevealDailyRiddle(ctx.Guild.Id);
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }


        [Command("updateLeaderBoard")]
        [Description("display the leaderboard")]
        [RequireRoles(RoleCheckMode.Any, "Game Master")]
        public async Task DisplayLeaderBoard(CommandContext ctx)
        {
            try
            {
                await GameUtilities.DisplayLeaderBoard(ctx.Guild.Id);
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }


        [Command("setGameInfo")]
        [Description("set the given game info to the given value")]
        [RequireRoles(RoleCheckMode.Any, "Game Master")]
        public async Task SetGameInfo(CommandContext ctx, string info, string value)
        {
            try
            {
                FileUtilities.SetGameInfo(ctx.Guild.Id, info, value);
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }

        [Command("clearLogs")]
        [Description("clear the logs file")]
        [RequireRoles(RoleCheckMode.Any, "Game Master")]
        public async Task ClearLogs(CommandContext ctx)
        {
            try
            {
                Logs.ClearLogs();
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }

        [Command("computePoints")]
        [Description("Manually compute players points")]
        [RequireRoles(RoleCheckMode.Any, "Game Master")]
        public async Task ComputePlayersPoints(CommandContext ctx)
        {
            try
            {
                GameUtilities.ComputeMemberPoints(ctx.Guild.Id);
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }


        [Command("recomputeAllPlayerPoints")]
        [Description("Quite costy operation, but rearrange all players point by looking though their channels.")]
        [RequireRoles(RoleCheckMode.Any, "Game Master")]
        public async Task DisplayDateTimeOffset(CommandContext ctx)
        {
            try
            {
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.WrongTime]);
                await GameUtilities.RecomputeAllRiddleResolveTime(ctx.Guild.Id);
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandDone]);
            }
            catch (Exception e)
            {
                Logs.WriteLog(e.ToString());
                await ctx.Message.CreateReactionAsync(BotConstants.botEmojis[Emojis.CommandError]);
            }
        }

    }
}
