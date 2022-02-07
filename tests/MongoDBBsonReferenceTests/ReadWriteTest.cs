using MongoDB.Bson.IO;
using System.IO;
using Xunit;

namespace MongoDBBsonReferenceTests
{
    public class ReadWriteTest
    {
        // mongo
        // use test
        // db.testColl.insertOne({a: NumberInt(2)})
        // mongodump --uri="mongodb://localhost:27017" -d="test" -c="testColl"
        // cd/dump/test
        // bsondump .\testColl.bson

        // Bson to Json online tool:
        // https://mcraiha.github.io/tools/BSONhexToJSON/bsonfiletojson.html

        /*
         * The IBSonReader interface contains all methods necessary to
         * read a BSON document or a JSON document. There is an implementation
         * for each format
         */

        [Fact]
        public void ShouldReadAsBsonDocument()
        {
            string inputFileName = Path.Combine(Directory.GetCurrentDirectory(),
                "Resources", "sampleFile.bson");

            Assert.True(File.Exists(inputFileName));

            using (FileStream stream = File.OpenRead(inputFileName))
            using (var reader = new BsonBinaryReader(stream))
            {
                reader.ReadStartDocument();
                string fieldName = reader.ReadName();
                int value = reader.ReadInt32();
                reader.ReadEndDocument();

                Assert.Equal("a", fieldName);
                Assert.Equal(1, value);
            }
        }

        [Fact]
        public void ShouldWriteAsBsonDocument()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string outputDirectory = Path.Combine(currentDirectory, "Output");

            Directory.CreateDirectory(outputDirectory);

            string outputFileName = Path.Combine(Directory.GetCurrentDirectory(),
                "Output", "sampleFile.bson");

            using (FileStream stream = File.OpenWrite(outputFileName))
            using (BsonBinaryWriter writer = new(stream))
            {
                writer.WriteStartDocument();
                writer.WriteName("a");
                writer.WriteInt32(1);
                writer.WriteEndDocument();
            }

            Assert.True(File.Exists(outputFileName));
        }

        [Fact]
        public void ShouldReadAJsonDocument()
        {
            string inputFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                "Resources", "sampleFile.json");

            Assert.True(File.Exists(inputFilePath));

            string jsonText = File.ReadAllText(inputFilePath);

            using (JsonReader reader = new(jsonText))
            {
                reader.ReadStartDocument();
                string fieldName = reader.ReadName();
                int value = reader.ReadInt32();
                reader.ReadEndDocument();

                Assert.Equal("a", fieldName);
                Assert.Equal(1, value);
            }

            // It doesn't matter if the field name doesn't have quotes
            // It is not strict.
            string jsonString = "{ a: 1 }";

            using (var anotherReader = new JsonReader(jsonString))
            {
                anotherReader.ReadStartDocument();
                string fieldName = anotherReader.ReadName();
                int value = anotherReader.ReadInt32();
                anotherReader.ReadEndDocument();

                Assert.Equal("a", fieldName);
                Assert.Equal(1, value);
            }
        }

        [Fact]
        public void ShouldWriteAsJsonDocument()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string outputDirectory = Path.Combine(currentDirectory, "Output");

            Directory.CreateDirectory(outputDirectory);

            string outputFileName = Path.Combine(Directory.GetCurrentDirectory(),
                "Output", "sampleFile.json");

            using (StreamWriter stream = new(outputFileName))
            using (JsonWriter writer = new(stream))
            {
                writer.WriteStartDocument();
                writer.WriteName("a");
                writer.WriteInt32(1);
                writer.WriteEndDocument();
            }

            Assert.True(File.Exists(outputFileName));
        }
    }
}