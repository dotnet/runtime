// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
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
        public async Task PrimitivesAsRootObject()
        {
            string json = await Serializer.SerializeWrapper<object>(1);
            Assert.Equal("1", json);
            json = await Serializer.SerializeWrapper(1, typeof(object));
            Assert.Equal("1", json);

            json = await Serializer.SerializeWrapper<object>("foo");
            Assert.Equal(@"""foo""", json);
            json = await Serializer.SerializeWrapper("foo", typeof(object));
            Assert.Equal(@"""foo""", json);

            json = await Serializer.SerializeWrapper<object>(true);
            Assert.Equal(@"true", json);
            json = await Serializer.SerializeWrapper(true, typeof(object));
            Assert.Equal(@"true", json);

            json = await Serializer.SerializeWrapper<object>(null);
            Assert.Equal(@"null", json);
            json = await Serializer.SerializeWrapper((object)null, typeof(object));
            Assert.Equal(@"null", json);

            decimal pi = 3.1415926535897932384626433833m;
            json = await Serializer.SerializeWrapper<object>(pi);
            Assert.Equal(@"3.1415926535897932384626433833", json);
            json = await Serializer.SerializeWrapper(pi, typeof(object));
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
        public async Task ArrayAsRootObject()
        {
            const string ExpectedJson = @"[1,true,{""City"":""MyCity""},null,""foo""]";
            const string ReversedExpectedJson = @"[""foo"",null,{""City"":""MyCity""},true,1]";

            string[] expectedObjects = { @"""foo""", @"null", @"{""City"":""MyCity""}", @"true", @"1" };

            var address = new Address();
            address.Initialize();

            var array = new object[] { 1, true, address, null, "foo" };
            string json = await Serializer.SerializeWrapper(array);
            Assert.Equal(ExpectedJson, json);

            var dictionary = new Dictionary<string, string> { { "City", "MyCity" } };
            var arrayWithDictionary = new object[] { 1, true, dictionary, null, "foo" };
            json = await Serializer.SerializeWrapper(arrayWithDictionary);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(array);
            Assert.Equal(ExpectedJson, json);

            List<object> list = new List<object> { 1, true, address, null, "foo" };
            json = await Serializer.SerializeWrapper(list);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(list);
            Assert.Equal(ExpectedJson, json);

            IEnumerable ienumerable = new List<object> { 1, true, address, null, "foo" };
            json = await Serializer.SerializeWrapper(ienumerable);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(ienumerable);
            Assert.Equal(ExpectedJson, json);

            IList ilist = new List<object> { 1, true, address, null, "foo" };
            json = await Serializer.SerializeWrapper(ilist);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(ilist);
            Assert.Equal(ExpectedJson, json);

            ICollection icollection = new List<object> { 1, true, address, null, "foo" };
            json = await Serializer.SerializeWrapper(icollection);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(icollection);
            Assert.Equal(ExpectedJson, json);

            IEnumerable<object> genericIEnumerable = new List<object> { 1, true, address, null, "foo" };
            json = await Serializer.SerializeWrapper(genericIEnumerable);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(genericIEnumerable);
            Assert.Equal(ExpectedJson, json);

            IList<object> genericIList = new List<object> { 1, true, address, null, "foo" };
            json = await Serializer.SerializeWrapper(genericIList);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(genericIList);
            Assert.Equal(ExpectedJson, json);

            ICollection<object> genericICollection = new List<object> { 1, true, address, null, "foo" };
            json = await Serializer.SerializeWrapper(genericICollection);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(genericICollection);
            Assert.Equal(ExpectedJson, json);

            IReadOnlyCollection<object> genericIReadOnlyCollection = new List<object> { 1, true, address, null, "foo" };
            json = await Serializer.SerializeWrapper(genericIReadOnlyCollection);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(genericIReadOnlyCollection);
            Assert.Equal(ExpectedJson, json);

            IReadOnlyList<object> genericIReadonlyList = new List<object> { 1, true, address, null, "foo" };
            json = await Serializer.SerializeWrapper(genericIReadonlyList);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(genericIReadonlyList);
            Assert.Equal(ExpectedJson, json);

            ISet<object> iset = new HashSet<object> { 1, true, address, null, "foo" };
            json = await Serializer.SerializeWrapper(iset);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(iset);
            Assert.Equal(ExpectedJson, json);

            Stack<object> stack = new Stack<object>(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(stack);
            Assert.Equal(ReversedExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(stack);
            Assert.Equal(ReversedExpectedJson, json);

            Queue<object> queue = new Queue<object>(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(queue);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(queue);
            Assert.Equal(ExpectedJson, json);

            HashSet<object> hashset = new HashSet<object>(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(hashset);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(hashset);
            Assert.Equal(ExpectedJson, json);

            LinkedList<object> linkedlist = new LinkedList<object>(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(linkedlist);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(linkedlist);
            Assert.Equal(ExpectedJson, json);

            ImmutableArray<object> immutablearray = ImmutableArray.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(immutablearray);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(immutablearray);
            Assert.Equal(ExpectedJson, json);

            IImmutableList<object> iimmutablelist = ImmutableList.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(iimmutablelist);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(iimmutablelist);
            Assert.Equal(ExpectedJson, json);

            IImmutableStack<object> iimmutablestack = ImmutableStack.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(iimmutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(iimmutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            IImmutableQueue<object> iimmutablequeue = ImmutableQueue.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(iimmutablequeue);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(iimmutablequeue);
            Assert.Equal(ExpectedJson, json);

            IImmutableSet<object> iimmutableset = ImmutableHashSet.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(iimmutableset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            json = await Serializer.SerializeWrapper<object>(iimmutableset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            ImmutableHashSet<object> immutablehashset = ImmutableHashSet.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(immutablehashset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            json = await Serializer.SerializeWrapper<object>(immutablehashset);
            foreach (string obj in expectedObjects)
            {
                Assert.Contains(obj, json);
            }

            ImmutableList<object> immutablelist = ImmutableList.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(immutablelist);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(immutablelist);
            Assert.Equal(ExpectedJson, json);

            ImmutableStack<object> immutablestack = ImmutableStack.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(immutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(immutablestack);
            Assert.Equal(ReversedExpectedJson, json);

            ImmutableQueue<object> immutablequeue = ImmutableQueue.CreateRange(new List<object> { 1, true, address, null, "foo" });
            json = await Serializer.SerializeWrapper(immutablequeue);
            Assert.Equal(ExpectedJson, json);

            json = await Serializer.SerializeWrapper<object>(immutablequeue);
            Assert.Equal(ExpectedJson, json);
        }

        [Fact]
        public async Task SimpleTestClassAsRootObject()
        {
            // Sanity checks on test type.
            Assert.Equal(typeof(object), typeof(SimpleTestClassWithObject).GetProperty("MyInt16").PropertyType);
            Assert.Equal(typeof(object), typeof(SimpleTestClassWithObject).GetProperty("MyBooleanTrue").PropertyType);
            Assert.Equal(typeof(object), typeof(SimpleTestClassWithObject).GetProperty("MyInt16Array").PropertyType);

            var obj = new SimpleTestClassWithObject();
            obj.Initialize();

            // Verify with actual type.
            string json = await Serializer.SerializeWrapper(obj);
            Assert.Contains(@"""MyInt16"":1", json);
            Assert.Contains(@"""MyBooleanTrue"":true", json);
            Assert.Contains(@"""MyInt16Array"":[1]", json);

            // Verify with object type.
            json = await Serializer.SerializeWrapper<object>(obj);
            Assert.Contains(@"""MyInt16"":1", json);
            Assert.Contains(@"""MyBooleanTrue"":true", json);
            Assert.Contains(@"""MyInt16Array"":[1]", json);
        }

        [Fact]
        public async Task NestedObjectAsRootObject()
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

            string json = await Serializer.SerializeWrapper(obj);
            Verify(json);

            json = await Serializer.SerializeWrapper<object>(obj);
            Verify(json);
        }

        [Fact]
        public async Task NestedObjectAsRootObjectIgnoreNullable()
        {
            // Ensure that null properties are properly written and support ignore.
            var obj = new ObjectWithObjectProperties();
            obj.NullableInt = null;

            string json = await Serializer.SerializeWrapper(obj);
            Assert.Contains(@"""NullableInt"":null", json);

            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IgnoreNullValues = true;
            json = await Serializer.SerializeWrapper(obj, options);
            Assert.DoesNotContain(@"""NullableInt"":null", json);
        }

        [Fact]
        public async Task StaticAnalysisBaseline()
        {
            Customer customer = new Customer();
            customer.Initialize();
            customer.Verify();

            string json = await Serializer.SerializeWrapper(customer);
            Customer deserializedCustomer = JsonSerializer.Deserialize<Customer>(json);
            deserializedCustomer.Verify();
        }

        [Fact]
        public async Task StaticAnalysis()
        {
            Customer customer = new Customer();
            customer.Initialize();
            customer.Verify();

            Person person = customer;

            // Generic inference used <TValue> = <Person>
            string json = await Serializer.SerializeWrapper(person);

            Customer deserializedCustomer = JsonSerializer.Deserialize<Customer>(json);

            // We only serialized the Person base class, so the Customer fields should be default.
            Assert.Equal(typeof(Customer), deserializedCustomer.GetType());
            Assert.Equal(0, deserializedCustomer.CreditLimit);
            ((Person)deserializedCustomer).VerifyNonVirtual();
        }

        [Fact]
        public async Task WriteStringWithRuntimeType()
        {
            Customer customer = new Customer();
            customer.Initialize();
            customer.Verify();

            Person person = customer;

            string json = await Serializer.SerializeWrapper(person, person.GetType());

            Customer deserializedCustomer = JsonSerializer.Deserialize<Customer>(json);

            // We serialized the Customer
            Assert.Equal(typeof(Customer), deserializedCustomer.GetType());
            deserializedCustomer.Verify();
        }

        [Fact]
        public async Task StaticAnalysisWithRelationship()
        {
            UsaCustomer usaCustomer = new UsaCustomer();
            usaCustomer.Initialize();
            usaCustomer.Verify();

            // Note: this could be typeof(UsaAddress) if we preserve objects created in the ctor. Currently we only preserve IEnumerables.
            Assert.Equal(typeof(Address), usaCustomer.Address.GetType());

            Customer customer = usaCustomer;

            // Generic inference used <TValue> = <Customer>
            string json = await Serializer.SerializeWrapper(customer);

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
        public async Task AnonymousType()
        {
            const string Expected = @"{""x"":1,""y"":true}";
            var value = new { x = 1, y = true };

            // Strongly-typed.
            string json = await Serializer.SerializeWrapper(value);
            Assert.Equal(Expected, json);

            // Boxed.
            object objValue = value;
            json = await Serializer.SerializeWrapper(objValue);
            Assert.Equal(Expected, json);
        }

        [Fact]
        public async Task ConcreteClass_RootValue_PolymorphismDisabled_ShouldSerializeAsBase()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = _ => false };
            ConcreteClass value = new DerivedClass { Boolean = true, Number = 42 };
            string expectedJson = @"{""Boolean"":true}";

            string actualJson = await Serializer.SerializeWrapper(value, options);

            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public async Task ConcreteClass_RootValue_PolymorphismEnabled_ShouldSerializePolymorphically()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = type => type == typeof(ConcreteClass) };
            ConcreteClass value = new DerivedClass { Boolean = true, Number = 42 };
            string expectedJson = @"{""Number"":42,""Boolean"":true}";

            string actualJson = await Serializer.SerializeWrapper(value, options);

            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void ConcreteClass_RootValue_PolymorphismEnabled_ShouldDeserializeAsBase()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = type => type == typeof(ConcreteClass) };
            string json = @"{""Number"":42,""Boolean"":true}";

            ConcreteClass result = JsonSerializer.Deserialize<ConcreteClass>(json, options);

            Assert.IsType<ConcreteClass>(result);
            Assert.True(result.Boolean);
        }

        [Fact]
        public async Task ConcreteClass_NestedValue_PolymorphismDisabled_ShouldSerializeAsBase()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = _ => false };
            List<ConcreteClass> value = new() { new DerivedClass { Boolean = true, Number = 42 } };
            string expectedJson = @"[{""Boolean"":true}]";

            string actualJson = await Serializer.SerializeWrapper(value);
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public async Task ConcreteClass_NestedValue_PolymorphismEnabled_ShouldSerializePolymorphically()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = type => type == typeof(ConcreteClass) };
            List<ConcreteClass> value = new() { new DerivedClass { Boolean = true, Number = 42 } };
            string expectedJson = @"[{""Number"":42,""Boolean"":true}]";

            string actualJson = await Serializer.SerializeWrapper(value, options);
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void ConcreteClass_NestedValue_PolymorphismEnabled_ShouldDeserializeAsBase()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = type => type == typeof(ConcreteClass) };
            string json = @"[{""Number"":42,""Boolean"":true}]";

            List<ConcreteClass> result = JsonSerializer.Deserialize<List<ConcreteClass>>(json, options);

            Assert.Equal(1, result.Count);
            Assert.IsType<ConcreteClass>(result[0]);
            Assert.True(result[0].Boolean);
        }

        [Fact]
        public async Task AbstractClass_RootValue_PolymorphismDisabled_ShouldSerializeAsBase()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = _ => false };
            AbstractClass value = new ConcreteClass { Boolean = true };
            string expectedJson = @"{}";

            string actualJson = await Serializer.SerializeWrapper(value, options);

            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public async Task AbstractClass_RootValue_PolymorphismEnabled_ShouldSerializePolymorphically()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = type => type == typeof(AbstractClass) };
            AbstractClass value = new ConcreteClass { Boolean = true };
            string expectedJson = @"{""Boolean"":true}";

            string actualJson = await Serializer.SerializeWrapper(value, options);

            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void AbstractClass_RootValue_PolymorphismEnabled_ShouldFailDeserialization()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = type => type == typeof(AbstractClass) };
            string json = @"{""Boolean"":true}";

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<AbstractClass>(json, options));
        }

        [Fact]
        public async Task AbstractClass_NestedValue_PolymorphismDisabled_ShouldSerializeAsBase()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = _ => false };
            List<AbstractClass> value = new() { new ConcreteClass { Boolean = true } };
            string expectedJson = @"[{}]";

            string actualJson = await Serializer.SerializeWrapper(value);
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public async Task AbstractClass_NestedValue_PolymorphismEnabled_ShouldSerializePolymorphically()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = type => type == typeof(AbstractClass) };
            List<AbstractClass> value = new() { new ConcreteClass { Boolean = true } };
            string expectedJson = @"[{""Boolean"":true}]";

            string actualJson = await Serializer.SerializeWrapper(value, options);
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void AbstractClass_NestedValue_PolymorphismEnabled_ShouldFailDeserialization()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = type => type == typeof(AbstractClass) };
            string json = @"[{""Boolean"":true}]";

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<List<AbstractClass>>(json, options));
        }

        [Fact]
        public async Task Interface_RootValue_PolymorphismDisabled_ShouldSerializeAsBase()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = _ => false };
            IThing value = new MyOtherThing { Number = 42, OtherNumber = 24 };
            string expectedJson = @"{""Number"":42}";

            string actualJson = await Serializer.SerializeWrapper(value, options);
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public async Task Interface_RootValue_PolymorphismEnabled_ShouldSerializePolymorphically()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = type => type == typeof(IThing) };
            IThing value = new MyOtherThing { Number = 42, OtherNumber = 24 };
            string expectedJson = @"{""Number"":42,""OtherNumber"":24}";

            string actualJson = await Serializer.SerializeWrapper(value, options);
            Assert.Equal(expectedJson, actualJson);
        }

        [Theory]
        [MemberData(nameof(GetObjectsContainingNestedIThingValues))]
        public async Task Interface_NestedValues_PolymorphismDisabled_ShouldSerializeAsBase<TValue>(TValue value, string expectedJson)
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = _ => false };
            string actualJson = await Serializer.SerializeWrapper(value, options);
            Assert.NotEqual(expectedJson, actualJson);
        }

        [Theory]
        [MemberData(nameof(GetObjectsContainingNestedIThingValues))]
        public async Task Interface_NestedValues_PolymorphismEnabled_ShouldSerializePolymorphically<TValue>(TValue value, string expectedJson)
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = type => type == typeof(IThing) };
            string actualJson = await Serializer.SerializeWrapper(value, options);
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void Interface_RootValue_PolymorphismEnabled_ShouldFailDeserialization()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = type => type == typeof(IThing) };
            string json = @"{""Number"":42,""OtherNumber"":24}";
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<IThing>(json, options));
        }

        [Theory]
        [MemberData(nameof(GetObjectsContainingNestedIThingValues))]
        public void Interface_NestedValues_PolymorphismEnabled_ShouldFailDeserialization<TValue>(TValue value, string json)
        {
            _ = value;
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = type => type == typeof(IThing) };
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<TValue>(json, options));
        }

        [Fact]
        public async Task PolymorphismEnabledForAllTypes_ShouldSerializeAllValuesPolymorphically()
        {
            var options = new JsonSerializerOptions { SupportedPolymorphicTypes = _ => true };
            object[] value = new object[]
            {
                42,
                "string",
                CreateList<IThing>(new MyOtherThing { Number = 42, OtherNumber = -1 }),
                CreateList<AbstractClass>(new DerivedClass { Number = 42, Boolean = true }, new ConcreteClass { Boolean = false }),
                CreateList<ConcreteClass>(new DerivedClass { Number = 42, Boolean = true }),
            };

            string expectedJson = @"[42,""string"",[{""Number"":42,""OtherNumber"":-1}],[{""Number"":42,""Boolean"":true},{""Boolean"":false}],[{""Number"":42,""Boolean"":true}]]";
            string actualJson = await Serializer.SerializeWrapper(value, options);

            Assert.Equal(expectedJson, actualJson);

            static List<T> CreateList<T>(params T[] values) => new(values);
        }


        [Fact]
        public async Task ClassWithCustomConverter_RootValue_ShouldNotBePolymorphic()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new CustomIThingConverter() },
                SupportedPolymorphicTypes = type => type == typeof(IThing)
            };

            IThing value = new MyOtherThing { Number = 42, OtherNumber = 21 };

            string expectedJson = "42";
            string actualJson = await Serializer.SerializeWrapper(value, options);

            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public async Task ClassWithCustomConverter_NestedValue_ShouldNotBePolymorphic()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new CustomIThingConverter() },
                SupportedPolymorphicTypes = type => type == typeof(IThing)
            };

            var value = new { Value = (IThing)new MyOtherThing { Number = 42, OtherNumber = 21 } };

            string expectedJson = @"{""Value"":42}";
            string actualJson = await Serializer.SerializeWrapper(value, options);

            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public async Task RootObjectValue_PolymorphismCannotBeDisabled()
        {
            var options = new JsonSerializerOptions
            {
                SupportedPolymorphicTypes = type => false
            };

            object value = new { Value = 42 };

            string expectedJson = @"{""Value"":42}";
            string actualJson = await Serializer.SerializeWrapper(value, options);

            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public async Task NestedObjectValue_PolymorphismCannotBeDisabled()
        {
            var options = new JsonSerializerOptions
            {
                SupportedPolymorphicTypes = type => false
            };

            var value = new { Value = (object)new { Value = 42 } };

            string expectedJson = @"{""Value"":{""Value"":42}}";
            string actualJson = await Serializer.SerializeWrapper(value, options);

            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public async Task JsonPolymorphicTypeAttribute_Class()
        {
            PolymorphicAnnotatedClass value = new DerivedFromPolymorphicAnnotatedClass { A = 1, B = 2, C = 3 };
            string expectedJson = @"{""A"":1,""B"":2,""C"":3}";

            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);
        }

        [Fact]
        public async Task JsonPolymorphicTypeAttribute_Interface()
        {
            IPolymorphicAnnotatedInterface value = new DerivedFromPolymorphicAnnotatedClass { A = 1, B = 2, C = 3 };
            string expectedJson = @"{""A"":1,""B"":2,""C"":3}";

            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);
        }

        [Fact]
        public async Task JsonPolymorphicTypeAttribute_IsNotInheritedByDerivedTypes()
        {
            DerivedFromPolymorphicAnnotatedClass value = new DerivedFromDerivedFromPolymorphicAnnotatedClass { A = 1, B = 2, C = 3, D = 4 };
            string expectedJson = @"{""A"":1,""B"":2,""C"":3}";

            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);
        }

        public static IEnumerable<object[]> GetObjectsContainingNestedIThingValues()
        {
            yield return WrapArgs<MyThingCollection>(
                new() { new MyThing { Number = -1 }, new MyOtherThing { Number = 42, OtherNumber = 24 } },
                @"[{""Number"":-1},{""Number"":42,""OtherNumber"":24}]"
            );

            yield return WrapArgs<MyClass>(
                new() { Value = "value", Thing = new MyOtherThing { Number = 42, OtherNumber = 24 } },
                @"{""Value"":""value"",""Thing"":{""Number"":42,""OtherNumber"":24}}"
            );

            yield return WrapArgs<MyThingDictionary>(
                new()
                {
                    ["key1"] = new MyOtherThing { Number = 42, OtherNumber = 24 },
                    ["key2"] = new MyThing { Number = -1 }
                },
                @"{""key1"":{""Number"":42,""OtherNumber"":24},""key2"":{""Number"":-1}}"
            );

            static object[] WrapArgs<TValue>(TValue value, string expectedJson) => new object[] { value, expectedJson };
        }

        [Fact]

        public async Task PolymorphicCyclicObjectGraphs_CycleDetectionEnabled()
        {
            var options = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };

            IBinTree tree = new BinTree<int>
            {
                Value = 0,
                Left = new BinTree<int> { Value = -1 },
            };

            tree.Left.Left = tree;
            tree.Left.Right = tree.Left;
            tree.Right = tree;

            string expectedJson = @"{""Value"":0,""Left"":{""Value"":-1,""Left"":null,""Right"":null},""Right"":null}";
            string actualJson = await Serializer.SerializeWrapper(tree, options);
            Assert.Equal(expectedJson, actualJson);
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

        class MyOtherThing : IThing
        {
            public int Number { get; set; }
            public int OtherNumber { get; set; }
        }

        abstract class AbstractClass
        {
        }

        class ConcreteClass : AbstractClass
        {
            public bool Boolean { get; set; }
        }

        class DerivedClass : ConcreteClass
        {
            public int Number { get; set; }
        }

        class MyThingCollection : List<IThing> { }

        class MyThingDictionary : Dictionary<string, IThing> { }

        [JsonPolymorphicType]
        public class IPolymorphicAnnotatedInterface
        {
            int A { get; set; }
        }

        [JsonPolymorphicType]
        public class PolymorphicAnnotatedClass : IPolymorphicAnnotatedInterface
        {
            public int A { get; set; }
            public int B { get; set; }
        }

        public class DerivedFromPolymorphicAnnotatedClass : PolymorphicAnnotatedClass
        {
            public int C { get; set; }
        }

        public class DerivedFromDerivedFromPolymorphicAnnotatedClass : DerivedFromPolymorphicAnnotatedClass
        {
            public int D { get; set; }
        }

        [JsonPolymorphicType]
        public interface IBinTree
        {
            public IBinTree? Left { get; set; }
            public IBinTree? Right { get; set; }
        }

        public class BinTree<T> : IBinTree
        {
            public T Value { get; set; }
            public IBinTree? Left { get; set; }
            public IBinTree? Right { get; set; }
        }

        class CustomIThingConverter : JsonConverter<IThing>
        {
            public override IThing? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();
            public override void Write(Utf8JsonWriter writer, IThing value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.Number);
            }
        }
    }
}
