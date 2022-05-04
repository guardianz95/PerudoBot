using Discord.Interactions;
using PerudoBot.EloService;
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
        public enum GameModeEnum
        {
            SuddenDeath,
            Reverse,
            Variable
        }

        [SlashCommand("elo", "Check elo standings")]
        public async Task Elo(GameModeEnum gameModeParam)
        {
            var gameMode = GameMode.Reverse;
            if (gameModeParam == GameModeEnum.SuddenDeath) gameMode = GameMode.SuddenDeath;
            if (gameModeParam == GameModeEnum.Variable) gameMode = GameMode.Variable;

            var eloHandler = new EloHandler(_db, Context.Guild.Id, gameMode);
            var eloSeason = eloHandler.GetCurrentEloSeason();

            var message = $"`{gameMode}: {eloSeason.SeasonName}`";

            var playerElos = eloSeason.PlayerElos
                .Where(x => x.GameMode == gameMode) // TODO: shouldn't have to do this since we instantiated the elohandler with gamemode. something needs to be fixed.
                .OrderByDescending(x => x.Rating)
                .ToList();

            foreach (var playerElo in playerElos)
            {
                message += $"\n{playerElo.Player.Name}: {playerElo.Rating}";
            }

            await RespondAsync(message);
        }

        private List<PlayerElo> CalculateElo(GameObject game)
        {
            var gameMode = game.GetGameMode();
            var eloHandler = new EloHandler(_db, Context.Guild.Id, gameMode);
            var gamePlayers = game.GetAllPlayers()
                .OrderBy(x => x.Rank);

            foreach (var gamePlayer in gamePlayers)
            {
                eloHandler.AddPlayer(gamePlayer.PlayerId, gamePlayer.Rank);
            }

            eloHandler.CalculateAndSaveElo();

            return eloHandler.GetEloResults();
        }
    }
}
