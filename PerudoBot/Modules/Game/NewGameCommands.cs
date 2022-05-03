using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using PerudoBot.GameService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerudoBot.Modules
{
    public partial class Commands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("new", "Create new game")]
        public async Task NewGame()
        {
            SetGuildAndChannel();
            var game = _gameHandler.GetActiveGame();

            if (game != null)
            {
                await RespondAsync(text: "Game in progress already", ephemeral: true);
                return;
            }

            if (DateTime.Now.Hour < 12) _gameHandler.SetGameModeSuddenDeath();
            else _gameHandler.SetGameModeReverse();

            _gameHandler.ClearPlayerList();

            await DeferAsync();
            await Context.Interaction.DeleteOriginalResponseAsync();

            var message = await ReplyAsync(components: CreateGameSetupComponents(), embed: CreateGameSetupEmbed());
            _gameHandler.SetSetupMessageId(message.Id);
        }

        [SlashCommand("terminate", "Terminate existing game")]
        public async Task Terminate()
        {
            SetGuildAndChannel();
            _gameHandler.Terminate();
            await RespondAsync(text: "Game terminated");
        }

        [UserCommand("Add user to the game")]
        public async Task AddPlayerCommand(SocketUser user)
        {
            SetGuildAndChannel();

            var game = _gameHandler.GetActiveGame();
            if (game != null)
            {
                await RespondAsync("Game already in progress", ephemeral: true);
                return;
            }

            var setupMessageId = _gameHandler.GetSetupMessageId();
            if (setupMessageId == 0)
            {
                await RespondAsync("No active game setup", ephemeral: true);
                return;
            }

            _gameHandler.AddPlayer(user.Id, user.Username, user.IsBot);

            var setupMessage = (IUserMessage)await Context.Channel.GetMessageAsync(setupMessageId);
            await setupMessage.ModifyAsync(x =>
            {
                x.Embed = CreateGameSetupEmbed();
            });

            await DeferAsync();
            await Context.Interaction.DeleteOriginalResponseAsync();
        }

        [ComponentInteraction("add_player")]
        public async Task AddPlayerHandler()
        {
            SetGuildAndChannel();

            var game = _gameHandler.GetActiveGame();
            if (game != null)
            {
                await RespondAsync("Game already in progress", ephemeral: true);
                return;
            }

            var socketUser = Context.User;
            _gameHandler.AddPlayer(socketUser.Id, socketUser.Username, socketUser.IsBot);

            await DeferAsync();
            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = CreateGameSetupEmbed();
            });
        }

        [ComponentInteraction("select_game_mode")]
        public async Task GameModeSelected(string[] gameMode)
        {
            SetGuildAndChannel();

            var game = _gameHandler.GetActiveGame();
            if (game != null)
            {
                await RespondAsync("Game already in progress", ephemeral: true);
                return;
            }

            if (gameMode[0] == GameMode.SuddenDeath) _gameHandler.SetGameModeSuddenDeath();
            if (gameMode[0] == GameMode.Reverse) _gameHandler.SetGameModeReverse();
            if (gameMode[0] == GameMode.Variable) _gameHandler.SetGameModeVariable();

            await DeferAsync();
            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = CreateGameSetupEmbed();
            });
        }

        private MessageComponent CreateGameSetupComponents()
        {
            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("Change game mode")
                .WithCustomId("select_game_mode")
                .AddOption("Sudden Death", GameMode.SuddenDeath)
                .AddOption("Reverse", GameMode.Reverse)
                .AddOption("Variable", GameMode.Variable);

            var addPlayerButton = new ButtonBuilder()
            {
                Label = "Add",
                CustomId = "add_player",
                Style = ButtonStyle.Secondary
            };

            var startGameButton = new ButtonBuilder()
            {
                Label = "Start",
                CustomId = "start_game",
                Style = ButtonStyle.Success
            };

            var components = new ComponentBuilder()
                .WithButton(addPlayerButton)
                .WithButton(startGameButton)
                .WithSelectMenu(menuBuilder);

            return components.Build();
        }

        private Embed CreateGameSetupEmbed()
        {
            var gamePlayers = _gameHandler.GetSetupPlayerIds();

            var listOfPlayers = gamePlayers
                .Select(x => $"{x.Name} `{x.AvailablePoints}`")
                .ToList();

            var gameType = "Sudden Death";
            if (_gameHandler.GetMode() == GameMode.Variable) gameType = "Variable";
            if (_gameHandler.GetMode() == GameMode.Reverse) gameType = "Reverse";

            var playerListText = string.Join("\n", listOfPlayers);
            if (playerListText == "") playerListText = "No players yet";

            var embed = new EmbedBuilder()
                            .WithTitle($":leaves: New Game :leaves:")
                            .AddField($"Players ({listOfPlayers.Count()})", $"{playerListText}", inline: false)
                            .AddField($"Game Mode", $"{gameType}", inline: false);

            return embed.Build();
        }
    }
}
