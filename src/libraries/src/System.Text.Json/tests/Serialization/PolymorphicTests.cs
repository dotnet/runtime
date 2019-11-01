// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class PolymorphicTests
    {
        [Fact]
        public static void PrimitivesAsRootObject()
        {
            string json = JsonSerializer.Serialize<object>(1);
            Assert.Equal("1", json);
            json = JsonSerializer.Serialize(1, typeof(object));
            Assert.Equal("1", json);

            json = JsonSerializer.Serialize<object>("foo");
            Assert.Equal(@"""foo""", json);
            json = JsonSerializer.Serialize("foo", typeof(object));
            Assert.Equal(@"""foo""", json);

            json = JsonSerializer.Serialize<object>(true);
            Assert.Equal(@"true", json);
            json = JsonSerializer.Serialize(true, typeof(object));
            Assert.Equal(@"true", json);

            json = JsonSerializer.Serialize<object>(null);
            Assert.Equal(@"null", json);
            json = JsonSerializer.Serialize((object)null, typeof(object));
            Assert.Equal(@"null", json);

            decimal pi = 3.1415926535897932384626433833m;
            json = JsonSerializer.Serialize<object>(pi);
            Assert.Equal(@"3.1415926535897932384626433833", json);
            json = JsonSerializer.Serialize(pi, typeof(object));
            Assert.Equal(@"3.1415926535897932384626433833", json);
        }

        [Fact]
        public static void ReadPrimitivesFail()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>(@""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>(@"a"));
        }

        [Fact]
        public static void ParseUntyped()
        {
            object obj = JsonSerializer.Deserialize<object>(@"""hello""");
            Assert.IsType<JsonElement>(obj);
            JsonElement element = (JsonElement)obj;
            Assert.Equal(JsonValueKind.String, element.ValueKind);
            Assert.Equal("hello", element.GetString());

            obj = JsonSerializer.Deserialize<object>(@"true");
            element = (JsonElement)obj;
            Assert.Equal(JsonValueKind.True, element.ValueKind);
            Assert.True(element.GetBoolean());

            obj = JsonSerializer.Deserialize<object>(@"null");
            Assert.Null(obj);

            obj = JsonSerializer.Deserialize<object>(@"[]");
            element = (JsonElement)obj;
            Assert.Equal(JsonValueKind.Array, element.ValueKind);
        }

        [Fact]
        public static void ArrayAsRootObject()
        {
            const string ExpectedJson = @"[1,true,{""City"":""MyCity""},null,""foo""]";
            const string ReversedExpectedJson = @"[""foo"",null,{""City"":""MyCity""},true,1]";

            string[] expectedObjects = { @"""foo""", @"null", @"{""City"":""MyCity""}", @"true", @"1" };

            var address = new Address();
            address.Initialize();

            var array = new object[] { 1, true, address, null, "foo" };
            string json = JsonSerializer.Serialize(array);
            Assert.Equal(ExpectedJson, json);

            var dictionary = new Dictionary<string, string> { { "City", "MyCity" } };
            var arrayWithDictionary = new object[] { 1, true, dictionary, null, "foo" };
            json = JsonSerializer.Serialize(arrayWithDictionary);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(array);
            Assert.Equal(ExpectedJson, json);

            List<object> list = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.Serialize(list);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(list);
            Assert.Equal(ExpectedJson, json);

            IEnumerable ienumerable = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.Serialize(ienumerable);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(ienumerable);
            Assert.Equal(ExpectedJson, json);

            IList ilist = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.Serialize(ilist);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(ilist);
            Assert.Equal(ExpectedJson, json);

            ICollection icollection = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.Serialize(icollection);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(icollection);
            Assert.Equal(ExpectedJson, json);

            IEnumerable<object> genericIEnumerable = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.Serialize(genericIEnumerable);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(genericIEnumerable);
            Assert.Equal(ExpectedJson, json);

            IList<object> genericIList = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.Serialize(genericIList);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(genericIList);
            Assert.Equal(ExpectedJson, json);

            ICollection<object> genericICollection = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.Serialize(genericICollection);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(genericICollection);
            Assert.Equal(ExpectedJson, json);

            IReadOnlyCollection<object> genericIReadOnlyCollection = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.Serialize(genericIReadOnlyCollection);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(genericIReadOnlyCollection);
            Assert.Equal(ExpectedJson, json);

            IReadOnlyList<object> genericIReadonlyList = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.Serialize(genericIReadonlyList);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(genericIReadonlyList);
            Assert.Equal(ExpectedJson, json);

            ISet<object> iset = new HashSet<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.Serialize(iset);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(iset);
            Assert.Equal(ExpectedJson, json);

            Stack<object> stack = new Stack<object>(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(stack);
            Assert.Equal(ReversedExpectedJson, json);

            json = JsonSerializer.Serialize<object>(stack);
            Assert.Equal(ReversedExpectedJson, json);

            Queue<object> queue = new Queue<object>(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(queue);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(queue);
            Assert.Equal(ExpectedJson, json);

            HashSet<object> hashset = new HashSet<object>(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(hashset);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(hashset);
            Assert.Equal(ExpectedJson, json);

            LinkedList<object> linkedlist = new LinkedList<object>(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(linkedlist);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(linkedlist);
            Assert.Equal(ExpectedJson, json);

            ImmutableArray<object> immutablearray = ImmutableArray.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(immutablearray);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(immutablearray);
            Assert.Equal(ExpectedJson, json);

            IImmutableList<object> iimmutablelist = ImmutableList.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(iimmutablelist);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(iimmutablelist);
            Assert.Equal(ExpectedJson, json);

            IImmutableStack<object> iimmutablestack = ImmutableStack.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(iimmutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            json = JsonSerializer.Serialize<object>(iimmutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            IImmutableQueue<object> iimmutablequeue = ImmutableQueue.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(iimmutablequeue);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(iimmutablequeue);
            Assert.Equal(ExpectedJson, json);

            IImmutableSet<object> iimmutableset = ImmutableHashSet.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(iimmutableset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            json = JsonSerializer.Serialize<object>(iimmutableset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            ImmutableHashSet<object> immutablehashset = ImmutableHashSet.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(immutablehashset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            json = JsonSerializer.Serialize<object>(immutablehashset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            ImmutableList<object> immutablelist = ImmutableList.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(immutablelist);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(immutablelist);
            Assert.Equal(ExpectedJson, json);

            ImmutableStack<object> immutablestack = ImmutableStack.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(immutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            json = JsonSerializer.Serialize<object>(immutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            ImmutableQueue<object> immutablequeue = ImmutableQueue.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.Serialize(immutablequeue);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.Serialize<object>(immutablequeue);
            Assert.Equal(ExpectedJson, json);
        }

        [Fact]
        public static void SimpleTestClassAsRootObject()
        {
            // Sanity checks on test type.
            Assert.Equal(typeof(object), typeof(SimpleTestClassWithObject).GetProperty("MyInt16").PropertyType);
            Assert.Equal(typeof(object), typeof(SimpleTestClassWithObject).GetProperty("MyBooleanTrue").PropertyType);
            Assert.Equal(typeof(object), typeof(SimpleTestClassWithObject).GetProperty("MyInt16Array").PropertyType);

            var obj = new SimpleTestClassWithObject();
            obj.Initialize();

            // Verify with actual type.
            string json = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""MyInt16"":1", json);
            Assert.Contains(@"""MyBooleanTrue"":true", json);
            Assert.Contains(@"""MyInt16Array"":[1]", json);

            // Verify with object type.
            json = JsonSerializer.Serialize<object>(obj);
            Assert.Contains(@"""MyInt16"":1", json);
            Assert.Contains(@"""MyBooleanTrue"":true", json);
            Assert.Contains(@"""MyInt16Array"":[1]", json);
        }

        [Fact]
        public static void NestedObjectAsRootObject()
        {
            static void Verify(string json)
            {
                Assert.Contains(@"""Address"":{""City"":""MyCity""}", json);
                Assert.Contains(@"""List"":[""Hello"",""World""]", json);
                Assert.Contains(@"""Array"":[""Hello"",""Again""]", json);
                Assert.Contains(@"""IEnumerable"":[""Hello"",""World""]", json);
                Assert.Contains(@"""IList"":[""Hello"",""World""]", json);
                Assert.Contains(@"""ICollection"":[""Hello"",""World""]", json);
                Assert.Contains(@"""IEnumerableT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""IListT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""ICollectionT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""IReadOnlyCollectionT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""IReadOnlyListT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""ISetT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""StackT"":[""World"",""Hello""]", json);
                Assert.Contains(@"""QueueT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""HashSetT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""LinkedListT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""SortedSetT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""ImmutableArrayT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""IImmutableListT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""IImmutableStackT"":[""World"",""Hello""]", json);
                Assert.Contains(@"""IImmutableQueueT"":[""Hello"",""World""]", json);
                Assert.True(json.Contains(@"""IImmutableSetT"":[""Hello"",""World""]") || json.Contains(@"""IImmutableSetT"":[""World"",""Hello""]"));
                Assert.True(json.Contains(@"""ImmutableHashSetT"":[""Hello"",""World""]") || json.Contains(@"""ImmutableHashSetT"":[""World"",""Hello""]"));
                Assert.Contains(@"""ImmutableListT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""ImmutableStackT"":[""World"",""Hello""]", json);
                Assert.Contains(@"""ImmutableQueueT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""ImmutableSortedSetT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""NullableInt"":42", json);
                Assert.Contains(@"""Object"":{}", json);
                Assert.Contains(@"""NullableIntArray"":[null,42,null]", json);
            }

            // Sanity checks on test type.
            Assert.Equal(typeof(object), typeof(ObjectWithObjectProperties).GetProperty("Address").PropertyType);
            Assert.Equal(typeof(object), typeof(ObjectWithObjectProperties).GetProperty("List").PropertyType);
            Assert.Equal(typeof(object), typeof(ObjectWithObjectProperties).GetProperty("Array").PropertyType);
            Assert.Equal(typeof(object), typeof(ObjectWithObjectProperties).GetProperty("NullableInt").PropertyType);
            Assert.Equal(typeof(object), typeof(ObjectWithObjectProperties).GetProperty("NullableIntArray").PropertyType);

            var obj = new ObjectWithObjectProperties();

            string json = JsonSerializer.Serialize(obj);
            Verify(json);

            json = JsonSerializer.Serialize<object>(obj);
            Verify(json);
        }

        [Fact]
        public static void NestedObjectAsRootObjectIgnoreNullable()
        {
            // Ensure that null properties are properly written and support ignore.
            var obj = new ObjectWithObjectProperties();
            obj.NullableInt = null;

            string json = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""NullableInt"":null", json);

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IgnoreNullValues = true;
            json = JsonSerializer.Serialize(obj, options);
            Assert.DoesNotContain(@"""NullableInt"":null", json);
        }

        [Fact]
        public static void StaticAnalysisBaseline()
        {
            Customer customer = new Customer();
            customer.Initialize();
            customer.Verify();

            string json = JsonSerializer.Serialize(customer);
            Customer deserializedCustomer = JsonSerializer.Deserialize<Customer>(json);
            deserializedCustomer.Verify();
        }

        [Fact]
        public static void StaticAnalysis()
        {
            Customer customer = new Customer();
            customer.Initialize();
            customer.Verify();

            Person person = customer;

            // Generic inference used <TValue> = <Person>
            string json = JsonSerializer.Serialize(person);

            Customer deserializedCustomer = JsonSerializer.Deserialize<Customer>(json);

            // We only serialized the Person base class, so the Customer fields should be default.
            Assert.Equal(typeof(Customer), deserializedCustomer.GetType());
            Assert.Equal(0, deserializedCustomer.CreditLimit);
            ((Person)deserializedCustomer).VerifyNonVirtual();
        }

        [Fact]
        public static void WriteStringWithRuntimeType()
        {
            Customer customer = new Customer();
            customer.Initialize();
            customer.Verify();

            Person person = customer;

            string json = JsonSerializer.Serialize(person, person.GetType());

            Customer deserializedCustomer = JsonSerializer.Deserialize<Customer>(json);

            // We serialized the Customer
            Assert.Equal(typeof(Customer), deserializedCustomer.GetType());
            deserializedCustomer.Verify();
        }

        [Fact]
        public static void StaticAnalysisWithRelationship()
        {
            UsaCustomer usaCustomer = new UsaCustomer();
            usaCustomer.Initialize();
            usaCustomer.Verify();

            // Note: this could be typeof(UsaAddress) if we preserve objects created in the ctor. Currently we only preserve IEnumerables.
            Assert.Equal(typeof(Address), usaCustomer.Address.GetType());

            Customer customer = usaCustomer;

            // Generic inference used <TValue> = <Customer>
            string json = JsonSerializer.Serialize(customer);

            UsaCustomer deserializedCustomer = JsonSerializer.Deserialize<UsaCustomer>(json);

            // We only serialized the Customer base class
            Assert.Equal(typeof(UsaCustomer), deserializedCustomer.GetType());
            Assert.Equal(typeof(Address), deserializedCustomer.Address.GetType());
            ((Customer)deserializedCustomer).VerifyNonVirtual();
        }

        [Fact]
        public static void PolymorphicInterface_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<MyClass>(@"{ ""Value"": ""A value"", ""Thing"": { ""Number"": 123 } }"));
        }

        [Fact]
        public static void GenericListOfInterface_WithInvalidJson_ThrowsJsonException()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MyThingCollection>("false"));
        }

        [Fact]
        public static void GenericListOfInterface_WithValidJson_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<MyThingCollection>("{}"));
        }

        class MyClass
        {
            public string Value { get; set; }
            public IThing Thing { get; set; }
        }

        interface IThing
        {
            int Number { get; set; }
        }

        class MyThing : IThing
        {
            public int Number { get; set; }
        }

        class MyThingCollection : List<IThing> { }
    }
}
