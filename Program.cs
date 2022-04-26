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
        var messageChecker = new ExcessiveMessageChecker();

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

            var mcTask = new Task(() => messageChecker.CheckMessage(message));
            mcTask.RunSynchronously();

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
                                string msgText = $"{recipe.Name} by {recipe.AuthorName}:\n{recipe.Content}";
                                if (msgText.Length > 2000)
                                {
                                    msgText = msgText.Substring(0, 2000);
                                    await message.Channel.SendMessageAsync($"WARNING! Message is too long. It will be trimmed to 2000 characters. Please edit it to be shorter.");
                                }
                                await message.Channel.SendMessageAsync(msgText);
                            }
                            break;
                        default:
                            await message.Channel.SendMessageAsync($"{result.Error}: {result.ErrorReason}");
                            break;
                    }
                }
            }
        };

        client.ReactionAdded += async (msg, _, reaction) =>
        {
            if (reaction.User.Value is not SocketGuildUser user || user.Id == client.CurrentUser.Id)
                return;

            var role = await RoleReact.GetReactRoleAsync(reaction);
            if (role is null)
                return;
            
            await user.AddRoleAsync(role.Value);
        };

        client.ReactionRemoved += async (msg, _, reaction) =>
        {
            if (reaction.User.Value is not SocketGuildUser user || user.Id == client.CurrentUser.Id)
                return;

            var role = await RoleReact.GetReactRoleAsync(reaction);
            if (role is null)
                return;
            
            await user.RemoveRoleAsync(role.Value);
        };

        await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_TOKEN"));
        await client.StartAsync();
        await Task.Delay(-1);
    }
}
