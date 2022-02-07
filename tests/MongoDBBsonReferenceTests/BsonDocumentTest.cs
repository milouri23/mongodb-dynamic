using MongoDB.Bson;
using System.Linq;
using Xunit;

namespace MongoDBBsonReferenceTests
{
    public class BsonDocumentTest
    {
        [Fact]
        public void ShouldCreateSimpleBsonDocument()
        {
            var doc = new BsonDocument
            {
                {"a", 1},
                {"b", new BsonArray
                {
                    new BsonDocument("c", 1)
                }}
            };

            var docEquivalent = BsonDocument.Parse("{ a: 1, b: [{c: 1}] }");

            // Equals works because Equals method is overwritten
            Assert.Equal(docEquivalent, doc);

            // In particular, the override works something like this
            Assert.True(docEquivalent.Elements.SequenceEqual(doc.Elements));

            // Which in the particular case of Xunit, can be Assert like this:
            Assert.Equal(docEquivalent.Elements, doc.Elements);

            // Elements are structs
            Assert.IsType<BsonElement>(docEquivalent.Elements.First());
            Assert.True(typeof(BsonElement).IsValueType);
        }
    }
}