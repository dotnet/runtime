// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            string json = JsonSerializer.ToString<object>(1);
            Assert.Equal("1", json);
            json = JsonSerializer.ToString(1, typeof(object));
            Assert.Equal("1", json);

            json = JsonSerializer.ToString<object>("foo");
            Assert.Equal(@"""foo""", json);
            json = JsonSerializer.ToString("foo", typeof(object));
            Assert.Equal(@"""foo""", json);

            json = JsonSerializer.ToString<object>(true);
            Assert.Equal(@"true", json);
            json = JsonSerializer.ToString(true, typeof(object));
            Assert.Equal(@"true", json);

            json = JsonSerializer.ToString<object>(null);
            Assert.Equal(@"null", json);
            json = JsonSerializer.ToString(null, typeof(object));
            Assert.Equal(@"null", json);

            decimal pi = 3.1415926535897932384626433833m;
            json = JsonSerializer.ToString<object>(pi);
            Assert.Equal(@"3.1415926535897932384626433833", json);
            json = JsonSerializer.ToString(pi, typeof(object));
            Assert.Equal(@"3.1415926535897932384626433833", json);
        }

        [Fact]
        public static void ReadPrimitivesFail()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<object>(@""));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<object>(@"a"));
        }

        [Fact]
        public static void ParseUntyped()
        {
            object obj = JsonSerializer.Parse<object>(@"""hello""");
            Assert.IsType<JsonElement>(obj);
            JsonElement element = (JsonElement)obj;
            Assert.Equal(JsonValueType.String, element.Type);
            Assert.Equal("hello", element.GetString());

            obj = JsonSerializer.Parse<object>(@"true");
            element = (JsonElement)obj;
            Assert.Equal(JsonValueType.True, element.Type);
            Assert.Equal(true, element.GetBoolean());

            obj = JsonSerializer.Parse<object>(@"null");
            Assert.Null(obj);
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
            string json = JsonSerializer.ToString(array);
            Assert.Equal(ExpectedJson, json);

            var dictionary = new Dictionary<string, string> { { "City", "MyCity" } };
            var arrayWithDictionary = new object[] { 1, true, dictionary, null, "foo" };
            json = JsonSerializer.ToString(arrayWithDictionary);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(array);
            Assert.Equal(ExpectedJson, json);

            List<object> list = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.ToString(list);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(list);
            Assert.Equal(ExpectedJson, json);

            IEnumerable<object> ienumerable = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.ToString(ienumerable);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(ienumerable);
            Assert.Equal(ExpectedJson, json);

            IList<object> ilist = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.ToString(ilist);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(ilist);
            Assert.Equal(ExpectedJson, json);

            ICollection<object> icollection = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.ToString(icollection);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(icollection);
            Assert.Equal(ExpectedJson, json);

            IReadOnlyCollection<object> ireadonlycollection = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.ToString(ireadonlycollection);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(ireadonlycollection);
            Assert.Equal(ExpectedJson, json);

            IReadOnlyList<object> ireadonlylist = new List<object> { 1, true, address, null, "foo" };
            json = JsonSerializer.ToString(ireadonlylist);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(ireadonlylist);
            Assert.Equal(ExpectedJson, json);

            Stack<object> stack = new Stack<object>(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.ToString(stack);
            Assert.Equal(ReversedExpectedJson, json);

            json = JsonSerializer.ToString<object>(stack);
            Assert.Equal(ReversedExpectedJson, json);

            Queue<object> queue = new Queue<object>(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.ToString(queue);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(queue);
            Assert.Equal(ExpectedJson, json);

            HashSet<object> hashset = new HashSet<object>(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.ToString(hashset);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(hashset);
            Assert.Equal(ExpectedJson, json);

            LinkedList<object> linkedlist = new LinkedList<object>(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.ToString(linkedlist);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(linkedlist);
            Assert.Equal(ExpectedJson, json);

            IImmutableList<object> iimmutablelist = ImmutableList.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.ToString(iimmutablelist);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(iimmutablelist);
            Assert.Equal(ExpectedJson, json);

            IImmutableStack<object> iimmutablestack = ImmutableStack.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.ToString(iimmutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            json = JsonSerializer.ToString<object>(iimmutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            IImmutableQueue<object> iimmutablequeue = ImmutableQueue.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.ToString(iimmutablequeue);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(iimmutablequeue);
            Assert.Equal(ExpectedJson, json);

            IImmutableSet<object> iimmutableset = ImmutableHashSet.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.ToString(iimmutableset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            json = JsonSerializer.ToString<object>(iimmutableset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            ImmutableHashSet<object> immutablehashset = ImmutableHashSet.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.ToString(immutablehashset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            json = JsonSerializer.ToString<object>(immutablehashset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            ImmutableList<object> immutablelist = ImmutableList.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.ToString(immutablelist);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(immutablelist);
            Assert.Equal(ExpectedJson, json);

            ImmutableStack<object> immutablestack = ImmutableStack.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.ToString(immutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            json = JsonSerializer.ToString<object>(immutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            ImmutableQueue<object> immutablequeue = ImmutableQueue.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = JsonSerializer.ToString(immutablequeue);
            Assert.Equal(ExpectedJson, json);

            json = JsonSerializer.ToString<object>(immutablequeue);
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
            string json = JsonSerializer.ToString(obj);
            Assert.Contains(@"""MyInt16"":1", json);
            Assert.Contains(@"""MyBooleanTrue"":true", json);
            Assert.Contains(@"""MyInt16Array"":[1]", json);

            // Verify with object type.
            json = JsonSerializer.ToString<object>(obj);
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
                Assert.Contains(@"""IEnumerableT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""IListT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""ICollectionT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""IReadOnlyCollectionT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""IReadOnlyListT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""StackT"":[""World"",""Hello""]", json);
                Assert.Contains(@"""QueueT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""HashSetT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""LinkedListT"":[""Hello"",""World""]", json);
                Assert.Contains(@"""SortedSetT"":[""Hello"",""World""]", json);
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

            string json = JsonSerializer.ToString(obj);
            Verify(json);

            json = JsonSerializer.ToString<object>(obj);
            Verify(json);
        }

        [Fact]
        public static void NestedObjectAsRootObjectIgnoreNullable()
        {
            // Ensure that null properties are properly written and support ignore.
            var obj = new ObjectWithObjectProperties();
            obj.NullableInt = null;

            string json = JsonSerializer.ToString(obj);
            Assert.Contains(@"""NullableInt"":null", json);

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IgnoreNullValues = true;
            json = JsonSerializer.ToString(obj, options);
            Assert.DoesNotContain(@"""NullableInt"":null", json);
        }

        [Fact]
        public static void StaticAnalysisBaseline()
        {
            Customer customer = new Customer();
            customer.Initialize();
            customer.Verify();

            string json = JsonSerializer.ToString(customer);
            Customer deserializedCustomer = JsonSerializer.Parse<Customer>(json);
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
            string json = JsonSerializer.ToString(person);

            Customer deserializedCustomer = JsonSerializer.Parse<Customer>(json);

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

            string json = JsonSerializer.ToString(person, person.GetType());

            Customer deserializedCustomer = JsonSerializer.Parse<Customer>(json);

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
            string json = JsonSerializer.ToString(customer);

            UsaCustomer deserializedCustomer = JsonSerializer.Parse<UsaCustomer>(json);

            // We only serialized the Customer base class
            Assert.Equal(typeof(UsaCustomer), deserializedCustomer.GetType());
            Assert.Equal(typeof(Address), deserializedCustomer.Address.GetType());
            ((Customer)deserializedCustomer).VerifyNonVirtual();
        }

        [Fact]
        public static void PolymorphicInterface_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Parse<MyClass>(@"{ ""Value"": ""A value"", ""Thing"": { ""Number"": 123 } }"));
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
    }
}
