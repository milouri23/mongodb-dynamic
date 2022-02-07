using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBDriverReferenceTests.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MongoDBDriverReferenceTests;

public class QuickTourBasicOperationsTest : IDisposable
{
    private readonly IMongoDatabase _mongoDatabase;
    private readonly IMongoCollection<BsonDocument> _collection;

    public QuickTourBasicOperationsTest()
    {
        IMongoClient mongoClient = new MongoClient(TestConstant.MongoUri);

        _mongoDatabase = mongoClient.GetDatabase(TestConstant.MongoDatabase);

        _collection = _mongoDatabase.GetCollection<BsonDocument>("BasicOperations");

        BsonDocument document = new BsonDocument
            {
                { "name", "MongoDB" },
                { "type", "Database" }, // Element, with Name and Value properties
                { "count", 1},
                { "info", new BsonDocument
                    {
                        {"x", 203 },
                        {"y", 102 }
                    }
                }
            };

        _collection.InsertOne(document);

        _collection.InsertMany(Enumerable.Range(0, 100).Select(i => new BsonDocument("counter", i)));
    }

    public void Dispose() =>
        _mongoDatabase.DropCollection("BasicOperations");

    [Fact]
    public async Task ShouldFindFirstDocumentInCollection()
    {
        //_collection.Find(filter: new BsonDocument()).FirstOrDefault();

        BsonDocument document = await _collection.Find(filter: new BsonDocument()).FirstOrDefaultAsync();

        Assert.NotNull(document);
        Assert.Equal("Database", document.GetValue("type"));
        Assert.Equal("Database", document.GetValue(2));
    }

    [Fact]
    public async Task ShouldFinAllDocumentsInCollection()
    {
        //_collection.Find(filter: new BsonDocument()).ToList();

        // Useful when the number of documents expected to be returned is small
        List<BsonDocument> document = await _collection.Find(filter: new BsonDocument()).ToListAsync();

        // Useful when the number of documents expected to be returned is large
        // Invoques a callback for every element returned
        await _collection.Find(new BsonDocument()).ForEachAsync(CallbackMethod);

        // If you want to process it in a syncrhonous way (large number scenario too)
        var cursor = _collection.Find(new BsonDocument()).ToCursor();
        foreach (var doc in cursor.ToEnumerable())
        {
            // Do something
            // Console.WriteLine(doc); // For instance
        }

        Assert.NotEmpty(document);
        Assert.Equal(101, document.Count);
    }

    public void CallbackMethod(BsonDocument doc)
    {
        // Do Something
        // Console.WriteLine(doc); // For instance
    }

    // Use the Filter, Sort and Projection builders for simple
    // and concise ways building up queries
    [Fact]
    public async Task ShouldGetSingleDocumentWithFilter()
    {
        var filter = Builders<BsonDocument>.Filter.Eq("counter", 71);

        // collection.Find(filter).First();

        var document = await _collection.Find(filter).FirstOrDefaultAsync();

        // The next asserts are equivalent
        Assert.Equal(71, document["counter"]);
        Assert.Equal(71, document.GetValue("counter"));
        Assert.Equal(71, document[1]);
        Assert.Equal(71, document.GetValue(1));
        Assert.Equal(71, document.GetElement(1).Value);
        Assert.Equal(71, document.GetElement("counter").Value);
    }

    [Fact]
    public async Task ShouldGetASetOfDocumentsWithAFilter()
    {
        // First example
        var filter = Builders<BsonDocument>.Filter.Gt("counter", 50);

        // collection.Find(filter).ToList();

        var documents = await _collection.Find(filter).ToListAsync();

        Assert.All(collection: documents.Select(document => (int)document.GetValue("counter")),
            action: (counter) => Assert.InRange(counter, 51, 99));
        Assert.Equal(49, documents.Count);

        // Second example (compound filter)
        var filterBuilder = Builders<BsonDocument>.Filter;
        var secondFilter = filterBuilder.Gt("counter", 50) & filterBuilder.Lte("counter", 60);

        var documentsWithSecondFilter = await _collection.Find(secondFilter).ToListAsync();

        Assert.All(collection: documentsWithSecondFilter.Select(document => (int)document.GetValue("counter")),
            action: (counter) => Assert.InRange(counter, 51, 60));
        Assert.Equal(10, documentsWithSecondFilter.Count);
    }

    [Fact]
    public async Task ShouldSortDocuments()
    {
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Exists("counter");
        SortDefinition<BsonDocument> sort = Builders<BsonDocument>.Sort.Descending("counter");

        // collection.Find(filter).Sort(sort).ToList();

        List<BsonDocument> documents = await _collection.Find(filter).Sort(sort).ToListAsync();

        Assert.Equal(99, documents.First()["counter"]);
        Assert.Equal(0, documents.Last()["counter"]);

        BsonDocument topCounterDocument = await _collection.Find(filter).Sort(sort).FirstOrDefaultAsync();

        Assert.Equal(99, topCounterDocument["counter"]);
    }

    [Fact]
    public async Task ShouldProjectFields()
    {
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Exists("counter");
        SortDefinition<BsonDocument> sort = Builders<BsonDocument>.Sort.Descending("counter");
        ProjectionDefinition<BsonDocument> project = Builders<BsonDocument>.Projection.Exclude("_id");

        BsonDocument documentWithoutProjection =
            await _collection.Find(filter).Sort(sort).FirstOrDefaultAsync();

        Assert.True(documentWithoutProjection.TryGetValue("_id", out _));

        BsonDocument topCounterDocument = await _collection.Find(filter).Project(project).Sort(sort).FirstOrDefaultAsync();

        Assert.False(topCounterDocument.TryGetValue("_id", out _));
    }

