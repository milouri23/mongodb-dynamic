﻿using DynamicMongoTests.Constants;
using DynamicMongoTests.Entities;
using DynamicMongoTests.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DynamicMongoTests
{
    public class JoinCollectionsTest : IDisposable
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IMongoCollection<Order> _ordersCollection;
        private readonly IMongoCollection<Person> _personsCollection;

        public JoinCollectionsTest()
        {
            IMongoClient mongoClient = new MongoClient(TestConstant.MongoUri);

            _mongoDatabase = mongoClient.GetDatabase(TestConstant.MongoDatabase);

            ConventionRegistry.Register("camelCase", new ConventionPack { new CamelCaseElementNameConvention() }, t => true);
            _ordersCollection = _mongoDatabase.GetCollection<Order>("Orders");
            _personsCollection = _mongoDatabase.GetCollection<Person>("Persons");

            List<Person> persons = DocumentDataRepository.GetAllPersons();

            _personsCollection.InsertMany(persons);

            Order order = new() { PersonId = persons[0].Id, ProductName = "Product X", Quantity = 3, UnitValue = 30.99M };
            _ordersCollection.InsertOne(order);
        }

        public void Dispose()
        {
            _mongoDatabase.DropCollection("Orders");
            _mongoDatabase.DropCollection("Persons");
        }

        //private async Task InsertOrder(Person person) =>
        //    await _ordersCollection.InsertOneAsync(new Order()
        //    {
        //        PersonId = person.Id,
        //        ProductName = "Product X",
        //        Quantity = 3,
        //        UnitValue = 30.99M
        //    });

        private async Task InsertOrder(Order order) =>
            await _ordersCollection.InsertOneAsync(order);

        [Fact]
        public async Task ShouldCreateAnOrder()
        {
            Person person = await _personsCollection.Find(_ => true).FirstOrDefaultAsync();

            Order order = new()
            {
                PersonId = person.Id,
                ProductName = "Product X",
                Quantity = 3,
                UnitValue = 30.99M
            };

            Assert.Null(order.Id);
            await _ordersCollection.InsertOneAsync(order);
            Assert.NotNull(order.Id);

            Order order2 = new()
            {
                PersonId = person.Id,
                ProductName = "Product X",
                Quantity = 3,
                UnitValue = 30.99M
            };

            Assert.Null(order2.Id);
            await InsertOrder(order2);
            Assert.NotNull(order2.Id);

            List<Order> orders = await _ordersCollection.Find(_ => true).ToListAsync();
            Assert.Equal(3, orders.Count);
        }

        [Fact]
        public async Task ShouldCreateOrderWithPersonId()
        {
            Person person = await _personsCollection.Find(_ => true).FirstOrDefaultAsync();

            Order order = await _ordersCollection.Find(_ => true).SingleOrDefaultAsync();

            Assert.Equal(person.Id, order.PersonId);
        }

        [Fact]
        public async Task ShouldMakeLookupAsBsonDocumentAndOrderPersons()
        {
            Person person = await _personsCollection.Find(_ => true).FirstOrDefaultAsync();

            List<BsonDocument> docs = await _ordersCollection.Aggregate().Lookup("Persons", "personId", "_id", "asPersons").As<BsonDocument>().ToListAsync();
            List<OrderPersons> ordersPersons = await _ordersCollection.Aggregate().Lookup("Persons", "personId", "_id", "person").As<OrderPersons>().ToListAsync();

            Assert.Equal(person.Id, docs[0].GetElement(1).Value.ToString());
            Assert.Equal(person.Name, ordersPersons[0].Person[0].Name);
        }

        [Fact]
        public async Task ShouldMakeLookupAsBsonDocumentAndOrderPerson()
        {
            Order order = await _ordersCollection.Find(_ => true).SingleOrDefaultAsync();
            Person person = await _personsCollection.Find(p => p.Id == order.PersonId).SingleOrDefaultAsync();

            OrderPerson orderPerson = await _ordersCollection.Aggregate()
                .Match(o => o.Id == order.Id)
                .Lookup("Persons", "personId", "_id", "person")
                .Unwind<OrderPerson>("person")
                .FirstOrDefaultAsync();

            Assert.Equal(person.Name, orderPerson.Person.Name);

            var orderPerson2 = await _ordersCollection.Aggregate()
                .Match(o => o.Id == order.Id)
                .Lookup<Order, Person, OrderPersons>(_personsCollection, o => o.PersonId, p => p.Id, op => op.Person)
                .Unwind<OrderPersons, OrderPerson>(op => op.Person)
                .FirstOrDefaultAsync();

            string joinField = FormatJoinField(nameof(OrderPerson.Person));

            var orderPerson3 = await _ordersCollection.Aggregate()
                .Match(o => o.Id == order.Id)
                .Lookup<Order, Person, BsonDocument>(
                    _personsCollection,
                    o => o.PersonId,
                    p => p.Id,
                    x => x[joinField])
                //x => x["person"])
                //.Unwind<OrderPerson>("person")
                .Unwind<OrderPerson>(joinField)
                .FirstOrDefaultAsync();

            Assert.Equal(person.Name, orderPerson2.Person.Name);
            Assert.Equal(person.Name, orderPerson3.Person.Name);
        }

        private string FormatJoinField(string fieldName) =>
            char.IsLower(fieldName[0])
                ? fieldName
                : string.Concat(char.ToLower(fieldName[0]), fieldName[1..]);

        [Fact]
        public async Task ShouldMakeLookupWithOrderWithoutProductName()
        {
            Order order = await _ordersCollection.Find(_ => true).SingleOrDefaultAsync();
            Person person = await _personsCollection.Find(p => p.Id == order.PersonId).SingleOrDefaultAsync();

            IMongoCollection<OrderWithoutProductName> orderWithoutProductNameCollection =
                _mongoDatabase.GetCollection<OrderWithoutProductName>("Orders");

            var a = await orderWithoutProductNameCollection.Find(_ => true).ToListAsync();

            OrderPerson orderPerson = await orderWithoutProductNameCollection.Aggregate()
                .Match(o => o.Id == order.Id)
                .Lookup("Persons", "personId", "_id", "person")
                .Unwind<OrderPerson>("person")
                .FirstOrDefaultAsync();

            Assert.Equal(person.Name, orderPerson.Person.Name);

            var orderPerson2 = await orderWithoutProductNameCollection.Aggregate()
                .Match(o => o.Id == order.Id)
                .Lookup<OrderWithoutProductName, Person, OrderPersons>(_personsCollection, o => o.PersonId, p => p.Id, op => op.Person)
                .Unwind<OrderPersons, OrderPersonWithoutProductName>(op => op.Person)
                .FirstOrDefaultAsync();

            Assert.Equal(person.Name, orderPerson2.Person.Name);
        }
    }
}