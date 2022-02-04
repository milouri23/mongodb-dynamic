using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace DynamicMongoTests.Entities
{
    public class OrderPersons
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string PersonId { get; set; }

        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitValue { get; set; }

        public List<Person> Person { get; set; }
    }
}