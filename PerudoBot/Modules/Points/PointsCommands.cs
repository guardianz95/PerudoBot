using Discord.Interactions;
using System.Linq;

namespace PerudoBot.Modules
{
    public partial class Commands : InteractionModuleBase<SocketInteractionContext>
    {
        public int GetAvailablePoints(int playerId)
        {
            return _db.Players.First(x => x.Id == playerId).AvailablePoints;
        }

        public void AddTotalPoints(int playerId, int points)
        {
            var player = _db.Players.First(x => x.Id == playerId);
            player.TotalPoints += points;
            _db.SaveChanges();
        }

        public void AddUsedPoints(int playerId, int points)
        {
            var player = _db.Players.First(x => x.Id == playerId);
            player.UsedPoints += points;
            _db.SaveChanges();
        }
    }
}
