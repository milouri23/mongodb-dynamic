using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DynamicMongoTests.Entities
{
    [BsonIgnoreExtraElements]
    public class OrderWithoutProductName
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string PersonId { get; set; }

        //public string ProductName { get; set; }
        public int Quantity { get; set; }

        public decimal UnitValue { get; set; }
    }
}