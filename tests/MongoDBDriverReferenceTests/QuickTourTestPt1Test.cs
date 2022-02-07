using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using Xunit;

namespace MongoDBDriverReferenceTests
{
    public class QuickTourTestPt1Test
    {
        // Explores MongoClient
        [Fact]
        public void ShouldMakeAConnection()
        {
            // Typically you only create one MongoClient instance for a given cluster and use it across your application.
            // Creating multiple MongoClients will, however, still share the same pool of connections if and only if the
            // connection strings are identical.

            // A Mongo Client instance actually represents a pool of connections to the database
            // You will only need one instance of class MongoClient even with multiple threads

            // Three ways to connect to a server
            IMongoClient client = new MongoClient();
            //IMongoClient clientWithConnectionString = new MongoClient("mongodb://localhost:27017");
            //IMongoClient clientWithReplicaSet = new MongoClient("mongodb://localhost:27017,localhost:27018,localhost:27019");

            bool isTheInstanceARepresentationOfPoolOfConnectionsToTheDatabase = true;
            bool needsAGivenClusterOnlyAMongoClient = true;

            Assert.True(isTheInstanceARepresentationOfPoolOfConnectionsToTheDatabase);
            Assert.True(needsAGivenClusterOnlyAMongoClient);

            Assert.IsType<MongoDB.Driver.MongoClient>(client);
            Assert.IsAssignableFrom<MongoDB.Driver.IMongoClient>(client);
            Assert.IsAssignableFrom<MongoDB.Driver.MongoClientBase>(client);
            Assert.IsAssignableFrom<MongoDB.Driver.MongoClient>(client);
        }

        // Explores GetDatabase
        [Fact]
        public void ShouldThrowExceptionIfMalFormedConnectionString()
        {
            string malFormedConnectionString = "wrongDirection";
            var ex2 = Assert.Throws<MongoDB.Driver.MongoConfigurationException>(() => new MongoClient(malFormedConnectionString));
            var ex3 = Assert.ThrowsAny<MongoDB.Driver.MongoClientException>(() => new MongoClient(malFormedConnectionString));
            var ex4 = Assert.ThrowsAny<MongoDB.Driver.MongoException>(() => new MongoClient(malFormedConnectionString));
            var ex = Assert.ThrowsAny<Exception>(() => new MongoClient(malFormedConnectionString));

            Assert.Equal(ex.Message, ex2.Message);
            Assert.Equal($"The connection string '{malFormedConnectionString}' is not valid.", ex2.Message);

            IMongoClient localClient = new MongoClient(); // Defaults to localhost:27017
            IMongoDatabase database = localClient.GetDatabase("foo"); // It doesn't matter if database doesn't exist

            string wellFormedConnectionStringWithBadPort = "mongodb://localhost:27001";

            // The server doesn't exist
            // mongoClient and GetDatabase operations doesn't fail
            IMongoClient mongoClient = new MongoClient(wellFormedConnectionStringWithBadPort);
            IMongoDatabase database2 = mongoClient.GetDatabase("foo");

            //Assert.IsType<MongoDB.Driver.MongoDatabaseImpl>(database2);
            Assert.IsAssignableFrom<MongoDB.Driver.IMongoDatabase>(database2);
            Assert.IsAssignableFrom<MongoDB.Driver.MongoDatabaseBase>(database2);
        }

        [Fact]
        public void ShouldGetACollection()
        {
            // The server doesn't exist
            // mongoClient and GetDatabase operations doesn't fail
            string connectionStringWithBadPort = "mongodb://localhost:27001";
            IMongoClient mongoClient = new MongoClient(connectionStringWithBadPort);
            // A database cannot contain spaces. It doesn't fail either
            IMongoDatabase database = mongoClient.GetDatabase("foo spaces");

            // A collection can contain spaces. GetCollection doesn't fail either.
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("var spaces");

            //Assert.IsType<MongoDB.Driver.MongoCollectionImpl<BsonDocument>>(collection);
            Assert.IsAssignableFrom<MongoDB.Driver.IMongoCollection<MongoDB.Bson.BsonDocument>>(collection);
            Assert.IsAssignableFrom<MongoDB.Driver.MongoCollectionBase<MongoDB.Bson.BsonDocument>>(collection);
        }

