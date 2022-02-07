using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBDriverReferenceTests.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MongoDBDriverReferenceTests;

public class QuickTourTestPt2Test : IDisposable
{
    private readonly IMongoDatabase _mongoDatabase;
    private readonly IMongoCollection<BsonDocument> _mongoCollection;

    public QuickTourTestPt2Test()
    {
        IMongoClient mongoClient = new MongoClient(TestConstant.MongoUri);

        _mongoDatabase = mongoClient.GetDatabase(TestConstant.MongoDatabase);

        _mongoCollection = _mongoDatabase.GetCollection<BsonDocument>("Parte2");
    }

    public void Dispose()
    {
        _mongoDatabase.DropCollection("Parte2");
    }

    [Fact]
    public async Task ShouldInsertMultipleDocuments()
    {
        // As an enumerable the id is not retreived
        IEnumerable<BsonDocument> documents = Enumerable.Range(0, 100).Select(i => new BsonDocument("counter", i));

        Assert.False(documents.First().TryGetValue("_id", out BsonValue value));

        await _mongoCollection.InsertManyAsync(documents);

        Assert.False(documents.First().TryGetValue("_id", out value));

        Assert.False(documents.ToList()[0].TryGetValue("_id", out value));

        _mongoDatabase.DropCollection("Parte2");

        // As a list the id is retreived
        List<BsonDocument> documentList = documents.ToList();

        Assert.False(documentList.First().TryGetValue("_id", out value));

        await _mongoCollection.InsertManyAsync(documentList);

        Assert.True(documentList.First().TryGetValue("_id", out value));

        // We can count the documents on the collection
        long documentsCount = await _mongoCollection.CountDocumentsAsync(filter: new BsonDocument());

        Assert.Equal(100, documentsCount);
    }
}