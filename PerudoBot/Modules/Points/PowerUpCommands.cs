using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using PerudoBot.Database.Data;
using PerudoBot.GameService;
using PerudoBot.GameService.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerudoBot.Modules
{
    public partial class Commands : InteractionModuleBase<SocketInteractionContext>
    {
        private Random _random = new Random();

        [SlashCommand("steal", "Steal up to 3 dice from target player, new dice are mystery")]
        public async Task Steal(SocketUser stealFrom)
        {
            SetGuildAndChannel();
            var game = _gameHandler.GetActiveGame();
            if (game == null) return;

            var powerUpPlayerId = GetPlayerId(Context.User.Id, Context.Guild.Id);
            var powerUpPlayer = game.GetPlayer(powerUpPlayerId);

            var stealFromPlayerId = GetPlayerId(stealFrom.Id, Context.Guild.Id);

            if (!game.HasPlayerWithDice(stealFromPlayerId) || stealFrom.IsBot)
            {
                await RespondAsync($"Steal target must be a human player with dice.", ephemeral: true);
                return;
            }

            var previousBid = game.GetPreviousBid();

            if (previousBid?.GamePlayer.PlayerId == stealFromPlayerId)
            {
                await RespondAsync($"You can't steal from a player that just bid.", ephemeral: true);
                return;
            }

            var stealFromPlayer = game.GetPlayer(stealFromPlayerId);
            var numberToSteal = Math.Min(3, stealFromPlayer.Dice.Count - 1);

            if (numberToSteal < 1)
            {
                await RespondAsync($"Steal target must be a player with more than one dice.", ephemeral: true);
                return;
            }

            if (!await AbleToUsePowerUp(game, powerUpPlayer, PowerUps.Steal)) return;

            var stolenDice = game.RemoveRandomDice(stealFromPlayerId, numberToSteal);
            game.AddDice(powerUpPlayerId, stolenDice, isMystery: true);

            await RespondAsync($":zap: **Steal**: {powerUpPlayer.Name} takes {numberToSteal} mystery dice from {stealFromPlayer.Name}.");

            stealFromPlayer = game.GetPlayer(stealFromPlayerId);
            powerUpPlayer = game.GetPlayer(powerUpPlayer.PlayerId);

            var playersToUpates = new List<PlayerData> { powerUpPlayer, stealFromPlayer };
            await SendOutDice(playersToUpates, isUpdate: true);
        }

        [SlashCommand("gamble", "Transform your dice unpredictably, use `!odds` to find out more")]
        public async Task Gamble()
        {
            SetGuildAndChannel();
            var game = _gameHandler.GetActiveGame();
            if (game == null) return;

            var powerUpPlayerId = GetPlayerId(Context.User.Id, Context.Guild.Id);
            var powerUpPlayer = game.GetPlayer(powerUpPlayerId);

            if (!await AbleToUsePowerUp(game, powerUpPlayer, PowerUps.Gamble)) return;

            var playerDiceCount = powerUpPlayer.Dice.Count;
            var chanceRoll = _random.Next(0, 100);

            if (chanceRoll < 5)
            {
                game.RemoveRandomDice(powerUpPlayerId, playerDiceCount);
                for (int i = 0; i < playerDiceCount; i++) game.AddDice(powerUpPlayerId, new List<int> { 1 });
                await RespondAsync($":zap: **Gamble**: {powerUpPlayer.Name} rerolled their dice.");
            }
            else if (chanceRoll < 10)
            {
                game.AddRandomDice(powerUpPlayerId, 2);
                await RespondAsync($":zap: **Gamble**: {powerUpPlayer.Name} gained 2 new dice.");
            }
            else if (chanceRoll < 20)
            {
                game.AddRandomDice(powerUpPlayerId, 2, isMystery: true);
                await RespondAsync($":zap: **Gamble**: {powerUpPlayer.Name} gained 2 new dice.");
            }
            else if (chanceRoll < 35)
            {
                game.AddRandomDice(powerUpPlayerId, 1);
                await RespondAsync($":zap: **Gamble**: {powerUpPlayer.Name} gained 1 new die.");
            }
            else if (chanceRoll < 55)
            {
                game.AddRandomDice(powerUpPlayerId, 1, isMystery: true);
                await RespondAsync($":zap: **Gamble**: {powerUpPlayer.Name} gained 1 new die.");
            }
            else
            {
                game.RemoveRandomDice(powerUpPlayerId, playerDiceCount);
                game.AddRandomDice(powerUpPlayerId, playerDiceCount);
                await RespondAsync($":zap: **Gamble**: {powerUpPlayer.Name} rerolled their dice.");
            }

            powerUpPlayer = game.GetPlayer(powerUpPlayer.PlayerId);

            var playersToUpates = new List<PlayerData> { powerUpPlayer };
            await SendOutDice(playersToUpates, isUpdate: true);
        }

        [SlashCommand("reverse", "Reverse player order, takes effect immediately")]
        public async Task Reverse()
        {
            SetGuildAndChannel();
            var game = _gameHandler.GetActiveGame();
            if (game == null) return;

            var powerUpPlayerId = GetPlayerId(Context.User.Id, Context.Guild.Id);
            var powerUpPlayer = game.GetPlayer(powerUpPlayerId);

            if (!await AbleToUsePowerUp(game, powerUpPlayer, PowerUps.Reverse)) return;

            game.ReversePlayerOrder();
            await RespondAsync($":zap: **Reverse**: {powerUpPlayer.Name} changed player order.");
        }

        [SlashCommand("lifetap", "Permanently lose a life to get 3 dice this turn")]
        public async Task Lifetap()
        {
            SetGuildAndChannel();
            var game = _gameHandler.GetActiveGame();
            if (game == null) return;

            var powerUpPlayerId = GetPlayerId(Context.User.Id, Context.Guild.Id);
            var powerUpPlayer = game.GetPlayer(powerUpPlayerId);

            var playerDiceCount = powerUpPlayer.NumberOfDice;

            if ((playerDiceCount >= 5 && game.GetMode() == GameMode.Reverse) || (playerDiceCount != 5 && game.GetMode() != GameMode.Reverse))
            {
                await RespondAsync($"Unable to use lifetap", ephemeral: true);
                return;
            }

            if (!await AbleToUsePowerUp(game, powerUpPlayer, PowerUps.Lifetap)) return;

            game.AddRandomDice(powerUpPlayerId, 3);

            if (game.GetMode() == GameMode.Reverse)
            {
                game.SetPlayerDice(powerUpPlayerId, playerDiceCount + 1);
                await RespondAsync($":zap: **Lifetap**: {powerUpPlayer.Name} loses a life to get 3 random dice this round.");
            }
            else
            {
                game.SetPlayerDice(powerUpPlayerId, 1);
                await SendMessageAsync($":zap: **Lifetap**: {powerUpPlayer.Name} loses all their lives but one to get 3 random dice this round.");
            }

            powerUpPlayer = game.GetPlayer(powerUpPlayer.PlayerId);

            var playersToUpates = new List<PlayerData> { powerUpPlayer };
            await SendOutDice(playersToUpates, isUpdate: true);
        }

        [SlashCommand("greed", "Permanently lose a life to get points")]
        public async Task Greed()
        {
            SetGuildAndChannel();
            var game = _gameHandler.GetActiveGame();
            if (game == null) return;

            var powerUpPlayerId = GetPlayerId(Context.User.Id, Context.Guild.Id);
            var powerUpPlayer = game.GetPlayer(powerUpPlayerId);

            var playerDiceCount = powerUpPlayer.NumberOfDice;

            if ((playerDiceCount >= 5 && game.GetMode() == GameMode.Reverse) || (playerDiceCount != 5 && game.GetMode() != GameMode.Reverse))
            {
                await RespondAsync($"Unable to use lifetap", ephemeral: true);
                return;
            }

            if (!await AbleToUsePowerUp(game, powerUpPlayer, PowerUps.Greed)) return;

            AddTotalPoints(powerUpPlayerId, PowerUps.GREED_POINTS);

            if (game.GetMode() == GameMode.Reverse)
            {
                game.SetPlayerDice(powerUpPlayerId, playerDiceCount + 1);
                await RespondAsync($":zap: **Greed**: {powerUpPlayer.Name} loses a life to get {PowerUps.GREED_POINTS} points.");
            }
            else
            {
                game.SetPlayerDice(powerUpPlayerId, 1);
                await RespondAsync($":zap: **Greed**: {powerUpPlayer.Name} loses all their lives but one to get {PowerUps.GREED_POINTS} points.");
            }
        }

        [SlashCommand("touch", "If you're going off the grid, might want to check the temperature first")]
        public async Task Touch(string param)
        {
            if (!int.TryParse(param, out int touchPips))
            {
                await RespondAsync($"Touch target must be a pip (2-6)", ephemeral: true);
                return;
            }

            if (touchPips < 1 || touchPips > 6)
            {
                await RespondAsync($"Touch target must be a pip (2-6)", ephemeral: true);
                return;
            }

            SetGuildAndChannel();
            var game = _gameHandler.GetActiveGame();
            if (game == null) return;

            if (touchPips == 1)
            {
                await RespondAsync($"You can't touch ones.", ephemeral: true);
                return;
            }

            var bids = game.GetCurrentRound().Actions.OfType<Bid>().Select(x => x.Pips).ToList();

            if (bids.Contains(touchPips))
            {
                await RespondAsync($"You can't touch a number that was bid this round.", ephemeral: true);
                return;
            }

            var powerUpPlayerId = GetPlayerId(Context.User.Id, Context.Guild.Id);
            var powerUpPlayer = game.GetPlayer(powerUpPlayerId);

            if (!await AbleToUsePowerUp(game, powerUpPlayer, PowerUps.Touch)) return;

            var allDice = game.GetAllDice();
            var touchedCount = allDice.Where(x => x == touchPips || x == 1).Count();

            var quickMath = allDice.Count / 3.0;

            if (touchedCount > quickMath)
            {
                await RespondAsync($":zap: **Touch**: {powerUpPlayer.Name} checked {touchPips}s and they felt :fire:");
            }
            else
            {
                await RespondAsync($":zap: **Touch**: {powerUpPlayer.Name} checked {touchPips}s and they felt :ice_cube:");
            }
        }

        [SlashCommand("powerups", "Power up information")]
        public async Task PowerUpInfo()
        {
            var builder = new EmbedBuilder().WithTitle($"Power Up Information");
            var powerUpList = PowerUps.PowerUpList.OrderByDescending(x => x.Cost);

            foreach (var powerUp in powerUpList)
            {
                builder.AddField($":zap: {powerUp.Name} - `{powerUp.Cost}` pts", $"{powerUp.Description}");
            }

            var embed = builder.Build();
            await RespondAsync(embed: embed, ephemeral: true);
        }

        [SlashCommand("odds", "Gamble odds")]
        public async Task GambleOdds()
        {
            var odds = "**Gamble Odds**\n" +
                        "```\n" +
                        "45% Reroll all of your dice \n" +
                        "20% Gain a new mystery die \n" +
                        "15% Gain a new regular die \n" +
                        "10% Gain two new mystery dice \n" +
                        " 5% Change all your dice to 1s \n" +
                        " 5% Gain two new regular dice \n" +
                        "```";

            await RespondAsync(odds, ephemeral: true);
        }

        private async Task<bool> AbleToUsePowerUp(GameObject game, PlayerData player, PowerUp powerUp)
        {
            var activePlayers = game.GetAllPlayers().Where(x => x.NumberOfDice > 0).Count();

            if (activePlayers < powerUp.MinPlayers)
            {
                await RespondAsync($"You can only use :zap: {powerUp.Name} with {powerUp.MinPlayers} or more players remaining", ephemeral: true);
                return false;
            }

            var currentPlayer = game.GetCurrentPlayer();
            if (!powerUp.OutOfTurn && currentPlayer.PlayerId != player.PlayerId)
            {
                await RespondAsync($"You can only use :zap: {powerUp.Name} on your own turn", ephemeral: true);
                return false;
            }

            if (player.Dice.Count < powerUp.MinDice)
            {
                await RespondAsync($"You can only use :zap: {powerUp.Name} with {powerUp.MinDice} or more dice", ephemeral: true);
                return false;
            }

            if (GetAvailablePoints(player.PlayerId) < powerUp.Cost)
            {
                await RespondAsync($"You don't have enough points to use :zap: {powerUp.Name}", ephemeral: true);
                return false;
            }

            var roundNumber = game.GetCurrentRound().RoundNumber;

            var specifcPowerPerGame = $"{powerUp.Name}";
            var anyPowerPerRound = $"{roundNumber}";
            var specificPowerPerRound = $"{roundNumber}-{powerUp.Name}";

            if (GetPowerUpUses(game, player.PlayerId, specifcPowerPerGame) >= powerUp.UsesPerGame)
            {
                await RespondAsync($"You have reached use limit for :zap: {powerUp.Name} this game.", ephemeral: true);
                return false;
            }

            if (GetPowerUpUses(game, player.PlayerId, anyPowerPerRound) >= PowerUps.TOTAL_USES_PER_ROUND)
            {
                await RespondAsync($"You have reached power up use limit for this round.", ephemeral: true);
                return false;
            }

            if (GetPowerUpUses(game, player.PlayerId, specificPowerPerRound) >= powerUp.UsesPerRound)
            {
                await RespondAsync($"You have reached use limit for :zap: {powerUp.Name} this round.", ephemeral: true);
                return false;
            }

            if (powerUp.Cost > 0)
            {
                AddUsedPoints(player.PlayerId, powerUp.Cost);
            }

            AddPowerUpUses(game, player.PlayerId, specifcPowerPerGame);
            AddPowerUpUses(game, player.PlayerId, anyPowerPerRound);
            AddPowerUpUses(game, player.PlayerId, specificPowerPerRound);

            return true;
        }

        private int GetPowerUpUses(GameObject game, int playerId, string usesKey)
        {
            var metaDataKey = $"{playerId}-{usesKey}-uses";

            var numUsedString = game.GetMetadata(metaDataKey);

            if (string.IsNullOrEmpty(numUsedString))
            {
                game.SetMetadata(metaDataKey, "0");
                return 0;
            }

            return int.Parse(numUsedString);
        }

        private void AddPowerUpUses(GameObject game, int playerId, string usesKey, int value = 1)
        {
            var metaDataKey = $"{playerId}-{usesKey}-uses";

            var currentyPowerUpUses = GetPowerUpUses(game, playerId, usesKey);
            game.SetMetadata(metaDataKey, (currentyPowerUpUses + value).ToString());
        }
    }
}
