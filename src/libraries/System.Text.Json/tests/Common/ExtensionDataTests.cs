// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract class ExtensionDataTests : SerializerTests
    {
        public ExtensionDataTests(JsonSerializerWrapper serializerWrapper) : base(serializerWrapper) { }

        [Fact]
        public async Task EmptyPropertyName_WinsOver_ExtensionDataEmptyPropertyName()
        {
            string json = @"{"""":1}";

            ClassWithEmptyPropertyNameAndExtensionProperty obj;

            // Create a new options instances to re-set any caches.
            JsonSerializerOptions options = new JsonSerializerOptions();

            // Verify the real property wins over the extension data property.
            obj = await Serializer.DeserializeWrapper<ClassWithEmptyPropertyNameAndExtensionProperty>(json, options);
            Assert.Equal(1, obj.MyInt1);
            Assert.Null(obj.MyOverflow);
        }

        [Fact]
        public async Task EmptyPropertyNameInExtensionData()
        {
            {
                string json = @"{"""":42}";
                EmptyClassWithExtensionProperty obj = await Serializer.DeserializeWrapper<EmptyClassWithExtensionProperty>(json);
                Assert.Equal(1, obj.MyOverflow.Count);
                Assert.Equal(42, obj.MyOverflow[""].GetInt32());
            }

            {
                // Verify that last-in wins.
                string json = @"{"""":42, """":43}";
                EmptyClassWithExtensionProperty obj = await Serializer.DeserializeWrapper<EmptyClassWithExtensionProperty>(json);
                Assert.Equal(1, obj.MyOverflow.Count);
                Assert.Equal(43, obj.MyOverflow[""].GetInt32());
            }
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Needs SimpleTestClass support.")]
#endif
        public async Task ExtensionPropertyNotUsed()
        {
            string json = @"{""MyNestedClass"":" + SimpleTestClass.s_json + "}";
            ClassWithExtensionProperty obj = await Serializer.DeserializeWrapper<ClassWithExtensionProperty>(json);
            Assert.Null(obj.MyOverflow);
        }

        [Fact]
        public async Task ExtensionPropertyRoundTrip()
        {
            ClassWithExtensionProperty obj;

            {
                string json = @"{""MyIntMissing"":2, ""MyInt"":1, ""MyNestedClassMissing"":" + SimpleTestClass.s_json + "}";
                obj = await Serializer.DeserializeWrapper<ClassWithExtensionProperty>(json);
                Verify();
            }

            // Round-trip the json.
            {
                string json = await Serializer.SerializeWrapper(obj);
                obj = await Serializer.DeserializeWrapper<ClassWithExtensionProperty>(json);
                Verify();

                // The json should not contain the dictionary name.
                Assert.DoesNotContain(nameof(ClassWithExtensionProperty.MyOverflow), json);
            }

            void Verify()
            {
                Assert.NotNull(obj.MyOverflow);
                Assert.Equal(1, obj.MyInt);
                Assert.Equal(2, obj.MyOverflow["MyIntMissing"].GetInt32());

                JsonProperty[] properties = obj.MyOverflow["MyNestedClassMissing"].EnumerateObject().ToArray();

                // Verify a couple properties
                Assert.Equal(1, properties.Where(prop => prop.Name == "MyInt16").First().Value.GetInt32());
                Assert.True(properties.Where(prop => prop.Name == "MyBooleanTrue").First().Value.GetBoolean());
            }
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Needs SimpleTestClass support.")]
#endif
        public async Task ExtensionFieldNotUsed()
        {
            string json = @"{""MyNestedClass"":" + SimpleTestClass.s_json + "}";
            ClassWithExtensionField obj = await Serializer.DeserializeWrapper<ClassWithExtensionField>(json);
            Assert.Null(obj.MyOverflow);
        }

        [Fact]
        public async Task ExtensionFieldRoundTrip()
        {
            ClassWithExtensionField obj;

            {
                string json = @"{""MyIntMissing"":2, ""MyInt"":1, ""MyNestedClassMissing"":" + SimpleTestClass.s_json + "}";
                obj = await Serializer.DeserializeWrapper<ClassWithExtensionField>(json);
                Verify();
            }

            // Round-trip the json.
            {
                string json = await Serializer.SerializeWrapper(obj);
                obj = await Serializer.DeserializeWrapper<ClassWithExtensionField>(json);
                Verify();

                // The json should not contain the dictionary name.
                Assert.DoesNotContain(nameof(ClassWithExtensionField.MyOverflow), json);
            }

            void Verify()
            {
                Assert.NotNull(obj.MyOverflow);
                Assert.Equal(1, obj.MyInt);
                Assert.Equal(2, obj.MyOverflow["MyIntMissing"].GetInt32());

                JsonProperty[] properties = obj.MyOverflow["MyNestedClassMissing"].EnumerateObject().ToArray();

                // Verify a couple properties
                Assert.Equal(1, properties.Where(prop => prop.Name == "MyInt16").First().Value.GetInt32());
                Assert.True(properties.Where(prop => prop.Name == "MyBooleanTrue").First().Value.GetBoolean());
            }
        }

        [Fact]
        public async Task ExtensionPropertyIgnoredWhenWritingDefault()
        {
            string expected = @"{}";
            string actual = await Serializer.SerializeWrapper(new ClassWithExtensionPropertyAsObject());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task MultipleExtensionPropertyIgnoredWhenWritingDefault()
        {
            var obj = new ClassWithMultipleDictionaries();
            string actual = await Serializer.SerializeWrapper(obj);
            Assert.Equal("{\"ActualDictionary\":null}", actual);

            obj = new ClassWithMultipleDictionaries
            {
                ActualDictionary = new Dictionary<string, object>()
            };
            actual = await Serializer.SerializeWrapper(obj);
            Assert.Equal("{\"ActualDictionary\":{}}", actual);

            obj = new ClassWithMultipleDictionaries
            {
                MyOverflow = new Dictionary<string, object>
                {
                    { "test", "value" }
                }
            };
            actual = await Serializer.SerializeWrapper(obj);
            Assert.Equal("{\"ActualDictionary\":null,\"test\":\"value\"}", actual);

            obj = new ClassWithMultipleDictionaries
            {
                ActualDictionary = new Dictionary<string, object>(),
                MyOverflow = new Dictionary<string, object>
                {
                    { "test", "value" }
                }
            };
            actual = await Serializer.SerializeWrapper(obj);
            Assert.Equal("{\"ActualDictionary\":{},\"test\":\"value\"}", actual);
        }

        [Fact]
        public async Task ExtensionPropertyInvalidJsonFail()
        {
            const string BadJson = @"{""Good"":""OK"",""Bad"":!}";

            JsonException jsonException = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsObject>(BadJson));
            Assert.Contains("Path: $.Bad | LineNumber: 0 | BytePositionInLine: 19.", jsonException.ToString());
            Assert.NotNull(jsonException.InnerException);
            Assert.IsAssignableFrom<JsonException>(jsonException.InnerException);
            Assert.Contains("!", jsonException.InnerException.ToString());
        }

        [Fact]
        public async Task ExtensionPropertyAlreadyInstantiated()
        {
            Assert.NotNull(new ClassWithExtensionPropertyAlreadyInstantiated().MyOverflow);

            string json = @"{""MyIntMissing"":2}";

            ClassWithExtensionProperty obj = await Serializer.DeserializeWrapper<ClassWithExtensionProperty>(json);
            Assert.Equal(2, obj.MyOverflow["MyIntMissing"].GetInt32());
        }

        [Fact]
        public async Task ExtensionPropertyAsObject()
        {
            string json = @"{""MyIntMissing"":2}";

            ClassWithExtensionPropertyAsObject obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsObject>(json);
            Assert.IsType<JsonElement>(obj.MyOverflow["MyIntMissing"]);
            Assert.Equal(2, ((JsonElement)obj.MyOverflow["MyIntMissing"]).GetInt32());
        }

        [Fact]
        public async Task ExtensionPropertyCamelCasing()
        {
            // Currently we apply no naming policy. If we do (such as a ExtensionPropertyNamingPolicy), we'd also have to add functionality to the JsonDocument.

            ClassWithExtensionProperty obj;
            const string jsonWithProperty = @"{""MyIntMissing"":1}";
            const string jsonWithPropertyCamelCased = @"{""myIntMissing"":1}";

            {
                // Baseline Pascal-cased json + no casing option.
                obj = await Serializer.DeserializeWrapper<ClassWithExtensionProperty>(jsonWithProperty);
                Assert.Equal(1, obj.MyOverflow["MyIntMissing"].GetInt32());
                string json = await Serializer.SerializeWrapper(obj);
                Assert.Contains(@"""MyIntMissing"":1", json);
            }

            {
                // Pascal-cased json + camel casing option.
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                obj = await Serializer.DeserializeWrapper<ClassWithExtensionProperty>(jsonWithProperty, options);
                Assert.Equal(1, obj.MyOverflow["MyIntMissing"].GetInt32());
                string json = await Serializer.SerializeWrapper(obj, options);
                Assert.Contains(@"""MyIntMissing"":1", json);
            }

            {
                // Baseline camel-cased json + no casing option.
                obj = await Serializer.DeserializeWrapper<ClassWithExtensionProperty>(jsonWithPropertyCamelCased);
                Assert.Equal(1, obj.MyOverflow["myIntMissing"].GetInt32());
                string json = await Serializer.SerializeWrapper(obj);
                Assert.Contains(@"""myIntMissing"":1", json);
            }

            {
                // Baseline camel-cased json + camel casing option.
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                obj = await Serializer.DeserializeWrapper<ClassWithExtensionProperty>(jsonWithPropertyCamelCased, options);
                Assert.Equal(1, obj.MyOverflow["myIntMissing"].GetInt32());
                string json = await Serializer.SerializeWrapper(obj, options);
                Assert.Contains(@"""myIntMissing"":1", json);
            }
        }

        [Fact]
        public async Task NullValuesIgnored()
        {
            const string json = @"{""MyNestedClass"":null}";
            const string jsonMissing = @"{""MyNestedClassMissing"":null}";

            {
                // Baseline with no missing.
                ClassWithExtensionProperty obj = await Serializer.DeserializeWrapper<ClassWithExtensionProperty>(json);
                Assert.Null(obj.MyOverflow);

                string outJson = await Serializer.SerializeWrapper(obj);
                Assert.Contains(@"""MyNestedClass"":null", outJson);
            }

            {
                // Baseline with missing.
                ClassWithExtensionProperty obj = await Serializer.DeserializeWrapper<ClassWithExtensionProperty>(jsonMissing);
                Assert.Equal(1, obj.MyOverflow.Count);
                Assert.Equal(JsonValueKind.Null, obj.MyOverflow["MyNestedClassMissing"].ValueKind);
            }

            {
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.IgnoreNullValues = true;

                ClassWithExtensionProperty obj = await Serializer.DeserializeWrapper<ClassWithExtensionProperty>(jsonMissing, options);

                // Currently we do not ignore nulls in the extension data. The JsonDocument would also need to support this mode
                // for any lower-level nulls.
                Assert.Equal(1, obj.MyOverflow.Count);
                Assert.Equal(JsonValueKind.Null, obj.MyOverflow["MyNestedClassMissing"].ValueKind);
            }
        }

        public class ClassWithInvalidExtensionProperty
        {
            [JsonExtensionData]
            public Dictionary<string, int> MyOverflow { get; set; }
        }

        public class ClassWithTwoExtensionProperties
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow1 { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow2 { get; set; }
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58945")]
#endif
        public async Task InvalidExtensionPropertyFail()
        {
            // Baseline
            await Serializer.DeserializeWrapper<ClassWithExtensionProperty>(@"{}");
            await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsObject>(@"{}");

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ClassWithInvalidExtensionProperty>(@"{}"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ClassWithTwoExtensionProperties>(@"{}"));
        }

        public class ClassWithIgnoredData
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }

            [JsonIgnore]
            public int MyInt { get; set; }
        }

        [Fact]
        public async Task IgnoredDataShouldNotBeExtensionData()
        {
            ClassWithIgnoredData obj = await Serializer.DeserializeWrapper<ClassWithIgnoredData>(@"{""MyInt"":1}");

            Assert.Equal(0, obj.MyInt);
            Assert.Null(obj.MyOverflow);
        }

        public class ClassWithExtensionData<T>
        {
            [JsonExtensionData]
            public T Overflow { get; set; }
        }

        public class CustomOverflowDictionary<T> : Dictionary<string, T>
        {
        }

        public class DictionaryOverflowConverter : JsonConverter<Dictionary<string, object>>
        {
            public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
            {
                writer.WriteString("MyCustomOverflowWrite", "OverflowValueWrite");
            }
        }

        public class JsonElementOverflowConverter : JsonConverter<Dictionary<string, JsonElement>>
        {
            public override Dictionary<string, JsonElement> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<string, JsonElement> value, JsonSerializerOptions options)
            {
                writer.WriteString("MyCustomOverflowWrite", "OverflowValueWrite");
            }
        }

        public class JsonObjectOverflowConverter : JsonConverter<JsonObject>
        {
            public override JsonObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, JsonObject value, JsonSerializerOptions options)
            {
                writer.WriteString("MyCustomOverflowWrite", "OverflowValueWrite");
            }
        }

        public class CustomObjectDictionaryOverflowConverter : JsonConverter<CustomOverflowDictionary<object>>
        {
            public override CustomOverflowDictionary<object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, CustomOverflowDictionary<object> value, JsonSerializerOptions options)
            {
                writer.WriteString("MyCustomOverflowWrite", "OverflowValueWrite");
            }
        }

        public class CustomJsonElementDictionaryOverflowConverter : JsonConverter<CustomOverflowDictionary<JsonElement>>
        {
            public override CustomOverflowDictionary<JsonElement> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, CustomOverflowDictionary<JsonElement> value, JsonSerializerOptions options)
            {
                writer.WriteString("MyCustomOverflowWrite", "OverflowValueWrite");
            }
        }

        [Theory]
        [InlineData(typeof(Dictionary<string, object>), typeof(DictionaryOverflowConverter))]
        [InlineData(typeof(Dictionary<string, JsonElement>), typeof(JsonElementOverflowConverter))]
        [InlineData(typeof(CustomOverflowDictionary<object>), typeof(CustomObjectDictionaryOverflowConverter))]
        [InlineData(typeof(CustomOverflowDictionary<JsonElement>), typeof(CustomJsonElementDictionaryOverflowConverter))]
        public void ExtensionProperty_SupportsWritingToCustomSerializerWithOptions(Type overflowType, Type converterType)
        {
            typeof(ExtensionDataTests)
                .GetMethod(nameof(ExtensionProperty_SupportsWritingToCustomSerializerWithOptionsInternal), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(overflowType, converterType)
                .Invoke(null, null);
        }

        private static void ExtensionProperty_SupportsWritingToCustomSerializerWithOptionsInternal<TDictionary, TConverter>()
            where TDictionary : new()
            where TConverter : JsonConverter, new()
        {
            var root = new ClassWithExtensionData<TDictionary>()
            {
                Overflow = new TDictionary()
            };

            var options = new JsonSerializerOptions();
            options.Converters.Add(new TConverter());

            string json = JsonSerializer.Serialize(root, options);
            Assert.Equal(@"{""MyCustomOverflowWrite"":""OverflowValueWrite""}", json);
        }

        private interface IClassWithOverflow<T>
        {
            public T Overflow { get; set; }
        }

        public class ClassWithExtensionDataWithAttributedConverter : IClassWithOverflow<Dictionary<string, object>>
        {
            [JsonExtensionData]
            [JsonConverter(typeof(DictionaryOverflowConverter))]
            public Dictionary<string, object> Overflow { get; set; }
        }

        public class ClassWithJsonElementExtensionDataWithAttributedConverter : IClassWithOverflow<Dictionary<string, JsonElement>>
        {
            [JsonExtensionData]
            [JsonConverter(typeof(JsonElementOverflowConverter))]
            public Dictionary<string, JsonElement> Overflow { get; set; }
        }

        public class ClassWithCustomElementExtensionDataWithAttributedConverter : IClassWithOverflow<CustomOverflowDictionary<object>>
        {
            [JsonExtensionData]
            [JsonConverter(typeof(CustomObjectDictionaryOverflowConverter))]
            public CustomOverflowDictionary<object> Overflow { get; set; }
        }

        public class ClassWithCustomJsonElementExtensionDataWithAttributedConverter : IClassWithOverflow<CustomOverflowDictionary<JsonElement>>
        {
            [JsonExtensionData]
            [JsonConverter(typeof(CustomJsonElementDictionaryOverflowConverter))]
            public CustomOverflowDictionary<JsonElement> Overflow { get; set; }
        }

        [Theory]
        [InlineData(typeof(ClassWithExtensionDataWithAttributedConverter), typeof(Dictionary<string, object>))]
        [InlineData(typeof(ClassWithJsonElementExtensionDataWithAttributedConverter), typeof(Dictionary<string, JsonElement>))]
        [InlineData(typeof(ClassWithCustomElementExtensionDataWithAttributedConverter), typeof(CustomOverflowDictionary<object>))]
        [InlineData(typeof(ClassWithCustomJsonElementExtensionDataWithAttributedConverter), typeof(CustomOverflowDictionary<JsonElement>))]
        public void ExtensionProperty_SupportsWritingToCustomSerializerWithExplicitConverter(Type attributedType, Type dictionaryType)
        {
            typeof(ExtensionDataTests)
                .GetMethod(nameof(ExtensionProperty_SupportsWritingToCustomSerializerWithExplicitConverterInternal), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(attributedType, dictionaryType)
                .Invoke(null, null);
        }

        private static void ExtensionProperty_SupportsWritingToCustomSerializerWithExplicitConverterInternal<TRoot, TDictionary>()
            where TRoot : IClassWithOverflow<TDictionary>, new()
            where TDictionary : new()
        {
            var root = new TRoot()
            {
                Overflow = new TDictionary()
            };

            string json = JsonSerializer.Serialize(root);
            Assert.Equal(@"{""MyCustomOverflowWrite"":""OverflowValueWrite""}", json);
        }

        [Theory]
        [InlineData(typeof(Dictionary<string, object>), typeof(DictionaryOverflowConverter), typeof(object))]
        [InlineData(typeof(Dictionary<string, JsonElement>), typeof(JsonElementOverflowConverter), typeof(JsonElement))]
        [InlineData(typeof(CustomOverflowDictionary<object>), typeof(CustomObjectDictionaryOverflowConverter), typeof(object))]
        [InlineData(typeof(CustomOverflowDictionary<JsonElement>), typeof(CustomJsonElementDictionaryOverflowConverter), typeof(JsonElement))]
        public void ExtensionProperty_IgnoresCustomSerializerWithOptions(Type overflowType, Type converterType, Type elementType)
        {
            typeof(ExtensionDataTests)
                .GetMethod(nameof(ExtensionProperty_IgnoresCustomSerializerWithOptionsInternal), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(overflowType, elementType, converterType)
                .Invoke(null, null);
        }

        [Fact]
        public async Task ExtensionProperty_IgnoresCustomSerializerWithOptions_JsonObject()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonObjectOverflowConverter());

            // A custom converter for JsonObject is not allowed on an extension property.
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Serializer.DeserializeWrapper<ClassWithExtensionData<JsonObject>>(@"{""TestKey"":""TestValue""}", options));

            Assert.Contains("JsonObject", ex.ToString());
        }

        private static void ExtensionProperty_IgnoresCustomSerializerWithOptionsInternal<TDictionary, TOverflowItem, TConverter>()
            where TConverter : JsonConverter, new()
            where TDictionary : IDictionary<string, TOverflowItem>
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new TConverter());

            ClassWithExtensionData<TDictionary> obj
                = JsonSerializer.Deserialize<ClassWithExtensionData<TDictionary>>(@"{""TestKey"":""TestValue""}", options);

            Assert.Equal("TestValue", ((JsonElement)(object)obj.Overflow["TestKey"]).GetString());
        }

        [Theory]
        [InlineData(typeof(ClassWithExtensionDataWithAttributedConverter), typeof(Dictionary<string, object>), typeof(object))]
        [InlineData(typeof(ClassWithJsonElementExtensionDataWithAttributedConverter), typeof(Dictionary<string, JsonElement>), typeof(JsonElement))]
        [InlineData(typeof(ClassWithCustomElementExtensionDataWithAttributedConverter), typeof(CustomOverflowDictionary<object>), typeof(object))]
        [InlineData(typeof(ClassWithCustomJsonElementExtensionDataWithAttributedConverter), typeof(CustomOverflowDictionary<JsonElement>), typeof(JsonElement))]
        public void ExtensionProperty_IgnoresCustomSerializerWithExplicitConverter(Type attributedType, Type dictionaryType, Type elementType)
        {
            typeof(ExtensionDataTests)
                .GetMethod(nameof(ExtensionProperty_IgnoresCustomSerializerWithExplicitConverterInternal), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(attributedType, dictionaryType, elementType)
                .Invoke(null, null);
        }

        [Fact]
        public async Task ExtensionProperty_IgnoresCustomSerializerWithExplicitConverter_JsonObject()
        {
            ClassWithExtensionData<JsonObject> obj
                = await Serializer.DeserializeWrapper<ClassWithExtensionData<JsonObject>>(@"{""TestKey"":""TestValue""}");

            Assert.Equal("TestValue", obj.Overflow["TestKey"].GetValue<string>());
        }

        private static void ExtensionProperty_IgnoresCustomSerializerWithExplicitConverterInternal<TRoot, TDictionary, TOverflowItem>()
            where TRoot : IClassWithOverflow<TDictionary>, new()
            where TDictionary : IDictionary<string, TOverflowItem>
        {
            ClassWithExtensionData<TDictionary> obj
                = JsonSerializer.Deserialize<ClassWithExtensionData<TDictionary>>(@"{""TestKey"":""TestValue""}");

            Assert.Equal("TestValue", ((JsonElement)(object)obj.Overflow["TestKey"]).GetString());
        }

        [Fact]
        public async Task ExtensionPropertyObjectValue_Empty()
        {
            ClassWithExtensionPropertyAlreadyInstantiated obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAlreadyInstantiated>(@"{}");
            Assert.Equal(@"{}", await Serializer.SerializeWrapper(obj));
        }

        [Fact]
        public async Task ExtensionPropertyObjectValue_SameAsExtensionPropertyName()
        {
            const string json = @"{""MyOverflow"":{""Key1"":""V""}}";

            // Deserializing directly into the overflow is not supported by design.
            ClassWithExtensionPropertyAsObject obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsObject>(json);

            // The JSON is treated as normal overflow.
            Assert.NotNull(obj.MyOverflow["MyOverflow"]);
            Assert.Equal(json, await Serializer.SerializeWrapper(obj));
        }

        public class ClassWithExtensionPropertyAsObjectAndNameProperty
        {
            public string Name { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }
        }

        public static IEnumerable<object[]> JsonSerializerOptions()
        {
            yield return new object[] { null };
            yield return new object[] { new JsonSerializerOptions() };
            yield return new object[] { new JsonSerializerOptions { UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement } };
            yield return new object[] { new JsonSerializerOptions { UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode } };
        }

        [Theory]
        [MemberData(nameof(JsonSerializerOptions))]
        public async Task ExtensionPropertyDuplicateNames(JsonSerializerOptions options)
        {
            var obj = new ClassWithExtensionPropertyAsObjectAndNameProperty();
            obj.Name = "Name1";

            obj.MyOverflow = new Dictionary<string, object>();
            obj.MyOverflow["Name"] = "Name2";

            string json = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal(@"{""Name"":""Name1"",""Name"":""Name2""}", json);

            // The overflow value comes last in the JSON so it overwrites the original value.
            obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsObjectAndNameProperty>(json, options);
            Assert.Equal("Name2", obj.Name);

            // Since there was no overflow, this should be null.
            Assert.Null(obj.MyOverflow);
        }

        [Theory]
        [MemberData(nameof(JsonSerializerOptions))]
        public async Task Null_SystemObject(JsonSerializerOptions options)
        {
            const string json = @"{""MissingProperty"":null}";

            {
                ClassWithExtensionPropertyAsObject obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsObject>(json, options);

                // A null value maps to <object>, so the value is null.
                object elem = obj.MyOverflow["MissingProperty"];
                Assert.Null(elem);
            }

            {
                ClassWithExtensionPropertyAsJsonObject obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsJsonObject>(json, options);

                JsonObject jObject = obj.MyOverflow;
                JsonNode jNode = jObject["MissingProperty"];
                // Since JsonNode is a reference type the value is null.
                Assert.Null(jNode);
            }
        }

        [Fact]
        public async Task Null_JsonElement()
        {
            const string json = @"{""MissingProperty"":null}";

            ClassWithExtensionPropertyAsJsonElement obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsJsonElement>(json);
            object elem = obj.MyOverflow["MissingProperty"];
            // Since JsonElement is a struct, it treats null as JsonValueKind.Null.
            Assert.IsType<JsonElement>(elem);
            Assert.Equal(JsonValueKind.Null, ((JsonElement)elem).ValueKind);
        }

        [Fact]
        public async Task Null_JsonObject()
        {
            const string json = @"{""MissingProperty"":null}";

            ClassWithExtensionPropertyAsJsonObject obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsJsonObject>(json);
            object elem = obj.MyOverflow["MissingProperty"];
            // Since JsonNode is a reference type the value is null.
            Assert.Null(elem);
        }

        [Fact]
        public async Task ExtensionPropertyObjectValue()
        {
            // Baseline
            ClassWithExtensionPropertyAlreadyInstantiated obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAlreadyInstantiated>(@"{}");
            obj.MyOverflow.Add("test", new object());
            obj.MyOverflow.Add("test1", 1);

            Assert.Equal(@"{""test"":{},""test1"":1}", await Serializer.SerializeWrapper(obj));
        }

        public class DummyObj
        {
            public string Prop { get; set; }
        }

        public struct DummyStruct
        {
            public string Prop { get; set; }
        }

        [Theory]
        [MemberData(nameof(JsonSerializerOptions))]
        public async Task ExtensionPropertyObjectValue_RoundTrip(JsonSerializerOptions options)
        {
            // Baseline
            ClassWithExtensionPropertyAlreadyInstantiated obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAlreadyInstantiated>(@"{}", options);
            obj.MyOverflow.Add("test", new object());
            obj.MyOverflow.Add("test1", 1);
            obj.MyOverflow.Add("test2", "text");
            obj.MyOverflow.Add("test3", new DummyObj() { Prop = "ObjectProp" });
            obj.MyOverflow.Add("test4", new DummyStruct() { Prop = "StructProp" });
            obj.MyOverflow.Add("test5", new Dictionary<string, object>() { { "Key", "Value" }, { "Key1", "Value1" }, });

            string json = await Serializer.SerializeWrapper(obj);
            ClassWithExtensionPropertyAlreadyInstantiated roundTripObj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAlreadyInstantiated>(json, options);

            Assert.Equal(6, roundTripObj.MyOverflow.Count);

            if (options?.UnknownTypeHandling == JsonUnknownTypeHandling.JsonNode)
            {
                Assert.IsAssignableFrom<JsonNode>(roundTripObj.MyOverflow["test"]);
                Assert.IsAssignableFrom<JsonNode>(roundTripObj.MyOverflow["test1"]);
                Assert.IsAssignableFrom<JsonNode>(roundTripObj.MyOverflow["test2"]);
                Assert.IsAssignableFrom<JsonNode>(roundTripObj.MyOverflow["test3"]);

                Assert.IsType<JsonObject>(roundTripObj.MyOverflow["test"]);

                Assert.IsAssignableFrom<JsonValue>(roundTripObj.MyOverflow["test1"]);
                Assert.Equal(1, ((JsonValue)roundTripObj.MyOverflow["test1"]).GetValue<int>());
                Assert.Equal(1, ((JsonValue)roundTripObj.MyOverflow["test1"]).GetValue<long>());

                Assert.IsAssignableFrom<JsonValue>(roundTripObj.MyOverflow["test2"]);
                Assert.Equal("text", ((JsonValue)roundTripObj.MyOverflow["test2"]).GetValue<string>());

                Assert.IsType<JsonObject>(roundTripObj.MyOverflow["test3"]);
                Assert.Equal("ObjectProp", ((JsonObject)roundTripObj.MyOverflow["test3"])["Prop"].GetValue<string>());

                Assert.IsType<JsonObject>(roundTripObj.MyOverflow["test4"]);
                Assert.Equal("StructProp", ((JsonObject)roundTripObj.MyOverflow["test4"])["Prop"].GetValue<string>());

                Assert.IsType<JsonObject>(roundTripObj.MyOverflow["test5"]);
                Assert.Equal("Value", ((JsonObject)roundTripObj.MyOverflow["test5"])["Key"].GetValue<string>());
                Assert.Equal("Value1", ((JsonObject)roundTripObj.MyOverflow["test5"])["Key1"].GetValue<string>());
            }
            else
            {
                Assert.IsType<JsonElement>(roundTripObj.MyOverflow["test"]);
                Assert.IsType<JsonElement>(roundTripObj.MyOverflow["test1"]);
                Assert.IsType<JsonElement>(roundTripObj.MyOverflow["test2"]);
                Assert.IsType<JsonElement>(roundTripObj.MyOverflow["test3"]);

                Assert.Equal(JsonValueKind.Object, ((JsonElement)roundTripObj.MyOverflow["test"]).ValueKind);

                Assert.Equal(JsonValueKind.Number, ((JsonElement)roundTripObj.MyOverflow["test1"]).ValueKind);
                Assert.Equal(1, ((JsonElement)roundTripObj.MyOverflow["test1"]).GetInt32());
                Assert.Equal(1, ((JsonElement)roundTripObj.MyOverflow["test1"]).GetInt64());

                Assert.Equal(JsonValueKind.String, ((JsonElement)roundTripObj.MyOverflow["test2"]).ValueKind);
                Assert.Equal("text", ((JsonElement)roundTripObj.MyOverflow["test2"]).GetString());

                Assert.Equal(JsonValueKind.Object, ((JsonElement)roundTripObj.MyOverflow["test3"]).ValueKind);
                Assert.Equal("ObjectProp", ((JsonElement)roundTripObj.MyOverflow["test3"]).GetProperty("Prop").GetString());

                Assert.Equal(JsonValueKind.Object, ((JsonElement)roundTripObj.MyOverflow["test4"]).ValueKind);
                Assert.Equal("StructProp", ((JsonElement)roundTripObj.MyOverflow["test4"]).GetProperty("Prop").GetString());

                Assert.Equal(JsonValueKind.Object, ((JsonElement)roundTripObj.MyOverflow["test5"]).ValueKind);
                Assert.Equal("Value", ((JsonElement)roundTripObj.MyOverflow["test5"]).GetProperty("Key").GetString());
                Assert.Equal("Value1", ((JsonElement)roundTripObj.MyOverflow["test5"]).GetProperty("Key1").GetString());
            }
        }

        [Fact]
        public async Task DeserializeIntoJsonObjectProperty()
        {
            string json = @"{""MyDict"":{""Property1"":1}}";
            ClassWithExtensionPropertyAsJsonObject obj =
                await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsJsonObject>(json);

            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(1, obj.MyOverflow["MyDict"]["Property1"].GetValue<int>());
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58945")]
#endif

        public async Task DeserializeIntoSystemObjectProperty()
        {
            string json = @"{""MyDict"":{""Property1"":1}}";

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsSystemObject>(json));

            // Cannot deserialize into System.Object overflow even if UnknownTypeHandling is set to use JsonNode.
            var options = new JsonSerializerOptions { UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode };
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsSystemObject>(json));
        }

        public class ClassWithReference
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> MyOverflow { get; set; }

            public ClassWithExtensionProperty MyReference { get; set; }
        }

        [Theory]
        [InlineData(@"{""MyIntMissing"":2,""MyReference"":{""MyIntMissingChild"":3}}")]
        [InlineData(@"{""MyReference"":{""MyIntMissingChild"":3},""MyIntMissing"":2}")]
        [InlineData(@"{""MyReference"":{""MyNestedClass"":null,""MyInt"":0,""MyIntMissingChild"":3},""MyIntMissing"":2}")]
        public async Task NestedClass(string json)
        {
            ClassWithReference obj;

            void Verify()
            {
                Assert.IsType<JsonElement>(obj.MyOverflow["MyIntMissing"]);
                Assert.Equal(1, obj.MyOverflow.Count);
                Assert.Equal(2, obj.MyOverflow["MyIntMissing"].GetInt32());

                ClassWithExtensionProperty child = obj.MyReference;

                Assert.IsType<JsonElement>(child.MyOverflow["MyIntMissingChild"]);
                Assert.IsType<JsonElement>(child.MyOverflow["MyIntMissingChild"]);
                Assert.Equal(1, child.MyOverflow.Count);
                Assert.Equal(3, child.MyOverflow["MyIntMissingChild"].GetInt32());
                Assert.Null(child.MyNestedClass);
                Assert.Equal(0, child.MyInt);
            }

            obj = await Serializer.DeserializeWrapper<ClassWithReference>(json);
            Verify();

            // Round-trip the json and verify.
            json = await Serializer.SerializeWrapper(obj);
            obj = await Serializer.DeserializeWrapper<ClassWithReference>(json);
            Verify();
        }

        public class ParentClassWithObject
        {
            public string Text { get; set; }
            public ChildClassWithObject Child { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
        }

        public class ChildClassWithObject
        {
            public int Number { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
        }

        [Fact]
        public async Task NestedClassWithObjectExtensionDataProperty()
        {
            var child = new ChildClassWithObject { Number = 2 };
            child.ExtensionData.Add("SpecialInformation", "I am child class");

            var parent = new ParentClassWithObject { Text = "Hello World" };
            parent.ExtensionData.Add("SpecialInformation", "I am parent class");
            parent.Child = child;

            // The extension data is based on the raw strings added above and not JsonElement.
            Assert.Equal("Hello World", parent.Text);
            Assert.IsType<string>(parent.ExtensionData["SpecialInformation"]);
            Assert.Equal("I am parent class", (string)parent.ExtensionData["SpecialInformation"]);
            Assert.Equal(2, parent.Child.Number);
            Assert.IsType<string>(parent.Child.ExtensionData["SpecialInformation"]);
            Assert.Equal("I am child class", (string)parent.Child.ExtensionData["SpecialInformation"]);

            // Round-trip and verify. Extension data is now based on JsonElement.
            string json = await Serializer.SerializeWrapper(parent);
            parent = await Serializer.DeserializeWrapper<ParentClassWithObject>(json);

            Assert.Equal("Hello World", parent.Text);
            Assert.IsType<JsonElement>(parent.ExtensionData["SpecialInformation"]);
            Assert.Equal("I am parent class", ((JsonElement)parent.ExtensionData["SpecialInformation"]).GetString());
            Assert.Equal(2, parent.Child.Number);
            Assert.IsType<JsonElement>(parent.Child.ExtensionData["SpecialInformation"]);
            Assert.Equal("I am child class", ((JsonElement)parent.Child.ExtensionData["SpecialInformation"]).GetString());
        }

        public class ParentClassWithJsonElement
        {
            public string Text { get; set; }

            public List<ChildClassWithJsonElement> Children { get; set; } = new List<ChildClassWithJsonElement>();

            [JsonExtensionData]
            // Use SortedDictionary as verification of supporting derived dictionaries.
            public SortedDictionary<string, JsonElement> ExtensionData { get; set; } = new SortedDictionary<string, JsonElement>();
        }

        public class ChildClassWithJsonElement
        {
            public int Number { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; } = new Dictionary<string, JsonElement>();
        }

        [Fact]
        public async Task NestedClassWithJsonElementExtensionDataProperty()
        {
            var child = new ChildClassWithJsonElement { Number = 4 };
            child.ExtensionData.Add("SpecialInformation", JsonDocument.Parse(await Serializer.SerializeWrapper("I am child class")).RootElement);

            var parent = new ParentClassWithJsonElement { Text = "Hello World" };
            parent.ExtensionData.Add("SpecialInformation", JsonDocument.Parse(await Serializer.SerializeWrapper("I am parent class")).RootElement);
            parent.Children.Add(child);

            Verify();

            // Round-trip and verify.
            string json = await Serializer.SerializeWrapper(parent);
            parent = await Serializer.DeserializeWrapper<ParentClassWithJsonElement>(json);
            Verify();

            void Verify()
            {
                Assert.Equal("Hello World", parent.Text);
                Assert.Equal("I am parent class", parent.ExtensionData["SpecialInformation"].GetString());
                Assert.Equal(1, parent.Children.Count);
                Assert.Equal(4, parent.Children[0].Number);
                Assert.Equal("I am child class", parent.Children[0].ExtensionData["SpecialInformation"].GetString());
            }
        }

        [Fact]
        public async Task DeserializeIntoObjectProperty()
        {
            ClassWithExtensionPropertyAsObject obj;
            string json;

            // Baseline dictionary.
            json = @"{""MyDict"":{""Property1"":1}}";
            obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsObject>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(1, ((JsonElement)obj.MyOverflow["MyDict"]).EnumerateObject().First().Value.GetInt32());

            // Attempt to deserialize directly into the overflow property; this is just added as a normal missing property like MyDict above.
            json = @"{""MyOverflow"":{""Property1"":1}}";
            obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsObject>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(1, ((JsonElement)obj.MyOverflow["MyOverflow"]).EnumerateObject().First().Value.GetInt32());

            // Attempt to deserialize null into the overflow property. This is also treated as a missing property.
            json = @"{""MyOverflow"":null}";
            obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsObject>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Null(obj.MyOverflow["MyOverflow"]);

            // Attempt to deserialize object into the overflow property. This is also treated as a missing property.
            json = @"{""MyOverflow"":{}}";
            obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsObject>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(JsonValueKind.Object, ((JsonElement)obj.MyOverflow["MyOverflow"]).ValueKind);
        }

        [Fact]
        public async Task DeserializeIntoMultipleDictionaries()
        {
            ClassWithMultipleDictionaries obj;
            string json;

            // Baseline dictionary.
            json = @"{""ActualDictionary"":{""Key"": {""Property0"":-1}},""MyDict"":{""Property1"":1}}";
            obj = await Serializer.DeserializeWrapper<ClassWithMultipleDictionaries>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(1, ((JsonElement)obj.MyOverflow["MyDict"]).EnumerateObject().First().Value.GetInt32());
            Assert.Equal(1, obj.ActualDictionary.Count);
            Assert.Equal(-1, ((JsonElement)obj.ActualDictionary["Key"]).EnumerateObject().First().Value.GetInt32());

            // Attempt to deserialize null into the dictionary and overflow property. This is also treated as a missing property.
            json = @"{""ActualDictionary"":null,""MyOverflow"":null}";
            obj = await Serializer.DeserializeWrapper<ClassWithMultipleDictionaries>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Null(obj.MyOverflow["MyOverflow"]);
            Assert.Null(obj.ActualDictionary);

            // Attempt to deserialize object into the dictionary and overflow property. This is also treated as a missing property.
            json = @"{""ActualDictionary"":{},""MyOverflow"":{}}";
            obj = await Serializer.DeserializeWrapper<ClassWithMultipleDictionaries>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(JsonValueKind.Object, ((JsonElement)obj.MyOverflow["MyOverflow"]).ValueKind);
            Assert.Equal(0, obj.ActualDictionary.Count);
        }

        [Fact]
        public async Task DeserializeIntoJsonElementProperty()
        {
            ClassWithExtensionPropertyAsJsonElement obj;
            string json;

            // Baseline dictionary.
            json = @"{""MyDict"":{""Property1"":1}}";
            obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsJsonElement>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(1, obj.MyOverflow["MyDict"].EnumerateObject().First().Value.GetInt32());

            // Attempt to deserialize directly into the overflow property; this is just added as a normal missing property like MyDict above.
            json = @"{""MyOverflow"":{""Property1"":1}}";
            obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsJsonElement>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(1, obj.MyOverflow["MyOverflow"].EnumerateObject().First().Value.GetInt32());

            // Attempt to deserialize null into the overflow property. This is also treated as a missing property.
            json = @"{""MyOverflow"":null}";
            obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsJsonElement>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(JsonValueKind.Null, obj.MyOverflow["MyOverflow"].ValueKind);

            // Attempt to deserialize object into the overflow property. This is also treated as a missing property.
            json = @"{""MyOverflow"":{}}";
            obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsJsonElement>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(JsonValueKind.Object, obj.MyOverflow["MyOverflow"].ValueKind);
        }

        [Fact]
        public async Task SerializerOutputRoundtripsWhenEscaping()
        {
            string jsonString = "{\"\u6C49\u5B57\":\"abc\",\"Class\":{\"\u6F22\u5B57\":\"xyz\"},\"\u62DC\u6258\":{\"\u62DC\u6258\u62DC\u6258\":1}}";

            ClassWithEscapedProperty input = await Serializer.DeserializeWrapper<ClassWithEscapedProperty>(jsonString);

            Assert.Equal("abc", input.\u6C49\u5B57);
            Assert.Equal("xyz", input.Class.\u6F22\u5B57);

            string normalizedString = await Serializer.SerializeWrapper(input);

            Assert.Equal(normalizedString, await Serializer.SerializeWrapper(await Serializer.DeserializeWrapper<ClassWithEscapedProperty>(normalizedString)));
        }

        public class ClassWithEscapedProperty
        {
            public string \u6C49\u5B57 { get; set; }
            public NestedClassWithEscapedProperty Class { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> Overflow { get; set; }
        }

        public class NestedClassWithEscapedProperty
        {
            public string \u6F22\u5B57 { get; set; }
        }

        public class ClassWithInvalidExtensionPropertyStringString
        {
            [JsonExtensionData]
            public Dictionary<string, string> MyOverflow { get; set; }
        }

        public class ClassWithInvalidExtensionPropertyObjectString
        {
            [JsonExtensionData]
            public Dictionary<DummyObj, string> MyOverflow { get; set; }
        }

        public class ClassWithInvalidExtensionPropertyStringJsonNode
        {
            [JsonExtensionData]
            public Dictionary<string, JsonNode> MyOverflow { get; set; }
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58945")]
#endif
        public async Task ExtensionProperty_InvalidDictionary()
        {
            var obj1 = new ClassWithInvalidExtensionPropertyStringString();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(obj1));

            var obj2 = new ClassWithInvalidExtensionPropertyObjectString();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(obj2));

            var obj3 = new ClassWithInvalidExtensionPropertyStringJsonNode();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(obj3));
        }

        public class ClassWithExtensionPropertyAlreadyInstantiated
        {
            public ClassWithExtensionPropertyAlreadyInstantiated()
            {
                MyOverflow = new Dictionary<string, object>();
            }

            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }
        }

        public class ClassWithExtensionPropertyAsObject
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }
        }

        public class ClassWithExtensionPropertyAsJsonElement
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> MyOverflow { get; set; }
        }

        public class ClassWithExtensionPropertyAsJsonObject
        {
            [JsonExtensionData]
            public JsonObject MyOverflow { get; set; }
        }

        public class ClassWithExtensionPropertyAsSystemObject
        {
            [JsonExtensionData]
            public object MyOverflow { get; set; }
        }

        public class ClassWithMultipleDictionaries
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }

            public Dictionary<string, object> ActualDictionary { get; set; }
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("https://github.com/dotnet/runtime/issues/58945")]
#endif
        public async Task DeserializeIntoImmutableDictionaryProperty()
        {
            // baseline
            await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsImmutable>(@"{}");
            await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsImmutableJsonElement>(@"{}");
            await Serializer.DeserializeWrapper<ClassWithExtensionPropertyPrivateConstructor>(@"{}");
            await Serializer.DeserializeWrapper<ClassWithExtensionPropertyPrivateConstructorJsonElement>(@"{}");

            await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsImmutable>("{\"hello\":\"world\"}"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsImmutableJsonElement>("{\"hello\":\"world\"}"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<ClassWithExtensionPropertyPrivateConstructor>("{\"hello\":\"world\"}"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<ClassWithExtensionPropertyPrivateConstructorJsonElement>("{\"hello\":\"world\"}"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ClassWithExtensionPropertyCustomIImmutable>("{\"hello\":\"world\"}"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ClassWithExtensionPropertyCustomIImmutableJsonElement>("{\"hello\":\"world\"}"));
        }

        [Fact]
        public async Task SerializeIntoImmutableDictionaryProperty()
        {
            // attempt to serialize a null immutable dictionary
            string expectedJson = "{}";
            var obj = new ClassWithExtensionPropertyAsImmutable();
            var json = await Serializer.SerializeWrapper(obj);
            Assert.Equal(expectedJson, json);

            // attempt to serialize an empty immutable dictionary
            expectedJson = "{}";
            obj = new ClassWithExtensionPropertyAsImmutable();
            obj.MyOverflow = ImmutableDictionary<string, object>.Empty;
            json = await Serializer.SerializeWrapper(obj);
            Assert.Equal(expectedJson, json);

            // attempt to serialize a populated immutable dictionary
            expectedJson = "{\"hello\":\"world\"}";
            obj = new ClassWithExtensionPropertyAsImmutable();
            var dictionaryStringObject = new Dictionary<string, object> { { "hello", "world" } };
            obj.MyOverflow = ImmutableDictionary.CreateRange(dictionaryStringObject);
            json = await Serializer.SerializeWrapper(obj);
            Assert.Equal(expectedJson, json);
        }

        public class ClassWithExtensionPropertyAsImmutable
        {
            [JsonExtensionData]
            public ImmutableDictionary<string, object> MyOverflow { get; set; }
        }

        public class ClassWithExtensionPropertyAsImmutableJsonElement
        {
            [JsonExtensionData]
            public ImmutableDictionary<string, JsonElement> MyOverflow { get; set; }
        }

        public class ClassWithExtensionPropertyPrivateConstructor
        {
            [JsonExtensionData]
            public GenericIDictionaryWrapperPrivateConstructor<string, object> MyOverflow { get; set; }
        }

        public class ClassWithExtensionPropertyPrivateConstructorJsonElement
        {
            [JsonExtensionData]
            public GenericIDictionaryWrapperPrivateConstructor<string, JsonElement> MyOverflow { get; set; }
        }

        public class ClassWithExtensionPropertyCustomIImmutable
        {
            [JsonExtensionData]
            public GenericIImmutableDictionaryWrapper<string, object> MyOverflow { get; set; }
        }

        public class ClassWithExtensionPropertyCustomIImmutableJsonElement
        {
            [JsonExtensionData]
            public GenericIImmutableDictionaryWrapper<string, JsonElement> MyOverflow { get; set; }
        }

        [Theory]
        [InlineData(typeof(ClassWithExtensionPropertyNoGenericParameters))]
        [InlineData(typeof(ClassWithExtensionPropertyOneGenericParameter))]
        [InlineData(typeof(ClassWithExtensionPropertyThreeGenericParameters))]
        public async Task DeserializeIntoGenericDictionaryParameterCount(Type type)
        {
            object obj = await Serializer.DeserializeWrapper("{\"hello\":\"world\"}", type);

            IDictionary<string, object> extData = (IDictionary<string, object>)type.GetProperty("MyOverflow").GetValue(obj)!;
            Assert.Equal("world", ((JsonElement)extData["hello"]).GetString());
        }

        public class ClassWithExtensionPropertyNoGenericParameters
        {
            [JsonExtensionData]
            public StringToObjectIDictionaryWrapper MyOverflow { get; set; }
        }

        public class ClassWithExtensionPropertyOneGenericParameter
        {
            [JsonExtensionData]
            public StringToGenericIDictionaryWrapper<object> MyOverflow { get; set; }
        }

        public class ClassWithExtensionPropertyThreeGenericParameters
        {
            [JsonExtensionData]
            public GenericIDictonaryWrapperThreeGenericParameters<string, object, string> MyOverflow { get; set; }
        }

        [Fact]
        public async Task CustomObjectConverterInExtensionProperty()
        {
            const string Json = "{\"hello\": \"world\"}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ObjectConverter());

            ClassWithExtensionPropertyAsObject obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsObject>(Json, options);
            object overflowProp = obj.MyOverflow["hello"];
            Assert.IsType<string>(overflowProp);
            Assert.Equal("world!!!", ((string)overflowProp));

            string newJson = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("{\"hello\":\"world!!!\"}", newJson);
        }

        public class ObjectConverter : JsonConverter<object>
        {
            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.GetString() + "!!!";
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                // Since we are in a user-provided (not internal to S.T.Json) object converter,
                // this converter will be called, not the internal string converter.
                writer.WriteStringValue((string)value);
            }
        }

        [Fact]
        public async Task CustomJsonElementConverterInExtensionProperty()
        {
            const string Json = "{\"hello\": \"world\"}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonElementConverter());

            ClassWithExtensionPropertyAsJsonElement obj = await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsJsonElement>(Json, options);
            JsonElement overflowProp = obj.MyOverflow["hello"];
            Assert.Equal(JsonValueKind.Undefined, overflowProp.ValueKind);

            string newJson = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("{\"hello\":{\"Hi\":\"There\"}}", newJson);
        }

        public class JsonElementConverter : JsonConverter<JsonElement>
        {
            public override JsonElement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                // Just return an empty JsonElement.
                reader.Skip();
                return new JsonElement();
            }

            public override void Write(Utf8JsonWriter writer, JsonElement value, JsonSerializerOptions options)
            {
                // Write a string we can test against easily.
                writer.WriteStartObject();
                writer.WriteString("Hi", "There");
                writer.WriteEndObject();
            }
        }

        [Fact]
        public async Task CustomJsonObjectConverterInExtensionProperty()
        {
            const string Json = "{\"hello\": \"world\"}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonObjectConverter());

            // A custom converter for JsonObject is not allowed on an extension property.
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Serializer.DeserializeWrapper<ClassWithExtensionPropertyAsJsonObject>(Json, options));

            Assert.Contains("JsonObject", ex.ToString());
        }

        public class JsonObjectConverter : JsonConverter<JsonObject>
        {
            public override JsonObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                // Just return an empty JsonElement.
                reader.Skip();
                return new JsonObject();
            }

            public override void Write(Utf8JsonWriter writer, JsonObject value, JsonSerializerOptions options)
            {
                // Write a string we can test against easily.
                writer.WriteStartObject();
                writer.WriteString("Hi", "There");
                writer.WriteEndObject();
            }
        }

        [Fact]
        public async Task EmptyPropertyAndExtensionData_PropertyFirst()
        {
            // Verify any caching treats real property (with empty name) differently than a missing property.

            ClassWithEmptyPropertyNameAndExtensionProperty obj;

            // Create a new options instances to re-set any caches.
            JsonSerializerOptions options = new JsonSerializerOptions();

            // First use an empty property.
            string json = @"{"""":43}";
            obj = await Serializer.DeserializeWrapper<ClassWithEmptyPropertyNameAndExtensionProperty>(json, options);
            Assert.Equal(43, obj.MyInt1);
            Assert.Null(obj.MyOverflow);

            // Then populate cache with a missing property name.
            json = @"{""DoesNotExist"":42}";
            obj = await Serializer.DeserializeWrapper<ClassWithEmptyPropertyNameAndExtensionProperty>(json, options);
            Assert.Equal(0, obj.MyInt1);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(42, obj.MyOverflow["DoesNotExist"].GetInt32());
        }

        [Fact]
        public async Task EmptyPropertyNameAndExtensionData_ExtDataFirst()
        {
            // Verify any caching treats real property (with empty name) differently than a missing property.

            ClassWithEmptyPropertyNameAndExtensionProperty obj;

            // Create a new options instances to re-set any caches.
            JsonSerializerOptions options = new JsonSerializerOptions();

            // First populate cache with a missing property name.
            string json = @"{""DoesNotExist"":42}";
            obj = await Serializer.DeserializeWrapper<ClassWithEmptyPropertyNameAndExtensionProperty>(json, options);
            Assert.Equal(0, obj.MyInt1);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(42, obj.MyOverflow["DoesNotExist"].GetInt32());

            // Then use an empty property.
            json = @"{"""":43}";
            obj = await Serializer.DeserializeWrapper<ClassWithEmptyPropertyNameAndExtensionProperty>(json, options);
            Assert.Equal(43, obj.MyInt1);
            Assert.Null(obj.MyOverflow);
        }

        [Fact]
        public async Task ExtensionDataDictionarySerialize_DoesNotHonor()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            EmptyClassWithExtensionProperty obj = await Serializer.DeserializeWrapper<EmptyClassWithExtensionProperty>(@"{""Key1"": 1}", options);

            // Ignore naming policy for extension data properties by default.
            Assert.False(obj.MyOverflow.ContainsKey("key1"));
            Assert.Equal(1, obj.MyOverflow["Key1"].GetInt32());
        }

        [Theory]
        [InlineData(0x1, 'v')]
        [InlineData(0x1, '\u0467')]
        [InlineData(0x10, 'v')]
        [InlineData(0x10, '\u0467')]
        [InlineData(0x100, 'v')]
        [InlineData(0x100, '\u0467')]
        [InlineData(0x1000, 'v')]
        [InlineData(0x1000, '\u0467')]
        [InlineData(0x10000, 'v')]
        [InlineData(0x10000, '\u0467')]
        public async Task LongPropertyNames(int propertyLength, char ch)
        {
            // Although the CLR may limit member length to 1023 bytes, the serializer doesn't have a hard limit.

            string val = new string(ch, propertyLength);
            string json = @"{""" + val + @""":1}";

            EmptyClassWithExtensionProperty obj = await Serializer.DeserializeWrapper<EmptyClassWithExtensionProperty>(json);

            Assert.True(obj.MyOverflow.ContainsKey(val));

            var options = new JsonSerializerOptions
            {
                // Avoid escaping '\u0467'.
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string jsonRoundTripped = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal(json, jsonRoundTripped);
        }

        public class EmptyClassWithExtensionProperty
        {
            [JsonExtensionData]
            public IDictionary<string, JsonElement> MyOverflow { get; set; }
        }

        public class ClassWithEmptyPropertyNameAndExtensionProperty
        {
            [JsonPropertyName("")]
            public int MyInt1 { get; set; }

            [JsonExtensionData]
            public IDictionary<string, JsonElement> MyOverflow { get; set; }
        }
    }
}
