using DynamicMongoTests.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DynamicMongoTests;

public class DynamicReadingTest : IDisposable
{
    private readonly IMongoDatabase _mongoDatabase;
    private readonly IMongoCollection<BsonDocument> _mongoCollection;
    private readonly IEnumerable<BsonDocument> _allDocuments;

    public DynamicReadingTest()
    {
        IMongoClient mongoClient = new MongoClient(
            "mongodb://localhost:27017/?readPreference=primary&appname=MongoDB%20Compass&directConnection=true&ssl=false");

        _mongoDatabase = mongoClient.GetDatabase("DynamicReadWriteDB");

        _mongoCollection = _mongoDatabase.GetCollection<BsonDocument>("NonUniformDocuments");

        _allDocuments = DocumentDataRepository.GetAllDynamicDocuments();
        _mongoCollection.InsertMany(_allDocuments);
    }

    public void Dispose() =>
        _mongoDatabase.DropCollection("NonUniformDocuments");

    [Fact]
    public async Task ShouldGetAllDocuments()
    {
        int numberOfDocumentsExpected = _allDocuments.Count();

        List<BsonDocument> documents = await _mongoCollection.Find(_ => true).ToListAsync();
        Assert.Equal(numberOfDocumentsExpected, documents.Count);
    }

    [Fact]
    public async Task ShouldGetOneDocument()
    {
        BsonDocument document = await _mongoCollection.Find(_ => true).FirstOrDefaultAsync();

        Assert.NotNull(document);
    }

    [Fact]
    public async Task ShouldGetDocumentByDictionaryFilter()
    {
        List<KeyValuePair<string, object>> documentKeyValuePairsRepresentation =
            new()
            {
                new("lastName", "Doe"),
                new("name", "John")
            };

        IDictionary<string, object> documentDictionaryRepresentation =
            new Dictionary<string, object>(documentKeyValuePairsRepresentation);

        BsonDocument filter = new(documentDictionaryRepresentation);
        List<BsonDocument> documents = await _mongoCollection.Find(filter).ToListAsync();

        BsonDocument filterEquivalent = new(documentKeyValuePairsRepresentation);
        List<BsonDocument> documentsEquivalent = await _mongoCollection.Find(filterEquivalent).ToListAsync();

        Assert.Single(documents);
        Assert.Single(documentsEquivalent);
    }

    [Fact]
    public async Task ShouldNotGetDocumentByDictionaryFilter()
    {
        var documentRepresentation =
            new List<KeyValuePair<string, object>>
            {
                new("lastName", "Doe"),
                new("name", "Jane")
            };

        BsonDocument filter = new(documentRepresentation);

        List<BsonDocument> documents = await _mongoCollection.Find(filter).ToListAsync();

        Assert.Empty(documents);
    }

    [Fact]
    public async Task ShouldConvertBsonToDictionary()
    {
        var keyValuePairs =
            new List<KeyValuePair<string, object>>
            {
                new("isTrue", true),
                new("dollars", 420.69m)
            };

        IDictionary<string, object> documentRepresentation =
            new Dictionary<string, object>(keyValuePairs);

        BsonDocument filter = new(documentRepresentation);

        List<BsonDocument> documents = await _mongoCollection.Find(filter).ToListAsync();

        IEnumerable<Dictionary<string, object>> dictionariesFromDocuments =
            documents.Select(document => document.ToDictionary());

        Assert.Single(documents);
        Assert.Single(dictionariesFromDocuments);

        var dictionaryFromDocument = dictionariesFromDocuments.Single();

        // Boolean types remain equals
        Assert.Equal(dictionaryFromDocument["isTrue"], documentRepresentation["isTrue"]);
        Assert.Equal(dictionaryFromDocument["isTrue"].GetType(), documentRepresentation["isTrue"].GetType());

        // Decimal types are not equals
        Assert.NotEqual(dictionaryFromDocument["dollars"].GetType(), documentRepresentation["dollars"].GetType());

        Assert.IsType<System.Decimal>(documentRepresentation["dollars"]);
        Assert.IsType<MongoDB.Bson.Decimal128>(dictionaryFromDocument["dollars"]);

        object decimal128Fix = FixDecimal128(dictionaryFromDocument["dollars"]);

        Assert.Equal(decimal128Fix, documentRepresentation["dollars"]);
        Assert.NotEqual(dictionaryFromDocument["dollars"].GetType(), documentRepresentation["dollars"].GetType());
        //(decimal)dictionaryFromDocument["dollars"] Throws exceptions
        // Convert.ToDecimal if I am really sure is one option
        Assert.Equal(Convert.ToDecimal(dictionaryFromDocument["dollars"]), documentRepresentation["dollars"]);
        Assert.NotEqual(dictionaryFromDocument["dollars"].GetType(), documentRepresentation["dollars"].GetType());
    }

    private static object FixDecimal128(object value)
    {
        if (value == null) return null;

        //Replace '.' by ',' is not necessary in United States
        if (value.GetType().FullName == "MongoDB.Bson.Decimal128")
            return decimal.TryParse(value.ToString()?.Replace('.', ','), out var dec) ? dec : default;

        return value;
    }

    [Fact]
    public async Task ShouldWorkWithDatesStoredInUTC()
    {
        IDictionary<string, object> dictionary = new Dictionary<string, object>(1);

        dictionary.Add(new("BirthDate", new DateTime(2000, 01, 01)));

        BsonDocument filter = new(dictionary);

        BsonDocument document = await _mongoCollection.Find(filter).FirstOrDefaultAsync();
        IDictionary<string, object> dictionaryFromDocument = document.ToDictionary();

        Assert.Equal(dictionaryFromDocument["BirthDate"].GetType(), dictionary["BirthDate"].GetType());

        // Not equal because Dates are stored in UTC
        Assert.NotEqual(dictionaryFromDocument["BirthDate"], dictionary["BirthDate"]);

        // Equal explictly specifying UTC
        Assert.Equal(
            DateTime.SpecifyKind(new DateTime(2000, 01, 01), DateTimeKind.Utc), dictionary["BirthDate"]);
    }
}