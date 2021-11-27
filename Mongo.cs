using MongoDB.Driver;

namespace InscryptionBot;

public static class Mongo
{
    public static MongoClient Conn { get; } = new($"mongodb://root:{Environment.GetEnvironmentVariable("MONGO_INITDB_ROOT_PASSWORD")}@mongo:27017");
    public static IMongoDatabase Database { get; } = Conn.GetDatabase("inscryption");
}
