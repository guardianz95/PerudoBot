using Discord;
using Discord.Interactions;
using PerudoBot.Extensions;
using PerudoBot.GameService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerudoBot.Modules
{
    public partial class Commands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("liar", "Call liar")]
        public async Task Liar()
        {
            SetGuildAndChannel();
            var game = _gameHandler.GetActiveGame();

            if (game == null)
            {
                await RespondAsync("No active game", ephemeral: true);
                return;
            }

            var currentPlayer = game.GetCurrentPlayer();
            var roundResult = game.Liar(currentPlayer.PlayerId);
            if (roundResult == null) return;

            var liarResult = roundResult.LiarResult;

            await RespondAsync($"{liarResult.PlayerWhoCalledLiar.Name} called **liar** on `{liarResult.BidQuantity}` ˣ {liarResult.BidPips.ToEmoji()}.");

            // for dramatic effect
            Thread.Sleep(2000);

            var losesOrGains = liarResult.DiceLost > 0 ? "loses" : "gains";
            await Context.Channel.SendMessageAsync($"There was actually `{liarResult.ActualQuantity}` dice. :fire: {liarResult.PlayerWhoLostDice.Name} {losesOrGains} {Math.Abs(liarResult.DiceLost)} dice. :fire:");

            if (liarResult.PlayerWhoLostDice.IsEliminated)
            {
                await SendMessageAsync($":fire::skull::fire: {liarResult.PlayerWhoLostDice.Name} defeated :fire::skull::fire:");
            }

            if (roundResult.BetResults.Count > 0)
            {
                await SendMessageAsync(embed: CreateBetSummary(roundResult.BetResults));
            }

            await SendMessageAsync(embed: CreateRoundSummary(game));

            await StartNewRound(game);
        }

        private Embed CreateBetSummary(List<BetResult> betResults)
        {
            var formattedBetResults = new List<string>();

            foreach (var betResult in betResults)
            {
                var pointsUsed = betResult.BetAmount;
                var pointsGained = betResult.IsSuccessful ? (int)Math.Round(pointsUsed * betResult.BetOdds) : 0;

                if (betResult.IsSuccessful)
                {
                    AddTotalPoints(betResult.BettingPlayer.Id, pointsGained - pointsUsed);
                }
                else
                {
                    AddUsedPoints(betResult.BettingPlayer.Id, pointsUsed);
                }

                var winsOrLoses = betResult.IsSuccessful ? "wins" : "loses";
                var odds = betResult.IsSuccessful ? $"*(x{betResult.BetOdds:0.0})* " : "";
                var pointDisplay = betResult.IsSuccessful ? pointsGained : pointsUsed;
                formattedBetResults.Add($":dollar: {betResult.BettingPlayer.Name} **{winsOrLoses} {pointDisplay}** {odds}points betting {betResult.BetType.ToLower()} on `{betResult.BetQuantity}` ˣ {betResult.BetPips.ToEmoji()}.");
            }

            var builder = new EmbedBuilder()
                .WithTitle($"Bets Summary")
                .AddField("Results", $"{string.Join("\n", formattedBetResults)}");

            return builder.Build();
        }

        private Embed CreateRoundSummary(GameObject game)
        {
            var players = game.GetAllPlayers()
                .Where(x => !x.IsEliminated)
                .OrderBy(x => x.TurnOrder);

            var playerDice = players.Select(x => $"{x.Name}: {string.Join(" ", x.Dice.Select(x => x.ToEmoji()))}".TrimEnd());

            var allDice = players.SelectMany(x => x.Dice);
            var allDiceGrouped = allDice
                .GroupBy(x => x)
                .OrderBy(x => x.Key);

            var countOfOnes = allDiceGrouped.SingleOrDefault(x => x.Key == 1)?.Count();

            var listOfAllDiceCounts = allDiceGrouped.Select(x => $"`{x.Count()}` ˣ {x.Key.ToEmoji()}");

            var totals = new List<string>();
            for (int i = 1; i <= 6; i++)
            {
                var countOfX = allDiceGrouped.SingleOrDefault(x => x.Key == i)?.Count();
                var count1 = countOfOnes ?? 0;
                if (i == 1) count1 = 0;
                var countX = countOfX ?? 0;
                totals.Add($"`{count1 + countX }` ˣ {i.ToEmoji()}");
            }

            var builder = new EmbedBuilder()
                .WithTitle($"Round {game.GetCurrentRoundNumber()} Summary")
                .AddField("Players", $"{string.Join("\n", playerDice)}", inline: true)
                .AddField("Dice", $"{string.Join("\n", listOfAllDiceCounts)}", inline: true)
                .AddField("Totals", $"{string.Join("\n", totals)}", inline: true);

            return builder.Build();
        }
    }
}
