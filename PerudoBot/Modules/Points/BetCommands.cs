using Discord.Interactions;
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
        public enum BetTypeChoice
        {
            Liar,
            Exact
        }

        [SlashCommand("bet", "Place a bet")]
        public async Task Bet(BetTypeChoice betTypeChoice, int betAmount)
        {
            if (betAmount <= 0)
            {
                await RespondAsync("Bet amount must be above 0", ephemeral: true);
                return;
            }

            SetGuildAndChannel();
            var game = _gameHandler.GetActiveGame();
            if (game == null) return;

            var targetAction = game.GetPreviousBid();
            if (targetAction == null)
            {
                await RespondAsync("There is nothing to bet one right now", ephemeral: true);
                return;
            }

            var bettingPlayer = _gameHandler
                .CreateAndGetDiscordPlayer(Context.User.Id, Context.User.Username, Context.User.IsBot)
                .Player;

            if (targetAction.GamePlayer.PlayerId == bettingPlayer.Id)
            {
                await RespondAsync("You can't bet on your own bid", ephemeral: true);
                return;
            }

            var playerHasBets = game
                .GetCurrentRound()
                .Actions.OfType<Bet>()
                .Where(x => x.BettingPlayerId == bettingPlayer.Id)
                .Any();

            if (playerHasBets)
            {
                await RespondAsync("You can only bet once per round", ephemeral: true);
                return;
            }

            if (bettingPlayer.AvailablePoints < betAmount)
            {
                await RespondAsync("You don't have enough points to place this bet", ephemeral: true);
                return;
            }

            var betType = betTypeChoice == BetTypeChoice.Liar ? BetType.Liar : BetType.Exact;

            game.BetOnLatestAction(bettingPlayer, betAmount, betType);

            var typeString = (betType == BetType.Liar) ? "a lie" : "exact";
            await RespondAsync($":dollar: {bettingPlayer.Name} bets {betAmount} that `{targetAction.Quantity}` ˣ {targetAction.Pips.ToEmoji()} is {typeString}.");
        }
    }
}
