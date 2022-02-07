using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace DynamicMongoTests.Entities
{
    public class Order
    {
        // At least one of the two must be present for Id

        //[BsonId] // Not necessary to be present
        [BsonRepresentation(BsonType.ObjectId)] // Must be present
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string PersonId { get; set; }

        public string ProductName { get; set; }
        public int Quantity { get; set; }

        public decimal UnitValue { get; set; }

        [BsonIgnore] // writing this attribute is optional
        public string SomeOtherProperty { get; set; }

        [BsonIgnoreIfDefault] // Not serialize if is null
        public string? APropertyThatCanBeNull { get; set; }

        [BsonIgnoreIfDefault]
        [BsonDefaultValue("abc")] // Will Serialize a null value but not abc
        public string SomeProperty { get; set; }

        [BsonDateTimeOptions(DateOnly = true)]
        public DateTime DateOfBirth { get; set; }

        // DateTime values in MongoDB are always saved as UTC.
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime AppointmentTime { get; set; }

        public bool ShouldSerializeDateOfBirth()
        {
            return DateOfBirth > new DateTime(1900, 1, 1);
        }
    }
}