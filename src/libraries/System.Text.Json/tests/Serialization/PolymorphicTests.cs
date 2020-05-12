// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class PolymorphicTests_Span : PolymorphicTests
    {
        public PolymorphicTests_Span() : base(SerializationWrapper.SpanSerializer) { }
    }

    public class PolymorphicTests_String : PolymorphicTests
    {
        public PolymorphicTests_String() : base(SerializationWrapper.StringSerializer) { }
    }

    public class PolymorphicTests_Stream : PolymorphicTests
    {
        public PolymorphicTests_Stream() : base(SerializationWrapper.StreamSerializer) { }
    }

    public class PolymorphicTests_StreamWithSmallBuffer : PolymorphicTests
    {
        public PolymorphicTests_StreamWithSmallBuffer() : base(SerializationWrapper.StreamSerializerWithSmallBuffer) { }
    }

    public class PolymorphicTests_Writer : PolymorphicTests
    {
        public PolymorphicTests_Writer() : base(SerializationWrapper.WriterSerializer) { }
    }

    public abstract class PolymorphicTests
    {
        private SerializationWrapper Serializer { get; }

        public PolymorphicTests(SerializationWrapper serializer)
        {
            Serializer = serializer;
        }

        [Fact]
        public void PrimitivesAsRootObject()
        {
            string json = Serializer.Serialize<object>(1);
            Assert.Equal("1", json);
            json = Serializer.Serialize(1, typeof(object));
            Assert.Equal("1", json);

            json = Serializer.Serialize<object>("foo");
            Assert.Equal(@"""foo""", json);
            json = Serializer.Serialize("foo", typeof(object));
            Assert.Equal(@"""foo""", json);

            json = Serializer.Serialize<object>(true);
            Assert.Equal(@"true", json);
            json = Serializer.Serialize(true, typeof(object));
            Assert.Equal(@"true", json);

            json = Serializer.Serialize<object>(null);
            Assert.Equal(@"null", json);
            json = Serializer.Serialize((object)null, typeof(object));
            Assert.Equal(@"null", json);

            decimal pi = 3.1415926535897932384626433833m;
            json = Serializer.Serialize<object>(pi);
            Assert.Equal(@"3.1415926535897932384626433833", json);
            json = Serializer.Serialize(pi, typeof(object));
            Assert.Equal(@"3.1415926535897932384626433833", json);
        }

        [Fact]
        public void ReadPrimitivesFail()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>(@""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>(@"a"));
        }

        [Fact]
        public void ParseUntyped()
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
        public void ArrayAsRootObject()
        {
            const string ExpectedJson = @"[1,true,{""City"":""MyCity""},null,""foo""]";
            const string ReversedExpectedJson = @"[""foo"",null,{""City"":""MyCity""},true,1]";

            string[] expectedObjects = { @"""foo""", @"null", @"{""City"":""MyCity""}", @"true", @"1" };

            var address = new Address();
            address.Initialize();

            var array = new object[] { 1, true, address, null, "foo" };
            string json = Serializer.Serialize(array);
            Assert.Equal(ExpectedJson, json);

            var dictionary = new Dictionary<string, string> { { "City", "MyCity" } };
            var arrayWithDictionary = new object[] { 1, true, dictionary, null, "foo" };
            json = Serializer.Serialize(arrayWithDictionary);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(array);
            Assert.Equal(ExpectedJson, json);

            List<object> list = new List<object> { 1, true, address, null, "foo" };
            json = Serializer.Serialize(list);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(list);
            Assert.Equal(ExpectedJson, json);

            IEnumerable ienumerable = new List<object> { 1, true, address, null, "foo" };
            json = Serializer.Serialize(ienumerable);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(ienumerable);
            Assert.Equal(ExpectedJson, json);

            IList ilist = new List<object> { 1, true, address, null, "foo" };
            json = Serializer.Serialize(ilist);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(ilist);
            Assert.Equal(ExpectedJson, json);

            ICollection icollection = new List<object> { 1, true, address, null, "foo" };
            json = Serializer.Serialize(icollection);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(icollection);
            Assert.Equal(ExpectedJson, json);

            IEnumerable<object> genericIEnumerable = new List<object> { 1, true, address, null, "foo" };
            json = Serializer.Serialize(genericIEnumerable);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(genericIEnumerable);
            Assert.Equal(ExpectedJson, json);

            IList<object> genericIList = new List<object> { 1, true, address, null, "foo" };
            json = Serializer.Serialize(genericIList);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(genericIList);
            Assert.Equal(ExpectedJson, json);

            ICollection<object> genericICollection = new List<object> { 1, true, address, null, "foo" };
            json = Serializer.Serialize(genericICollection);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(genericICollection);
            Assert.Equal(ExpectedJson, json);

            IReadOnlyCollection<object> genericIReadOnlyCollection = new List<object> { 1, true, address, null, "foo" };
            json = Serializer.Serialize(genericIReadOnlyCollection);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(genericIReadOnlyCollection);
            Assert.Equal(ExpectedJson, json);

            IReadOnlyList<object> genericIReadonlyList = new List<object> { 1, true, address, null, "foo" };
            json = Serializer.Serialize(genericIReadonlyList);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(genericIReadonlyList);
            Assert.Equal(ExpectedJson, json);

            ISet<object> iset = new HashSet<object> { 1, true, address, null, "foo" };
            json = Serializer.Serialize(iset);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(iset);
            Assert.Equal(ExpectedJson, json);

            Stack<object> stack = new Stack<object>(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(stack);
            Assert.Equal(ReversedExpectedJson, json);

            json = Serializer.Serialize<object>(stack);
            Assert.Equal(ReversedExpectedJson, json);

            Queue<object> queue = new Queue<object>(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(queue);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(queue);
            Assert.Equal(ExpectedJson, json);

            HashSet<object> hashset = new HashSet<object>(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(hashset);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(hashset);
            Assert.Equal(ExpectedJson, json);

            LinkedList<object> linkedlist = new LinkedList<object>(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(linkedlist);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(linkedlist);
            Assert.Equal(ExpectedJson, json);

            ImmutableArray<object> immutablearray = ImmutableArray.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(immutablearray);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(immutablearray);
            Assert.Equal(ExpectedJson, json);

            IImmutableList<object> iimmutablelist = ImmutableList.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(iimmutablelist);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(iimmutablelist);
            Assert.Equal(ExpectedJson, json);

            IImmutableStack<object> iimmutablestack = ImmutableStack.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(iimmutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            json = Serializer.Serialize<object>(iimmutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            IImmutableQueue<object> iimmutablequeue = ImmutableQueue.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(iimmutablequeue);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(iimmutablequeue);
            Assert.Equal(ExpectedJson, json);

            IImmutableSet<object> iimmutableset = ImmutableHashSet.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(iimmutableset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            json = Serializer.Serialize<object>(iimmutableset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            ImmutableHashSet<object> immutablehashset = ImmutableHashSet.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(immutablehashset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            json = Serializer.Serialize<object>(immutablehashset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            ImmutableList<object> immutablelist = ImmutableList.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(immutablelist);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(immutablelist);
            Assert.Equal(ExpectedJson, json);

            ImmutableStack<object> immutablestack = ImmutableStack.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(immutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            json = Serializer.Serialize<object>(immutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            ImmutableQueue<object> immutablequeue = ImmutableQueue.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = Serializer.Serialize(immutablequeue);
            Assert.Equal(ExpectedJson, json);

            json = Serializer.Serialize<object>(immutablequeue);
            Assert.Equal(ExpectedJson, json);
        }

        [Fact]
        public void SimpleTestClassAsRootObject()
        {
            // Sanity checks on test type.
            Assert.Equal(typeof(object), typeof(SimpleTestClassWithObject).GetProperty("MyInt16").PropertyType);
            Assert.Equal(typeof(object), typeof(SimpleTestClassWithObject).GetProperty("MyBooleanTrue").PropertyType);
            Assert.Equal(typeof(object), typeof(SimpleTestClassWithObject).GetProperty("MyInt16Array").PropertyType);

            var obj = new SimpleTestClassWithObject();
            obj.Initialize();

            // Verify with actual type.
            string json = Serializer.Serialize(obj);
            Assert.Contains(@"""MyInt16"":1", json);
            Assert.Contains(@"""MyBooleanTrue"":true", json);
            Assert.Contains(@"""MyInt16Array"":[1]", json);

            // Verify with object type.
            json = Serializer.Serialize<object>(obj);
            Assert.Contains(@"""MyInt16"":1", json);
            Assert.Contains(@"""MyBooleanTrue"":true", json);
            Assert.Contains(@"""MyInt16Array"":[1]", json);
        }

        [Fact]
        public void NestedObjectAsRootObject()
        {
            void Verify(string json)
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

            string json = Serializer.Serialize(obj);
            Verify(json);

            json = Serializer.Serialize<object>(obj);
            Verify(json);
        }

        [Fact]
        public void NestedObjectAsRootObjectIgnoreNullable()
        {
            // Ensure that null properties are properly written and support ignore.
            var obj = new ObjectWithObjectProperties();
            obj.NullableInt = null;

            string json = Serializer.Serialize(obj);
            Assert.Contains(@"""NullableInt"":null", json);

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IgnoreNullValues = true;
            json = Serializer.Serialize(obj, options);
            Assert.DoesNotContain(@"""NullableInt"":null", json);
        }

        [Fact]
        public void StaticAnalysisBaseline()
        {
            Customer customer = new Customer();
            customer.Initialize();
            customer.Verify();

            string json = Serializer.Serialize(customer);
            Customer deserializedCustomer = JsonSerializer.Deserialize<Customer>(json);
            deserializedCustomer.Verify();
        }

        [Fact]
        public void StaticAnalysis()
        {
            Customer customer = new Customer();
            customer.Initialize();
            customer.Verify();

            Person person = customer;

            // Generic inference used <TValue> = <Person>
            string json = Serializer.Serialize(person);

            Customer deserializedCustomer = JsonSerializer.Deserialize<Customer>(json);

            // We only serialized the Person base class, so the Customer fields should be default.
            Assert.Equal(typeof(Customer), deserializedCustomer.GetType());
            Assert.Equal(0, deserializedCustomer.CreditLimit);
            ((Person)deserializedCustomer).VerifyNonVirtual();
        }

        [Fact]
        public void WriteStringWithRuntimeType()
        {
            Customer customer = new Customer();
            customer.Initialize();
            customer.Verify();

            Person person = customer;

            string json = Serializer.Serialize(person, person.GetType());

            Customer deserializedCustomer = JsonSerializer.Deserialize<Customer>(json);

            // We serialized the Customer
            Assert.Equal(typeof(Customer), deserializedCustomer.GetType());
            deserializedCustomer.Verify();
        }

        [Fact]
        public void StaticAnalysisWithRelationship()
        {
            UsaCustomer usaCustomer = new UsaCustomer();
            usaCustomer.Initialize();
            usaCustomer.Verify();

            // Note: this could be typeof(UsaAddress) if we preserve objects created in the ctor. Currently we only preserve IEnumerables.
            Assert.Equal(typeof(Address), usaCustomer.Address.GetType());

            Customer customer = usaCustomer;

            // Generic inference used <TValue> = <Customer>
            string json = Serializer.Serialize(customer);

            UsaCustomer deserializedCustomer = JsonSerializer.Deserialize<UsaCustomer>(json);

            // We only serialized the Customer base class
            Assert.Equal(typeof(UsaCustomer), deserializedCustomer.GetType());
            Assert.Equal(typeof(Address), deserializedCustomer.Address.GetType());
            ((Customer)deserializedCustomer).VerifyNonVirtual();
        }

        [Fact]
        public void PolymorphicInterface_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<MyClass>(@"{ ""Value"": ""A value"", ""Thing"": { ""Number"": 123 } }"));
        }

        [Fact]
        public void GenericListOfInterface_WithInvalidJson_ThrowsJsonException()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MyThingCollection>("false"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MyThingCollection>("{}"));
        }

        [Fact]
        public void GenericListOfInterface_WithValidJson_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<MyThingCollection>("[{}]"));
        }

        [Fact]
        public void GenericDictionaryOfInterface_WithInvalidJson_ThrowsJsonException()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MyThingDictionary>(@"{"""":1}"));
        }

        [Fact]
        public void GenericDictionaryOfInterface_WithValidJson_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<MyThingDictionary>(@"{"""":{}}"));
        }

        [Fact]
        public void AnonymousType()
        {
            const string Expected = @"{""x"":1,""y"":true}";
            var value = new { x = 1, y = true };

            // Strongly-typed.
            string json = Serializer.Serialize(value);
            Assert.Equal(Expected, json);

            // Boxed.
            object objValue = value;
            json = Serializer.Serialize(objValue);
            Assert.Equal(Expected, json);
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

        class MyThingDictionary : Dictionary<string, IThing> { }
    }
}
