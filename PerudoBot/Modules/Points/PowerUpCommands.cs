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
        [SlashCommand("powerups", "Power up information")]
        public async Task PowerUpInfo()
        {
            await RespondAsync("Power ups currently unavailable", ephemeral: true);
        }
    }
}
