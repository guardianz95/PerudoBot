﻿using Discord.Interactions;
using Newtonsoft.Json;
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

        [SlashCommand("bid", "Place a bid")]
        public async Task Bid(string bidParam)
        {
            SetGuildAndChannel();
            var game = _gameHandler.GetActiveGame();

            await RespondAsync($"Bidding...", ephemeral: true);

            if (game == null)
            {
                await FollowupAsync("No active game", ephemeral: true);
                return;
            }

            var currentPlayer = game.GetCurrentPlayer();
            var userId = GetUserId(currentPlayer);

            if (Context.User.Id != userId)
            {
                await FollowupAsync("Not your turn", ephemeral: true);
                return;
            }

            var bidText = bidParam.Split(" ");
            if (bidText.Length < 2)
            {
                await FollowupAsync("Invalid bid", ephemeral: true);
                return;
            }

            var quantity = int.Parse(bidText[0]);
            var pips = int.Parse(bidText[1].Trim('s'));

            var isValid = game.BidValidate(currentPlayer.PlayerId, quantity, pips);

            if (!isValid)
            {
                await FollowupAsync("Invalid bid", ephemeral: true);
                return;
            }

            game.Bid(currentPlayer.PlayerId, quantity, pips);
            var nextPlayer = game.GetCurrentPlayer();

            await FollowupAsync($"{currentPlayer.Name} bids `{quantity}` ˣ { pips.ToEmoji() }. { nextPlayer.GetMention(_db)} is up.");
        }
    }
}
