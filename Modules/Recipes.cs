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

    private const string helpText = "`%r help`: Shows this list" + "\n" +
                                    "`%r add {name} {content}`: Adds a recipe that can then be used by just saying %{name}" + "\n" +
                                    "`%r edit {name} {content}`: Modifies the content of a recipe" + "\n" +
                                    "`%r remove {name}`: Removes a recipe";

    [Command("help")]
    public async Task ListRecipeCommandsAsync()
    {
        await Context.Channel.SendMessageAsync(helpText);
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

        string prefix = $"{name} by {Context.User.Username}#{Context.User.Discriminator}:\n";
        int maxLength = (2000 - prefix.Length);

        if (content.Length > maxLength)
        {
            content = content.Substring(0, maxLength);
            Context.Channel.SendMessageAsync($"WARNING! Message is too long. It will be trimmed to 2000 characters.");
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

    [Command("edit")]
    public async Task EditRecipeAsync(string name, [Remainder] string content)
    {
        var recipes = Mongo.Database.GetCollection<Recipe>("recipes");
        var filter = Builders<Recipe>.Filter.Eq(nameof(Recipe.Name), name);
        var foundrecipe = await (await recipes.FindAsync(filter)).FirstOrDefaultAsync();

        if (foundrecipe == default)
        {
            await Context.Channel.SendMessageAsync($"No recipe with the name {name} exists");
            return;
        }

        var owneruser = Context.Guild.GetUser(Context.User.Id);

        if (foundrecipe.AuthorId == Context.User.Id || owneruser.Roles.Any(r => r.Permissions.ManageMessages))
        {
            await recipes.UpdateOneAsync(filter, Builders<Recipe>.Update.Set(r => r.Content, content));
            await Context.Channel.SendMessageAsync($"Edited recipe called {name}");
        }
        else
        {
            await Context.Channel.SendMessageAsync($"You are not the owner of the recipe called {name} or do not have the required permissions");
        }

    }

    [Command("remove")]
    public async Task RemoveRecipeAsync(string name)
    {
        var recipes = Mongo.Database.GetCollection<Recipe>("recipes");
        var filter = Builders<Recipe>.Filter.Eq(nameof(Recipe.Name), name);
        var foundrecipe = await (await recipes.FindAsync(filter)).FirstOrDefaultAsync();

        if (foundrecipe == default)
        {
            await Context.Channel.SendMessageAsync($"No recipe with the name {name} exists");
            return;
        }

        var owneruser = Context.Guild.GetUser(Context.User.Id);

        if (foundrecipe.AuthorId == Context.User.Id || owneruser.Roles.Any(r => r.Permissions.ManageMessages))
        {
            await recipes.FindOneAndDeleteAsync(filter);
            await Context.Channel.SendMessageAsync($"Removed recipe called {name}");
        }
        else
        {
            await Context.Channel.SendMessageAsync($"You are not the owner of the recipe called {name} or do not have the required permissions");
        }

    }
}
