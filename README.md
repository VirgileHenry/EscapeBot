# EscapeBot
This is a Discord bot that simulates an escape game in a discord server. 
The bot itself can manage any number of games, but at most one game per server.

All commands to init / start / stop the games are in the "masterCommand" channel, and require a role named "Game Master"


To make it work, the bot also need a "Data" directory, and the path to this directory must be updated in Bot.cs.
In this directory, you need :
- a "Server" directory;
- a "Logs" directory, containing "LogsErrorId.txt" and "Logs.txt";
- a "Json" directory, containing a "config.json" file, containing the following :
{
    "token": "YOUR-BOT-TOKEN",
    "prefix": "COMMAND-PREFIX"
}
- a "Messages" directory, containing "<errorMessageName>.txt" files where the errorMessageName can be find when loading the bot messages, line 108 of BotConstants.cs

  
# TODO ; update this readme to explain how to setup games and play
