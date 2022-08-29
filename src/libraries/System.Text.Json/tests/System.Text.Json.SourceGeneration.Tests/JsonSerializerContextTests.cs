// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public static partial class JsonSerializerContextTests
    {
        [Fact]
        public static void VariousNestingAndVisibilityLevelsAreSupported()
        {
            Assert.NotNull(PublicContext.Default);
            Assert.NotNull(NestedContext.Default);
            Assert.NotNull(NestedPublicContext.Default);
            Assert.NotNull(NestedPublicContext.NestedProtectedInternalClass.Default);
        }

        [Fact]
        public static void VariousGenericsAreSupported()
        {
            AssertGenericContext(GenericContext<int>.Default);
            AssertGenericContext(ContextGenericContainer<int>.NestedInGenericContainerContext.Default);
            AssertGenericContext(ContextGenericContainer<int>.NestedGenericInGenericContainerContext<int>.Default);
            AssertGenericContext(ContextGenericContainer<int>.NestedGenericContainer<int>.NestedInNestedGenericContainerContext.Default);
            AssertGenericContext(ContextGenericContainer<int>.NestedGenericContainer<int>.NestedGenericInNestedGenericContainerContext<int>.Default);

            Assert.NotNull(NestedGenericTypesContext.Default);
            var original = new MyContainingGenericClass<int>.MyNestedGenericClass<int>.MyNestedGenericNestedGenericClass<int>()
            {
                DataT = 1,
                DataT1 = 10,
                DataT2 = 100
            };
            Type type = typeof(MyContainingGenericClass<int>.MyNestedGenericClass<int>.MyNestedGenericNestedGenericClass<int>);
            string json = JsonSerializer.Serialize(original, type, NestedGenericTypesContext.Default);
            var deserialized = (MyContainingGenericClass<int>.MyNestedGenericClass<int>.MyNestedGenericNestedGenericClass<int>)JsonSerializer.Deserialize(json, type, NestedGenericTypesContext.Default);
            Assert.Equal(1, deserialized.DataT);
            Assert.Equal(10, deserialized.DataT1);
            Assert.Equal(100, deserialized.DataT2);

            static void AssertGenericContext(JsonSerializerContext context)
            {
                Assert.NotNull(context);
                string json = JsonSerializer.Serialize(new JsonMessage { Message = "Hi" }, typeof(JsonMessage), context);
                JsonMessage deserialized = (JsonMessage)JsonSerializer.Deserialize(json, typeof(JsonMessage), context);
                Assert.Equal("Hi", deserialized.Message);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/63802", TargetFrameworkMonikers.NetFramework)]
        public static void Converters_AndTypeInfoCreator_NotRooted_WhenMetadataNotPresent()
        {
            RemoteExecutor.Invoke(
                static () =>
                {
                    object[] objArr = new object[] { new MyStruct() };

                    // Metadata not generated for MyStruct without JsonSerializableAttribute.
                    NotSupportedException ex = Assert.Throws<NotSupportedException>(
                        () => JsonSerializer.Serialize(objArr, MetadataContext.Default.ObjectArray));
                    string exAsStr = ex.ToString();
                    Assert.Contains(typeof(MyStruct).ToString(), exAsStr);
                    Assert.Contains("JsonSerializerOptions", exAsStr);

                    // This test uses reflection to:
                    // - Access DefaultJsonTypeInfoResolver.s_defaultSimpleConverters
                    // - Access DefaultJsonTypeInfoResolver.s_defaultFactoryConverters
                    //
                    // If any of them changes, this test will need to be kept in sync.

                    // Confirm built-in converters not set.
                    AssertFieldNull("s_defaultSimpleConverters");
                    AssertFieldNull("s_defaultFactoryConverters");

                    static void AssertFieldNull(string fieldName)
                    {
                        FieldInfo fieldInfo = typeof(DefaultJsonTypeInfoResolver).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
                        Assert.NotNull(fieldInfo);
                        Assert.Null(fieldInfo.GetValue(null));
                    }
                }).Dispose();
        }

        [Fact]
        public static void SupportsReservedLanguageKeywordsAsProperties()
        {
            GreetingCard card = new()
            {
                @event = "Birthday",
                message = @"Happy Birthday!"
            };

            byte[] utf8Json = JsonSerializer.SerializeToUtf8Bytes(card, GreetingCardJsonContext.Default.GreetingCard);

            card = JsonSerializer.Deserialize<GreetingCard>(utf8Json, GreetingCardJsonContext.Default.GreetingCard);
            Assert.Equal("Birthday", card.@event);
            Assert.Equal("Happy Birthday!", card.message);
        }

        [Fact]
        public static void SupportsReservedLanguageKeywordsAsFields()
        {
            var options = new JsonSerializerOptions { IncludeFields = true };

            GreetingCardWithFields card = new() {@event = "Birthday", message = @"Happy Birthday!"};
        
            byte[] utf8Json = JsonSerializer.SerializeToUtf8Bytes(card, GreetingCardWithFieldsJsonContext.Default.GreetingCardWithFields);
        
            card = JsonSerializer.Deserialize<GreetingCardWithFields>(utf8Json, GreetingCardWithFieldsJsonContext.Default.GreetingCardWithFields);
            Assert.Equal("Happy Birthday!", card.message);
            Assert.Equal("Birthday", card.@event);
        }

        [Fact]
        public static void SupportsPositionalRecords()
        {
            Person person = new(FirstName: "Jane", LastName: "Doe");

            byte[] utf8Json = JsonSerializer.SerializeToUtf8Bytes(person, PersonJsonContext.Default.Person);

            person = JsonSerializer.Deserialize<Person>(utf8Json, PersonJsonContext.Default.Person);
            Assert.Equal("Jane", person.FirstName);
            Assert.Equal("Doe", person.LastName);
        }

        [Fact]
        public static void CombiningContexts_ResolveJsonTypeInfo()
        {
            IJsonTypeInfoResolver combined = JsonTypeInfoResolver.Combine(NestedContext.Default, PersonJsonContext.Default);
            var options = new JsonSerializerOptions { TypeInfoResolver = combined };

            JsonTypeInfo messageInfo = combined.GetTypeInfo(typeof(JsonMessage), options);
            Assert.IsAssignableFrom<JsonTypeInfo<JsonMessage>>(messageInfo);
            Assert.Same(options, messageInfo.Options);

            JsonTypeInfo personInfo = combined.GetTypeInfo(typeof(Person), options);
            Assert.IsAssignableFrom<JsonTypeInfo<Person>>(personInfo);
            Assert.Same(options, personInfo.Options);
        }

        [Fact]
        public static void CombiningContexts_ResolveJsonTypeInfo_DifferentCasing()
        {
            IJsonTypeInfoResolver combined = JsonTypeInfoResolver.Combine(NestedContext.Default, PersonJsonContext.Default);
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = combined,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            Assert.NotSame(JsonNamingPolicy.CamelCase, NestedContext.Default.Options.PropertyNamingPolicy);
            Assert.Same(JsonNamingPolicy.CamelCase, PersonJsonContext.Default.Options.PropertyNamingPolicy);

            JsonTypeInfo messageInfo = combined.GetTypeInfo(typeof(JsonMessage), options);
            Assert.Equal(2, messageInfo.Properties.Count);
            Assert.Equal("message", messageInfo.Properties[0].Name);
            Assert.Equal("length", messageInfo.Properties[1].Name);

            JsonTypeInfo personInfo = combined.GetTypeInfo(typeof(Person), options);
            Assert.Equal(2, personInfo.Properties.Count);
            Assert.Equal("firstName", personInfo.Properties[0].Name);
            Assert.Equal("lastName", personInfo.Properties[1].Name);
        }

        [Fact]
        public static void FastPathSerialization_ResolvingJsonTypeInfo()
        {
            JsonSerializerOptions options = FastPathSerializationContext.Default.Options;

            JsonTypeInfo<JsonMessage> jsonMessageInfo = (JsonTypeInfo<JsonMessage>)options.GetTypeInfo(typeof(JsonMessage));
            Assert.NotNull(jsonMessageInfo.SerializeHandler);

            var value = new JsonMessage { Message = "Hi" };
            string expectedJson = """{"Message":"Hi","Length":2}""";

            Assert.Equal(expectedJson, JsonSerializer.Serialize(value, jsonMessageInfo));
            Assert.Equal(expectedJson, JsonSerializer.Serialize(value, options));

            // Throws since deserialization without metadata is not supported
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<JsonMessage>(expectedJson, jsonMessageInfo));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<JsonMessage>(expectedJson, options));
        }

        [Fact]
        public static void FastPathSerialization_CombinedContext_ThrowsInvalidOperationException()
        {
            // TODO change exception assertions once https://github.com/dotnet/runtime/issues/71933 is fixed.

            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonTypeInfoResolver.Combine(FastPathSerializationContext.Default, new DefaultJsonTypeInfoResolver())
            };

            JsonTypeInfo<JsonMessage> jsonMessageInfo = (JsonTypeInfo<JsonMessage>)options.GetTypeInfo(typeof(JsonMessage));
            Assert.NotNull(jsonMessageInfo.SerializeHandler);

            var value = new JsonMessage { Message = "Hi" };
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(value, jsonMessageInfo));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(value, options));

            JsonTypeInfo<ClassWithJsonMessage> classInfo = (JsonTypeInfo<ClassWithJsonMessage>)options.GetTypeInfo(typeof(ClassWithJsonMessage));
            Assert.Null(classInfo.SerializeHandler);

            var largerValue = new ClassWithJsonMessage { Message = value };
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(largerValue, classInfo));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(largerValue, options));
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
        [JsonSerializable(typeof(JsonMessage))]
        public partial class FastPathSerializationContext : JsonSerializerContext
        { }

        public class ClassWithJsonMessage
        {
            public JsonMessage Message { get; set; }
        }

        [Theory]
        [MemberData(nameof(GetCombiningContextsData))]
        public static void CombiningContexts_Serialization<T>(T value, string expectedJson)
        {
            IJsonTypeInfoResolver combined = JsonTypeInfoResolver.Combine(NestedContext.Default, PersonJsonContext.Default);
            var options = new JsonSerializerOptions { TypeInfoResolver = combined };

            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)combined.GetTypeInfo(typeof(T), options)!;

            string json = JsonSerializer.Serialize(value, typeInfo);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);

            json = JsonSerializer.Serialize(value, options);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);

            JsonSerializer.Deserialize<T>(json, typeInfo);
            JsonSerializer.Deserialize<T>(json, options);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void CombiningContextWithCustomResolver_ReplacePoco()
        {
            TestResolver customResolver = new((type, options) =>
            {
                if (type != typeof(TestPoco))
                    return null;

                JsonTypeInfo<TestPoco> typeInfo = JsonTypeInfo.CreateJsonTypeInfo<TestPoco>(options);
                typeInfo.CreateObject = () => new TestPoco();
                JsonPropertyInfo property = typeInfo.CreateJsonPropertyInfo(typeof(string), "test");
                property.Get = (o) => System.Runtime.CompilerServices.Unsafe.Unbox<TestPoco>(o).IntProperty.ToString();
                property.Set = (o, val) =>
                {
                    System.Runtime.CompilerServices.Unsafe.Unbox<TestPoco>(o).StringProperty = (string)val;
                    System.Runtime.CompilerServices.Unsafe.Unbox<TestPoco>(o).IntProperty = int.Parse((string)val);
                };

                typeInfo.Properties.Add(property);
                return typeInfo;
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = JsonTypeInfoResolver.Combine(customResolver, ClassWithPocoListDictionaryAndNullablePropertyContext.Default);

            // ensure we're not falling back to reflection serialization
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(new Person("a", "b"), o));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize((byte)1, o));

            ClassWithPocoListDictionaryAndNullable obj = new()
            {
                UIntProperty = 13,
                ListOfPocoProperty = new List<TestPoco>() { new TestPoco() { IntProperty = 4 }, new TestPoco() { IntProperty = 5 } },
                DictionaryPocoValueProperty = new Dictionary<char, TestPoco>() { ['c'] = new TestPoco() { IntProperty = 6 }, ['d'] = new TestPoco() { IntProperty = 7 } },
                NullablePocoProperty = new TestPoco() { IntProperty = 8 },
                PocoProperty = new TestPoco() { IntProperty = 9 },
            };

            string json = JsonSerializer.Serialize(obj, o);
            Assert.Equal("""{"UIntProperty":13,"ListOfPocoProperty":[{"test":"4"},{"test":"5"}],"DictionaryPocoValueProperty":{"c":{"test":"6"},"d":{"test":"7"}},"NullablePocoProperty":{"test":"8"},"PocoProperty":{"test":"9"}}""", json);

            ClassWithPocoListDictionaryAndNullable deserialized = JsonSerializer.Deserialize<ClassWithPocoListDictionaryAndNullable>(json, o);
            Assert.Equal(obj.UIntProperty, deserialized.UIntProperty);
            Assert.Equal(obj.ListOfPocoProperty.Count, deserialized.ListOfPocoProperty.Count);
            Assert.Equal(2, obj.ListOfPocoProperty.Count);
            Assert.Equal(obj.ListOfPocoProperty[0].IntProperty.ToString(), deserialized.ListOfPocoProperty[0].StringProperty);
            Assert.Equal(obj.ListOfPocoProperty[0].IntProperty, deserialized.ListOfPocoProperty[0].IntProperty);
            Assert.Equal(obj.ListOfPocoProperty[1].IntProperty.ToString(), deserialized.ListOfPocoProperty[1].StringProperty);
            Assert.Equal(obj.ListOfPocoProperty[1].IntProperty, deserialized.ListOfPocoProperty[1].IntProperty);
            Assert.Equal(obj.DictionaryPocoValueProperty.Count, deserialized.DictionaryPocoValueProperty.Count);
            Assert.Equal(2, obj.DictionaryPocoValueProperty.Count);
            Assert.Equal(obj.DictionaryPocoValueProperty['c'].IntProperty.ToString(), deserialized.DictionaryPocoValueProperty['c'].StringProperty);
            Assert.Equal(obj.DictionaryPocoValueProperty['c'].IntProperty, deserialized.DictionaryPocoValueProperty['c'].IntProperty);
            Assert.Equal(obj.DictionaryPocoValueProperty['d'].IntProperty.ToString(), deserialized.DictionaryPocoValueProperty['d'].StringProperty);
            Assert.Equal(obj.DictionaryPocoValueProperty['d'].IntProperty, deserialized.DictionaryPocoValueProperty['d'].IntProperty);
            Assert.Equal(obj.NullablePocoProperty.Value.IntProperty.ToString(), deserialized.NullablePocoProperty.Value.StringProperty);
            Assert.Equal(obj.NullablePocoProperty.Value.IntProperty, deserialized.NullablePocoProperty.Value.IntProperty);
            Assert.Equal(obj.PocoProperty.IntProperty.ToString(), deserialized.PocoProperty.StringProperty);
            Assert.Equal(obj.PocoProperty.IntProperty, deserialized.PocoProperty.IntProperty);
        }

        public static IEnumerable<object[]> GetCombiningContextsData()
        {
            yield return WrapArgs(new JsonMessage { Message = "Hi" }, """{ "Message" : "Hi", "Length" : 2 }""");
            yield return WrapArgs(new Person("John", "Doe"), """{ "FirstName" : "John", "LastName" : "Doe" }""");
            static object[] WrapArgs<T>(T value, string expectedJson) => new object[] { value, expectedJson };
        }

        [JsonSerializable(typeof(JsonMessage))]
        internal partial class NestedContext : JsonSerializerContext { }

        [JsonSerializable(typeof(JsonMessage))]
        public partial class NestedPublicContext : JsonSerializerContext
        {
            [JsonSerializable(typeof(JsonMessage))]
            protected internal partial class NestedProtectedInternalClass : JsonSerializerContext { }
        }

        internal record Person(string FirstName, string LastName);

        [JsonSourceGenerationOptions(
            PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
        [JsonSerializable(typeof(Person))]
        internal partial class PersonJsonContext : JsonSerializerContext
        {
        }

        internal class GreetingCard
        {
            public string @event { get;set; }
            public string message { get;set; }
        }

        internal class GreetingCardWithFields
        {
            public string @event;
            public string message;
        }

        [JsonSerializable(typeof(GreetingCard))]
        internal partial class GreetingCardJsonContext : JsonSerializerContext
        {
        }

        [JsonSourceGenerationOptions(IncludeFields = true)]
        [JsonSerializable(typeof(GreetingCardWithFields))]
        internal partial class GreetingCardWithFieldsJsonContext : JsonSerializerContext
        {
        }

        // Regression test for https://github.com/dotnet/runtime/issues/62079
        [Fact]
        public static void SupportsPropertiesWithCustomConverterFactory()
        {
            var value = new ClassWithCustomConverterFactoryProperty { MyEnum = Serialization.Tests.SampleEnum.MinZero };
            string json = JsonSerializer.Serialize(value, SingleClassWithCustomConverterFactoryPropertyContext.Default.ClassWithCustomConverterFactoryProperty);
            Assert.Equal(@"{""MyEnum"":""MinZero""}", json);
        }

        public class ParentClass
        {
            public ClassWithCustomConverterFactoryProperty? Child { get; set; }
        }

        [JsonSerializable(typeof(ParentClass))]
        internal partial class SingleClassWithCustomConverterFactoryPropertyContext : JsonSerializerContext
        {
        }

        // Regression test for https://github.com/dotnet/runtime/issues/61860
        [Fact]
        public static void SupportsGenericParameterWithCustomConverterFactory()
        {
            var value = new List<TestEnum> { TestEnum.Cee };
            string json = JsonSerializer.Serialize(value, GenericParameterWithCustomConverterFactoryContext.Default.ListTestEnum);
            Assert.Equal(@"[""Cee""]", json);
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum TestEnum
        {
            Aye, Bee, Cee
        }

        [JsonSerializable(typeof(List<TestEnum>))]
        internal partial class GenericParameterWithCustomConverterFactoryContext : JsonSerializerContext
        {
        }

        [JsonSerializable(typeof(ClassWithPocoListDictionaryAndNullable))]
        internal partial class ClassWithPocoListDictionaryAndNullablePropertyContext : JsonSerializerContext
        {

        }

        internal class ClassWithPocoListDictionaryAndNullable
        {
            public uint UIntProperty { get; set; }
            public List<TestPoco> ListOfPocoProperty { get; set; }
            public Dictionary<char, TestPoco> DictionaryPocoValueProperty { get; set; }
            public TestPoco? NullablePocoProperty { get; set; }
            public TestPoco PocoProperty { get; set; }
        }

        internal struct TestPoco
        {
            public string StringProperty { get; set; }
            public int IntProperty { get; set; }
        }

        internal class TestResolver : IJsonTypeInfoResolver
        {
            private Func<Type, JsonSerializerOptions, JsonTypeInfo?> _getTypeInfo;

            public TestResolver(Func<Type, JsonSerializerOptions, JsonTypeInfo?> getTypeInfo)
            {
                _getTypeInfo = getTypeInfo;
            }

            public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) => _getTypeInfo(type, options);
        }
    }
}
