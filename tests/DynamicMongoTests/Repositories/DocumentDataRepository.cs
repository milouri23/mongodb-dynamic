using DynamicMongoTests.Enums;
using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace DynamicMongoTests.Repositories;

public static class DocumentDataRepository
{
    internal static IEnumerable<BsonDocument> GetAllDynamicDocuments() =>
        new BsonDocument[]
        {
                new BsonDocument(new List<BsonElement> {
                        new ("name", "John"),
                        new ("lastName", "Doe"),
                        new ("age", 42)}),
                new BsonDocument(new List<BsonElement> {
                        new ("email", "user@xmail.com"),
                        new ("phrase", "The quick brown fox jumps over the lazy dog"),
                        new ("var", "foo")}),
                new BsonDocument(new List<BsonElement> {
                        new ("isTrue", true),
                        new ("BirthDate", new DateTime(2000, 01, 01)),
                        new ("dollars", 420.69M)})
        };

    internal static IEnumerable<BsonDocument> GetAllDynamicWithStaticPartDocuments() =>
        new BsonDocument[]
        {
                new BsonDocument(new List<BsonElement> {
                        new ("clientType", ClientType.Platinum),
                        new ("totalPurchases", 50),
                        new ("additionalInfo", new BsonDocument(new List<BsonElement>
                        {
                            new ("name", "John"),
                            new ("lastName", "Doe"),
                            new ("age", 42)
                        }))
                }),
                new BsonDocument(new List<BsonElement> {
                        new ("clientType", ClientType.Engaged),
                        new ("totalPurchases", 15),
                        new ("additionalInfo", new BsonDocument(new List<BsonElement>
                        {
                            new ("email", "user@xmail.com"),
                            new ("phrase", "The quick brown fox jumps over the lazy dog"),
                            new ("var", "foo")
                        }))
                }),
                new BsonDocument(new List<BsonElement> {
                        new ("clientType", ClientType.Basic),
                        new ("totalPurchases", 2),
                        new ("additionalInfo", new BsonDocument(new List<BsonElement>
                        {
                            new ("isTrue", true),
                            new ("BirthDate", new DateTime(2000, 01, 01)),
                            new ("dollars", 420.69M)
                        }))
                })
        };
}