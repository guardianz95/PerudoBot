using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Perudobot;
using PerudoBot.Database.Data;
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
        private readonly PerudoBotDbContext _db;
        private readonly IMemoryCache _cache;

        private readonly GameHandler _gameHandler;

        public Commands(IServiceProvider serviceProvider)
        {
            _cache = serviceProvider.GetRequiredService<IMemoryCache>();
            _db = serviceProvider.GetRequiredService<PerudoBotDbContext>();

            _gameHandler = new GameHandler(_db, _cache);
        }

        private void SetGuildAndChannel()
        {
            _gameHandler.SetChannel(Context.Channel.Id);
            _gameHandler.SetGuild(Context.Guild.Id);
        }

        private ulong GetUserId(PlayerData currentPlayer)
        {
            return _db.Players
                .Single(x => x.Id == currentPlayer.PlayerId)
                .DiscordPlayer.UserId;
        }
        private int GetPlayerId(ulong userId, ulong guildId)
        {
            return _db.Players
                .AsQueryable()
                .Where(x => x.DiscordPlayer.GuildId == guildId)
                .Single(x => x.DiscordPlayer.UserId == userId)
                .Id;
        }

        private async Task<IUserMessage> SendMessageAsync(string message = null, Embed embed = null, MessageComponent components = null)
        {
            var requestOptions = new RequestOptions() { RetryMode = RetryMode.RetryRatelimit };
            return await Context.Channel.SendMessageAsync(message, options: requestOptions, embed: embed, components: components);
        }
    }
}