    [Fact]
    public async Task ShouldUpdateAtMostOneDocument()
    {
        //FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Exists("counter");
        //SortDefinition<BsonDocument> sort = Builders<BsonDocument>.Sort.Descending("counter");
        //ProjectionDefinition<BsonDocument> project = Builders<BsonDocument>.Projection.Exclude("_id");

        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("counter", 10);
        UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set("counter", 110);

        UpdateResult updateResult = await _collection.UpdateOneAsync(filter, update);

        Assert.True(updateResult.IsModifiedCountAvailable); // Server is above 2.6
        Assert.Equal(1, updateResult.ModifiedCount); // one element was updated
        Assert.Equal(1, updateResult.MatchedCount); // Filter match with one element

        updateResult = await _collection.UpdateOneAsync(filter, update);

        Assert.True(updateResult.IsModifiedCountAvailable); // Server is above 2.6
        Assert.Equal(0, updateResult.ModifiedCount); // no element was updated
        Assert.Equal(0, updateResult.MatchedCount); // Filter match no element

        filter = Builders<BsonDocument>.Filter.Eq("counter", 110);

        updateResult = await _collection.UpdateOneAsync(filter, update);

        Assert.True(updateResult.IsModifiedCountAvailable); // Server is above 2.6
        Assert.Equal(0, updateResult.ModifiedCount); // no element was updated (it has that value already)
        Assert.Equal(1, updateResult.MatchedCount); // Filter match with one element
    }

    [Fact]
    public async Task ShouldUpdateManyDocuments()
    {
        var filter = Builders<BsonDocument>.Filter.Lt("counter", 100);
        var update = Builders<BsonDocument>.Update.Inc("counter", 100);

        UpdateResult updateResult = await _collection.UpdateManyAsync(filter, update);

        Assert.Equal(100, updateResult.MatchedCount);
        Assert.Equal(100, updateResult.ModifiedCount);

        BsonDocument topCounterDocument = await _collection
            .Find(Builders<BsonDocument>.Filter.Exists("counter"))
            //.Find(Builders<BsonDocument>.Filter.Empty)
            .Project(Builders<BsonDocument>.Projection.Exclude("_id"))
            .Sort(Builders<BsonDocument>.Sort.Descending("counter"))
            .Limit(1)
            .FirstOrDefaultAsync();

        Assert.Equal(199, topCounterDocument["counter"]);
        Assert.False(topCounterDocument.TryGetElement("_id", out _));
    }

    [Fact]
    public async Task ShouldDeleteAtMostOneDocument()
    {
        var filter = Builders<BsonDocument>.Filter.Eq("counter", 110);

        DeleteResult deleteResult = await _collection.DeleteOneAsync(filter);

        Assert.Equal(0, deleteResult.DeletedCount);

        filter = Builders<BsonDocument>.Filter.Eq("counter", 10);

        deleteResult = await _collection.DeleteOneAsync(filter);

        Assert.Equal(1, deleteResult.DeletedCount);
    }

    [Fact]
    public async Task ShouldDeleteManyDocuments()
    {
        var filterBuilder = Builders<BsonDocument>.Filter;
        var filter = filterBuilder.Lt("counter", 100) & filterBuilder.Gt("counter", 50);

        DeleteResult deleteResult = await _collection.DeleteManyAsync(filter);

        Assert.Equal(49, deleteResult.DeletedCount);
    }

    [Fact]
    public async Task ShouldBulkWriteOrderedDocuments()
    {
        // Ordered bulk operation - order of operation is guaranteed
        // Errors out on the first error

        var models = new WriteModel<BsonDocument>[]
        {
            new InsertOneModel<BsonDocument>(new BsonDocument("_id", 4)),
            new InsertOneModel<BsonDocument>(new BsonDocument("_id", 5)),
            new InsertOneModel<BsonDocument>(new BsonDocument("_id", 6)),
            new UpdateOneModel<BsonDocument>(
                new BsonDocument("_id", 1),
                new BsonDocument("$set", new BsonDocument("x", 2))),
            new DeleteOneModel<BsonDocument>(new BsonDocument("_id", 3)),
            new ReplaceOneModel<BsonDocument>(
                new BsonDocument("_id", 3),
                new BsonDocument("_id", 3).Add("x", 4))
        };

        BulkWriteResult bulkWriteResult = await _collection.BulkWriteAsync(models);

        Assert.Equal(3, bulkWriteResult.InsertedCount);
        Assert.True(true);
    }

    [Fact]
    public async Task ShouldBulkWriteUnorderedDocuments()
    {
        // Unordered bulk operation - no guarantee of order of operation
        // Errors out at the end

        var models = new WriteModel<BsonDocument>[]
        {
            new InsertOneModel<BsonDocument>(new BsonDocument("_id", 4)),
            new InsertOneModel<BsonDocument>(new BsonDocument("_id", 5)),
            new InsertOneModel<BsonDocument>(new BsonDocument("_id", 6)),
            new UpdateOneModel<BsonDocument>(
                new BsonDocument("_id", 1),
                new BsonDocument("$set", new BsonDocument("x", 2))),
            new DeleteOneModel<BsonDocument>(new BsonDocument("_id", 3)),
            new ReplaceOneModel<BsonDocument>(
                new BsonDocument("_id", 3),
                new BsonDocument("_id", 3).Add("x", 4))
        };

        BulkWriteResult bulkWriteResult = await _collection.BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = false });

        Assert.Equal(3, bulkWriteResult.InsertedCount);
    }
}