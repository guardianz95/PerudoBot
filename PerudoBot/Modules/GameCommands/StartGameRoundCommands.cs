using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using PerudoBot.Database.Data;
using PerudoBot.Extensions;
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
        [ComponentInteraction("start_game")]
        public async Task StartGameHandler()
        {
            SetGuildAndChannel();
            var players = _gameHandler.GetSetupPlayerIds();

            if (players.Count < 2)
            {
                await RespondAsync($"Need at least 2 players to start", ephemeral: true);
                return;
            }

            var game = _gameHandler.CreateGame(players);

            if (game == null)
            {
                await RespondAsync($"Game already in progress", ephemeral: true);
                return;
            }

            game.ShufflePlayers();

            await RespondAsync("Starting the game!\nUse `/bid 2 2s` or `/liar` to play.");
            await StartNewRound(game);
        }

        private async Task StartNewRound(GameObject game)
        {
            var roundStatus = game.StartNewRound();

            if (roundStatus.IsActive == false)
            {
                await SendMessageAsync($":trophy: {roundStatus.Winner.GetMention(_db)} is the winner with `{roundStatus.Winner.NumberOfDice}` dice remaining! :trophy:");
                return;
            }

            await SendOutDice(roundStatus.Players);

            var nextPlayer = game.GetCurrentPlayer();
            var updateMessage = $"A new round has begun. {nextPlayer.GetMention(_db)} goes first";

            await SendMessageAsync(updateMessage, embed: CreateRoundStatusEmbed(roundStatus));
        }

        private Embed CreateRoundStatusEmbed(RoundStatus roundStatus)
        {
            SetGuildAndChannel();

            var game = _gameHandler.GetActiveGame();
            if (game == null) return null;

            var totalDice = roundStatus.Players.Sum(x => x.Dice.Count);

            var players = roundStatus.ActivePlayers
                            .OrderBy(x => x.TurnOrder)
                            .Select(x => $"`{x.Dice.Count}` {x.Name} `{x.AvailablePoints} pts`");

            var nextPlayer = game.GetCurrentPlayer();

            var playerList = string.Join("\n", players);

            var bids = roundStatus.Bids.Select(x => $"`{x.Quantity}` ˣ {x.Pips.ToEmoji()}");
            var bidList = string.Join("\n", bids);

            var probability = 3.0;
            var quickmaths = $"Quick maths: {totalDice}/{probability:F0} = `{totalDice / probability:F2}`";

            var builder = new EmbedBuilder()
                .WithTitle($"Round {roundStatus.RoundNumber}")
                .AddField("Players", $"{playerList}\n\nTotal dice left: `{totalDice}`\n{quickmaths}", inline: false);

            return builder.Build();
        }

        private async Task SendOutDice(List<PlayerData> playerDice, bool isUpdate = false)
        {
            foreach (var player in playerDice)
            {
                // send dice to each player
                if (player.NumberOfDice == 0) continue;
                var diceEmojis = player.GetDiceEmojis();

                var userId = GetUserId(player);
                var user = Context.Guild.Users.Single(x => x.Id == userId);

                var prefix = isUpdate ? "         :" : "Your dice:";
                var message = $"`{prefix}` {string.Join(" ", diceEmojis)}";
                var requestOptions = new RequestOptions() { RetryMode = RetryMode.RetryRatelimit };

                await user.SendMessageAsync(message, options: requestOptions);
            }
        }
    }
}