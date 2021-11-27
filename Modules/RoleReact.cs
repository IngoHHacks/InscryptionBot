using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace InscryptionBot.Modules;

[Group("role_react"), RequireUserPermission(GuildPermission.ManageRoles)]
public class RoleReact : ModuleBase<SocketCommandContext>
{
    private class RoleMessage
    {
        [BsonId]
        public ulong MessageId;
        public Dictionary<string, ulong> RoleReactions;
    }

    private const string RoleReactCollection = "role_reactions";
    
    [Command("add")]
    public async Task AddRoleReact(ulong messageId, ulong roleId, [Remainder] string reactIcon)
    {
        var roleReacts = Mongo.Database.GetCollection<RoleMessage>(RoleReactCollection);
        var filter = Builders<RoleMessage>.Filter.Eq(nameof(RoleMessage.MessageId), messageId);
        var existing = await (await roleReacts.FindAsync(filter)).FirstOrDefaultAsync();
        existing ??= new RoleMessage
        {
            MessageId = messageId,
            RoleReactions = new Dictionary<string, ulong>()
            {
                { reactIcon, roleId }
            }
        };

        await roleReacts.FindOneAndReplaceAsync(filter, existing, new FindOneAndReplaceOptions<RoleMessage>()
        {
            IsUpsert = true
        });

        IEmote iemote;
        if (Emote.TryParse(reactIcon, out var emote))
            iemote = emote;
        else
            iemote = new Emoji(reactIcon);
        await Context.Client.Rest.AddReactionAsync(Context.Channel.Id, messageId, iemote);
        await Context.Channel.SendMessageAsync($"Added {iemote} as a reaction role");
    }

    [Command("remove_all")]
    public async Task RemoveAllRoleReacts(ulong messageId)
    {
        var roleReacts = Mongo.Database.GetCollection<RoleMessage>(RoleReactCollection);
        var filter = Builders<RoleMessage>.Filter.Eq(nameof(RoleMessage.MessageId), messageId);
        await roleReacts.DeleteOneAsync(filter);
        await Context.Channel.SendMessageAsync("Removed all role reactions");
    }

    public static async Task<ulong?> GetReactRoleAsync(SocketReaction reaction)
    {
        var roleReacts = Mongo.Database.GetCollection<RoleMessage>(RoleReactCollection);
        var filter = Builders<RoleReact.RoleMessage>.Filter.Eq(nameof(RoleMessage.MessageId), reaction.MessageId);
        var roleReact = await (await roleReacts.FindAsync(filter)).FirstOrDefaultAsync();
        if (roleReact == default)
            return null;

        string reactStr = reaction.Emote.ToString()!;

        if (!roleReact.RoleReactions.TryGetValue(reactStr, out var roleId))
            return null;

        return roleId;
    }
}
