using Discord;
using Discord.Commands;
using Discord.WebSocket;
using InscryptionBot.Modules;
using MongoDB.Driver;

namespace InscryptionBot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var client = new DiscordSocketClient();

        client.Log += static async e =>
        {
            await Console.Out.WriteLineAsync(e.ToString());
        };
        
        var commands = new CommandService(new CommandServiceConfig
        {
            DefaultRunMode = RunMode.Async
        });
        _ = commands.AddModulesAsync(typeof(Program).Assembly, null);

        client.MessageReceived += async msg =>
        {
            if (msg is not SocketUserMessage message || msg.Author.Id == client.CurrentUser.Id)
                return;

            int argPos = 0;
            if (message.HasCharPrefix('%', ref argPos))
            {
                var result = await commands.ExecuteAsync(new SocketCommandContext(client, message), argPos, null);

                if (!result.IsSuccess)
                {
                    switch (result.Error)
                    {
                        case CommandError.UnknownCommand:
                            var recipeName = message.Content.Split(' ')[0][argPos..];
                            var filter = Builders<Recipes.Recipe>.Filter.Eq(nameof(Recipes.Recipe.Name), recipeName);
                            var recipe = await (await Mongo.Database.GetCollection<Recipes.Recipe>("recipes").FindAsync(filter)).FirstOrDefaultAsync();
                            if (recipe != default)
                            {
                                await message.Channel.SendMessageAsync($"{recipe.Name} by {recipe.AuthorName}:\n{recipe.Content}");
                            }
                            break;
                        default:
                            await message.Channel.SendMessageAsync($"{result.Error}: {result.ErrorReason}");
                            break;
                    }
                }
            }
        };

        await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_TOKEN"));
        await client.StartAsync();
        await Task.Delay(-1);
    }
}
