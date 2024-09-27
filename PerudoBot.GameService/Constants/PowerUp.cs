using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerudoBot.GameService.Constants
{
    public class PowerUp
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; } = 0;
        public bool OutOfTurn { get; set; } = false;
        public int MinPlayers { get; set; } = 3;
        public int MinDice { get; set; } = 1;
        public int UsesPerRound { get; set; } = 99;
        public int UsesPerGame { get; set; } = 99;
    }

    public static class PowerUps
    {
        public static int TOTAL_USES_PER_ROUND = 1;
        public static int GREED_POINTS = 50;

        public static PowerUp Skip = new PowerUp
        {
            Name = "Skip",
            Description = "Permanently lose a life to skip your turn",
            Cost = 30
        };

        public static PowerUp Touch = new PowerUp
        {
            Name = "Touch",
            Description = "If you're going off the grid, might want to check the temperature first",
            Cost = 20
        };

        public static PowerUp Reverse = new PowerUp
        {
            Name = "Reverse",
            Description = "Reverse player order, takes effect immediately",
            Cost = 10
        };

        public static PowerUp Steal = new PowerUp
        {
            Name = "Steal",
            Description = "Steal up to 3 dice from target player, new dice are mystery",
            Cost = 20
        };

        public static PowerUp Gamble = new PowerUp
        {
            Name = "Gamble",
            Description = "Reroll your dice with a small chance for a lucky outcome",
            Cost = 30
        };

        public static PowerUp Greed = new PowerUp
        {
            Name = "Greed",
            Description = $"Permanently lose a life to get {GREED_POINTS} points once per game",
            UsesPerGame = 1
        };

        public static List<PowerUp> PowerUpList = new List<PowerUp>
        {
             Steal, Gamble, Touch, Skip, Reverse, Greed
        };
    }
}