        [Fact]
        public void ShouldDescribeADocument()
        {
            /*  {
                    "name": "MongoDB",
                    "type": "database",
                    "count": 1,
                    "info": {
                        x: 203,
                        y: 102
                }
            }*/

            // A bson document could be written in a dictionary fashion
            var document = new BsonDocument
            {
                { "name", "MongoDB" },
                { "type", "Database" },
                { "count", 1},
                { "info", new BsonDocument
                    {
                        {"x", 203 },
                        {"y", 102 }
                    }
                }
            };

            // The former is equivalent to the next long way
            var documentEquivalent = new BsonDocument();
            documentEquivalent.Add("name", "MongoDB");
            documentEquivalent.Add("type", "Database");
            documentEquivalent.Add("count", 1);

            var internalSubdocument = new BsonDocument();
            internalSubdocument.Add("x", 203);
            internalSubdocument.Add("y", 102);

            documentEquivalent.Add("info", internalSubdocument);

            // The dictionary creation would be similar
            IDictionary<string, object> dictionary = new Dictionary<string, object>
            {
                { "name", "MongoDB" },
                { "type", "Database" },
                { "count", 1},
                { "info", new Dictionary<string, object>
                    {
                        {"x", 203 },
                        {"y", 102 }
                    }
                }
            };

            // Dictionary<string, object>. key: string, value: object
            // BsonDocument. key: string, value: BsonValue

            Assert.IsType<MongoDB.Bson.BsonDocument>(document.GetValue("info"));
            Assert.IsAssignableFrom<MongoDB.Bson.BsonValue>(document.GetValue("info"));
        }

        [Fact]
        public void ShouldInsertADocument()
        {
            // The server doesn't exist
            // mongoClient and GetDatabase operations doesn't fail
            string connectionStringWithBadPort = "mongodb://localhost:27001";
            IMongoClient mongoClient = new MongoClient(connectionStringWithBadPort);
            // A database cannot contain spaces. It doesn't fail either
            IMongoDatabase database = mongoClient.GetDatabase("foo spaces");

            // A collection can contain spaces. GetCollection doesn't fail either.
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("var spaces");

            var document = new BsonDocument
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

            // Commented because it is slow
            //var ex = Assert.Throws<System.TimeoutException>(() => collection.InsertOne(document));

            //{"Command insert failed: Invalid namespace specified 'foo spaces.var spaces'."}

            string databaseName = "foo spaces";
            string collectionName = "var spaces";

            string correctConnectionString = "mongodb://localhost:27017";
            mongoClient = new MongoClient(correctConnectionString);
            database = mongoClient.GetDatabase(databaseName);
            collection = database.GetCollection<BsonDocument>(collectionName);

            // Before trying an insertion, document doesn't have an id
            var _idException = Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => document.GetValue("_id"));
            Assert.Equal("Element '_id' not found.", _idException.Message);

            Assert.IsAssignableFrom<SystemException>(_idException);
            Assert.IsAssignableFrom<System.Exception>(_idException);

            var ex = Assert.Throws<MongoDB.Driver.MongoCommandException>(() => collection.InsertOne(document));
            //var ex2 = Assert.ThrowsAny<MongoDB.Driver.MongoServerException>(() => collection.InsertOne(document));
            //var ex3 = Assert.ThrowsAny<MongoDB.Driver.MongoException>(() => collection.InsertOne(document));
            Assert.IsAssignableFrom<MongoDB.Driver.MongoServerException>(ex);
            Assert.IsAssignableFrom<MongoDB.Driver.MongoException>(ex);
            Assert.IsAssignableFrom<System.Exception>(ex);

            string @namespace = string.Join('.', databaseName, collectionName);
            Assert.Equal(
                $"Command insert failed: Invalid namespace specified '{@namespace}'.", ex.Message);

            // After trying, an Id is assigned
            var id = document.GetValue("_id");
            Assert.Equal(document.GetValue("_id"), document.GetValue(0));
            Assert.Equal(24, id.ToString()!.Length);

            Assert.IsType<MongoDB.Bson.BsonObjectId>(id);
            MongoDB.Bson.BsonObjectId _id = (MongoDB.Bson.BsonObjectId)id;
            Assert.IsType<MongoDB.Bson.ObjectId>(_id.Value);

            Assert.IsAssignableFrom<BsonValue>(id);

            // Let's fix the databaseName
            databaseName = "fooNoSpaces";

            database = mongoClient.GetDatabase(databaseName);
            collection = database.GetCollection<BsonDocument>(collectionName); // Collections could have spaces

            //ObjectId idBefore = (ObjectId)document.GetValue(0);
            collection.InsertOne(document);
            //ObjectId idAfter = (ObjectId)document.GetValue(0); // Is the same as before

            Assert.Equal(collection.Find(_ => true).SingleOrDefault().GetValue(0), id);
            //Assert.NotEqual(idBefore, idAfter);

            // Cleanup actions. Drops the collection with all it's documents
            database.DropCollection(collectionName);
        }
    }
}