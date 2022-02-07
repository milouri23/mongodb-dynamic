using DynamicMongoTests.Constants;
using DynamicMongoTests.Enums;
using DynamicMongoTests.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DynamicMongoTests;

public class SerializationFixedAndExtraElementsTest : IDisposable
{
    private readonly IMongoDatabase _mongoDatabase;
    private readonly IMongoCollection<BsonDocument> _mongoCollection;
    private readonly IEnumerable<BsonDocument> _allDocuments;

    private readonly IMongoCollection<ClientInfo> _clientsInfoCollection;
    private readonly IMongoCollection<ClientInfoBetter> _clientsInfoBetterCollection;

    public SerializationFixedAndExtraElementsTest()
    {
        IMongoClient mongoClient = new MongoClient(TestConstant.MongoUri);

        _mongoDatabase = mongoClient.GetDatabase(TestConstant.MongoDatabase);

        _mongoCollection = _mongoDatabase.GetCollection<BsonDocument>("ClientsInfo");

        _allDocuments = DocumentDataRepository.GetAllDynamicWithStaticPartDocuments();
        _mongoCollection.InsertMany(_allDocuments);

        ConventionRegistry.Register("EnumStringConvention",
            new ConventionPack { new EnumRepresentationConvention(BsonType.String) }, t => true);
        _clientsInfoCollection = _mongoDatabase.GetCollection<ClientInfo>("ClientsInfo");

        // Only the next collection would be register with the camelCase Convention
        ConventionRegistry.Register("camelCase", new ConventionPack { new CamelCaseElementNameConvention() }, t => true);
        _clientsInfoBetterCollection = _mongoDatabase.GetCollection<ClientInfoBetter>("ClientsInfo");
    }

    public void Dispose() =>
        _mongoDatabase.DropCollection("ClientsInfo");

    [Fact]
    public async Task ShouldNotDeserializeCamelCaseConventionNotSet()
    {
        List<ClientInfo> clientsInfo = await _clientsInfoCollection.Find(_ => true).ToListAsync();
        var additionalsInfo = clientsInfo.Select(clientInfo => clientInfo.AdditionalInfo);

        Assert.Equal(_allDocuments.Count(), clientsInfo.Count);
    }

    [Fact]
    public async Task ShouldDeserializeCamelCaseConventionSet()
    {
        List<ClientInfoBetter> clientsInfo = await _clientsInfoBetterCollection.Find(_ => true).ToListAsync();

        Assert.Equal(_allDocuments.Count(), clientsInfo.Count);
    }

    // Breakpoint on dispose method to see the changes
    [Fact]
    public async Task StoreEnumAsStringAlternative1()
    {
        // Note the attribute on ClientInfoBetter's ClientType property
        await _clientsInfoBetterCollection.InsertOneAsync(new ClientInfoBetter
        {
            ClientType = ClientType.Platinum,
            TotalPurchases = 102,
            AdditionalInfo = new Dictionary<string, object>(new KeyValuePair<string, object>[] { new("hidden", true) })
        });

        List<ClientInfoBetter> clientsInfo = await _clientsInfoBetterCollection.Find(_ => true).ToListAsync();

        Assert.Equal(_allDocuments.Count() + 1, clientsInfo.Count);
    }

    // Breakpoint on dispose method to see the changes
    [Fact]
    public async Task StoreEnumAsStringAlternative2()
    {
        // Note that ClientInfo's ClientType property doesn't have the attribute
        // The convention in the constructor of this test do the trick
        await _clientsInfoCollection.InsertOneAsync(new ClientInfo
        {
            ClientType = ClientType.Platinum,
            TotalPurchases = 102,
            AdditionalInfo = new Dictionary<string, object>(new KeyValuePair<string, object>[] { new("hidden", true) })
        });

        List<ClientInfo> clientsInfo = await _clientsInfoCollection.Find(_ => true).ToListAsync();

        Assert.Equal(_allDocuments.Count() + 1, clientsInfo.Count);
    }
}

public class ClientInfo
{
    public ClientType ClientType { get; set; }
    public int TotalPurchases { get; set; }

    [BsonExtraElements]
    public IDictionary<string, object> AdditionalInfo { get; set; }

    // A Bson Document could be used instead
    //[BsonExtraElements]
    //public BsonDocument AdditionalInfo { get; set; }
}

[BsonIgnoreExtraElements] // I do not want to map the _id field
public class ClientInfoBetter
{
    //[JsonConverter(typeof(StringEnumConverter))]    // JSON.NET
    [BsonRepresentation(BsonType.String)]           // Mongo
    public ClientType ClientType { get; set; }

    public int TotalPurchases { get; set; }

    //[BsonExtraElements()]
    // A dinamic dictionary that has 'additionalInfo' name in database
    public IDictionary<string, object> AdditionalInfo { get; set; }

    // Could be used a Bson document that has 'additionalInfo' name in database
    //public BsonDocument AdditionalInfo { get; set; }
}