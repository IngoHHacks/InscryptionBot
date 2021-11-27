using Discord.Commands;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace InscryptionBot.Modules;

[Group("r")]
public class Recipes : ModuleBase<SocketCommandContext>
{
    public class Recipe
    {
        [BsonId]
        public string Name;
        public string AuthorName;
        public ulong AuthorId;
        public string Content;
    }

    [Command("add")]
    public async Task AddRecipeAsync(string name, [Remainder] string content)
    {
        var recipes = Mongo.Database.GetCollection<Recipe>("recipes");

        var findRecipeByName = await recipes.FindAsync(Builders<Recipe>.Filter.Eq(nameof(Recipe.Name), name));
        if (await findRecipeByName.AnyAsync())
        {
            await Context.Channel.SendMessageAsync($"A recipe with the name {name} already exists, please choose a different name or use `%r edit` to edit if you own this recipe.");
            return;
        }

        await recipes.InsertOneAsync(new Recipe()
        {
            Name = name,
            AuthorName = $"{Context.User.Username}#{Context.User.Discriminator}",
            AuthorId = Context.User.Id,
            Content = content
        });

        await Context.Channel.SendMessageAsync($"Added new recipe called {name}");
    }
}
