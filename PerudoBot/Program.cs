using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PerudoBot.Database.Data;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Perudobot
{
    public class Program
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _services;

        private readonly DiscordSocketConfig _socketConfig = new()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
            AlwaysDownloadUsers = true,
        };

        public Program()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            //var memory = new MemoryCache(new MemoryCacheOptions());
            _services = new ServiceCollection()
                .AddMemoryCache()
                .AddSingleton(_configuration)
                .AddSingleton(_socketConfig)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddEntityFrameworkSqlite()
                .AddDbContext<PerudoBotDbContext>(options =>
                    options.UseLazyLoadingProxies()
                        .UseSqlite(_configuration.GetConnectionString("PerudoBotDb")))
                .BuildServiceProvider();

            var db = _services.GetRequiredService<PerudoBotDbContext>();
            db.Database.Migrate();
        }

        static void Main(string[] args)
        {
            new Program().RunAsync()
                .GetAwaiter()
                .GetResult();
        }

        public async Task RunAsync()
        {
            var client = _services.GetRequiredService<DiscordSocketClient>();

            client.Log += LogAsync;

            // Here we can initialize the service that will register and execute our commands
            await _services.GetRequiredService<InteractionHandler>()
                .InitializeAsync();

            // Bot token can be provided from the Configuration object we set up earlier
            await client.LoginAsync(TokenType.Bot, _configuration["DiscordToken"]);
            await client.StartAsync();

            // Never quit the program until manually forced to.
            await Task.Delay(Timeout.Infinite);
        }

        private async Task LogAsync(LogMessage message)
        {
            Console.WriteLine(message.ToString());
        }

        public static bool IsDebug()
        {
#if DEBUG
            return true;
#else
                return false;
#endif
        }
    }
}