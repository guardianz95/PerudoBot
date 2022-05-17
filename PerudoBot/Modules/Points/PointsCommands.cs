using Discord.Commands;
using Discord.WebSocket;
using PerudoBot.GameService;
using System.Linq;
using System.Threading.Tasks;
namespace PerudoBot.Modules
{
    public partial class Commands : ModuleBase<SocketCommandContext>
    {
        private async Task AwardPoints(GameObject game)
        {
            var gamePlayers = game.GetAllPlayers()
                .OrderBy(x => x.Rank)
                .ToList();

            if(gamePlayers.Count == 1)
            {
                var gamePlayer = gamePlayers.First();
                await SendMessageAsync($"{gamePlayer.Name} has been awarded no points for participating in an incomplete game.");
                return;
            }

            var message = "`Points:`";
            foreach (var gamePlayer in gamePlayers)
            {
                var player = _db.Players.First(x => x.Id == gamePlayer.PlayerId);
                var originalPoints = player.Points;

                var awardedPoints = (gamePlayers.Count() - gamePlayer.Rank + 3) * 10;
                player.Points += awardedPoints;

                _db.SaveChanges(); ;

                message += $"\n`{gamePlayer.Rank}` {gamePlayer.Name} `{originalPoints}` => `{player.Points}` ({awardedPoints})";
            }

            await SendMessageAsync(message);
        }

        public void AddPoints(int playerId, int points)
        {
            var player = _db.Players.First(x => x.Id == playerId);
            player.Points += points;
            _db.SaveChanges();
        }

        [Command("points")]
        public async Task Points(params string[] options)
        {
            var players = _db.Players
                .ToList()
                .OrderByDescending(x => x.Points)
                .ToList();

            var message = "`Points:`";

            foreach (var player in players)
            {
                message += $"\n{player.Name}: `{player.Points}`";
            }
            await SendMessageAsync(message);
        }
    }
}