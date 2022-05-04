using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerudoBot.Modules
{
    public partial class Commands : InteractionModuleBase<SocketInteractionContext>
    {
        [SlashCommand("resenddice", "Resend dice")]
        public async Task ResendDice()
        {
            _gameHandler.SetChannel(Context.Channel.Id);
            var game = _gameHandler.GetActiveGame();

            if (game == null)
            {
                await RespondAsync("No active game", ephemeral: true);
                return;
            }

            var playerDice = game.GetAllPlayers();
            await SendOutDice(playerDice);
            await RespondAsync("Re-sent game dice");
        }

        [SlashCommand("restartround", "Restart round")]
        public async Task RestartRound()
        {
            SetGuildAndChannel();
            var game = _gameHandler.GetActiveGame();

            if (game == null)
            {
                await RespondAsync("No active game", ephemeral: true);
                return;
            }

            await RespondAsync("Restarting round", embed: CreateRoundSummary(game));
            await StartNewRound(game);
        }

        [SlashCommand("status", "Current game status")]
        public async Task Status()
        {
            SetGuildAndChannel();
            var game = _gameHandler.GetActiveGame();

            if (game == null)
            {
                await RespondAsync("No active game", ephemeral: true);
                return;
            }

            var roundStatus = game.GetCurrentRoundStatus();
            await RespondAsync(embed: CreateRoundStatusEmbed(roundStatus), ephemeral: true);
        }
    }
}
