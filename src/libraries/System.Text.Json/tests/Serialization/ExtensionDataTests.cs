// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class ExtensionDataTests
    {
        [Fact]
        public static void ExtensionPropertyNotUsed()
        {
            string json = @"{""MyNestedClass"":" + SimpleTestClass.s_json + "}";
            ClassWithExtensionProperty obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(json);
            Assert.Null(obj.MyOverflow);
        }

        [Fact]
        public static void ExtensionPropertyRoundTrip()
        {
            ClassWithExtensionProperty obj;

            {
                string json = @"{""MyIntMissing"":2, ""MyInt"":1, ""MyNestedClassMissing"":" + SimpleTestClass.s_json + "}";
                obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(json);
                Verify();
            }

            // Round-trip the json.
            {
                string json = JsonSerializer.Serialize(obj);
                obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(json);
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
        public static void ExtensionFieldNotUsed()
        {
            string json = @"{""MyNestedClass"":" + SimpleTestClass.s_json + "}";
            ClassWithExtensionField obj = JsonSerializer.Deserialize<ClassWithExtensionField>(json);
            Assert.Null(obj.MyOverflow);
        }

        [Fact]
        public static void ExtensionFieldRoundTrip()
        {
            ClassWithExtensionField obj;

            {
                string json = @"{""MyIntMissing"":2, ""MyInt"":1, ""MyNestedClassMissing"":" + SimpleTestClass.s_json + "}";
                obj = JsonSerializer.Deserialize<ClassWithExtensionField>(json);
                Verify();
            }

            // Round-trip the json.
            {
                string json = JsonSerializer.Serialize(obj);
                obj = JsonSerializer.Deserialize<ClassWithExtensionField>(json);
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
        public static void ExtensionPropertyIgnoredWhenWritingDefault()
        {
            string expected = @"{}";
            string actual = JsonSerializer.Serialize(new ClassWithExtensionPropertyAsObject());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void MultipleExtensionPropertyIgnoredWhenWritingDefault()
        {
            var obj = new ClassWithMultipleDictionaries();
            string actual = JsonSerializer.Serialize(obj);
            Assert.Equal("{\"ActualDictionary\":null}", actual);

            obj = new ClassWithMultipleDictionaries
            {
                ActualDictionary = new Dictionary<string, object>()
            };
            actual = JsonSerializer.Serialize(obj);
            Assert.Equal("{\"ActualDictionary\":{}}", actual);

            obj = new ClassWithMultipleDictionaries
            {
                MyOverflow = new Dictionary<string, object>
                {
                    { "test", "value" }
                }
            };
            actual = JsonSerializer.Serialize(obj);
            Assert.Equal("{\"ActualDictionary\":null,\"test\":\"value\"}", actual);

            obj = new ClassWithMultipleDictionaries
            {
                ActualDictionary = new Dictionary<string, object>(),
                MyOverflow = new Dictionary<string, object>
                {
                    { "test", "value" }
                }
            };
            actual = JsonSerializer.Serialize(obj);
            Assert.Equal("{\"ActualDictionary\":{},\"test\":\"value\"}", actual);
        }

        [Fact]
        public static void ExtensionPropertyInvalidJsonFail()
        {
            const string BadJson = @"{""Good"":""OK"",""Bad"":!}";

            JsonException jsonException = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(BadJson));
            Assert.Contains("Path: $.Bad | LineNumber: 0 | BytePositionInLine: 19.", jsonException.ToString());
            Assert.NotNull(jsonException.InnerException);
            Assert.IsAssignableFrom<JsonException>(jsonException.InnerException);
            Assert.Contains("!", jsonException.InnerException.ToString());
        }

        [Fact]
        public static void ExtensionPropertyAlreadyInstantiated()
        {
            Assert.NotNull(new ClassWithExtensionPropertyAlreadyInstantiated().MyOverflow);

            string json = @"{""MyIntMissing"":2}";

            ClassWithExtensionProperty obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(json);
            Assert.Equal(2, obj.MyOverflow["MyIntMissing"].GetInt32());
        }

        [Fact]
        public static void ExtensionPropertyAsObject()
        {
            string json = @"{""MyIntMissing"":2}";

            ClassWithExtensionPropertyAsObject obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(json);
            Assert.IsType<JsonElement>(obj.MyOverflow["MyIntMissing"]);
            Assert.Equal(2, ((JsonElement)obj.MyOverflow["MyIntMissing"]).GetInt32());
        }

        [Fact]
        public static void ExtensionPropertyCamelCasing()
        {
            // Currently we apply no naming policy. If we do (such as a ExtensionPropertyNamingPolicy), we'd also have to add functionality to the JsonDocument.

            ClassWithExtensionProperty obj;
            const string jsonWithProperty = @"{""MyIntMissing"":1}";
            const string jsonWithPropertyCamelCased = @"{""myIntMissing"":1}";

            {
                // Baseline Pascal-cased json + no casing option.
                obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(jsonWithProperty);
                Assert.Equal(1, obj.MyOverflow["MyIntMissing"].GetInt32());
                string json = JsonSerializer.Serialize(obj);
                Assert.Contains(@"""MyIntMissing"":1", json);
            }

            {
                // Pascal-cased json + camel casing option.
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(jsonWithProperty, options);
                Assert.Equal(1, obj.MyOverflow["MyIntMissing"].GetInt32());
                string json = JsonSerializer.Serialize(obj, options);
                Assert.Contains(@"""MyIntMissing"":1", json);
            }

            {
                // Baseline camel-cased json + no casing option.
                obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(jsonWithPropertyCamelCased);
                Assert.Equal(1, obj.MyOverflow["myIntMissing"].GetInt32());
                string json = JsonSerializer.Serialize(obj);
                Assert.Contains(@"""myIntMissing"":1", json);
            }

            {
                // Baseline camel-cased json + camel casing option.
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(jsonWithPropertyCamelCased, options);
                Assert.Equal(1, obj.MyOverflow["myIntMissing"].GetInt32());
                string json = JsonSerializer.Serialize(obj, options);
                Assert.Contains(@"""myIntMissing"":1", json);
            }
        }

        [Fact]
        public static void NullValuesIgnored()
        {
            const string json = @"{""MyNestedClass"":null}";
            const string jsonMissing = @"{""MyNestedClassMissing"":null}";

            {
                // Baseline with no missing.
                ClassWithExtensionProperty obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(json);
                Assert.Null(obj.MyOverflow);

                string outJson = JsonSerializer.Serialize(obj);
                Assert.Contains(@"""MyNestedClass"":null", outJson);
            }

            {
                // Baseline with missing.
                ClassWithExtensionProperty obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(jsonMissing);
                Assert.Equal(1, obj.MyOverflow.Count);
                Assert.Equal(JsonValueKind.Null, obj.MyOverflow["MyNestedClassMissing"].ValueKind);
            }

            {
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.IgnoreNullValues = true;

                ClassWithExtensionProperty obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(jsonMissing, options);

                // Currently we do not ignore nulls in the extension data. The JsonDocument would also need to support this mode
                // for any lower-level nulls.
                Assert.Equal(1, obj.MyOverflow.Count);
                Assert.Equal(JsonValueKind.Null, obj.MyOverflow["MyNestedClassMissing"].ValueKind);
            }
        }

        private class ClassWithInvalidExtensionProperty
        {
            [JsonExtensionData]
            public Dictionary<string, int> MyOverflow { get; set; }
        }

        private class ClassWithTwoExtensionProperties
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow1 { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow2 { get; set; }
        }

        [Fact]
        public static void InvalidExtensionPropertyFail()
        {
            // Baseline
            JsonSerializer.Deserialize<ClassWithExtensionProperty>(@"{}");
            JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(@"{}");

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<ClassWithInvalidExtensionProperty>(@"{}"));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<ClassWithTwoExtensionProperties>(@"{}"));
        }

        private class ClassWithIgnoredData
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }

            [JsonIgnore]
            public int MyInt { get; set; }
        }

        [Fact]
        public static void IgnoredDataShouldNotBeExtensionData()
        {
            ClassWithIgnoredData obj = JsonSerializer.Deserialize<ClassWithIgnoredData>(@"{""MyInt"":1}");

            Assert.Equal(0, obj.MyInt);
            Assert.Null(obj.MyOverflow);
        }

        private class ClassWithExtensionData<T>
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
        public static void ExtensionProperty_SupportsWritingToCustomSerializerWithOptions(Type overflowType, Type converterType)
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

        private class ClassWithExtensionDataWithAttributedConverter : IClassWithOverflow<Dictionary<string, object>>
        {
            [JsonExtensionData]
            [JsonConverter(typeof(DictionaryOverflowConverter))]
            public Dictionary<string, object> Overflow { get; set; }
        }

        private class ClassWithJsonElementExtensionDataWithAttributedConverter : IClassWithOverflow<Dictionary<string, JsonElement>>
        {
            [JsonExtensionData]
            [JsonConverter(typeof(JsonElementOverflowConverter))]
            public Dictionary<string, JsonElement> Overflow { get; set; }
        }

        private class ClassWithCustomElementExtensionDataWithAttributedConverter : IClassWithOverflow<CustomOverflowDictionary<object>>
        {
            [JsonExtensionData]
            [JsonConverter(typeof(CustomObjectDictionaryOverflowConverter))]
            public CustomOverflowDictionary<object> Overflow { get; set; }
        }

        private class ClassWithCustomJsonElementExtensionDataWithAttributedConverter : IClassWithOverflow<CustomOverflowDictionary<JsonElement>>
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
        public static void ExtensionProperty_SupportsWritingToCustomSerializerWithExplicitConverter(Type attributedType, Type dictionaryType)
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
        public static void ExtensionProperty_IgnoresCustomSerializerWithOptions(Type overflowType, Type converterType, Type elementType)
        {
            typeof(ExtensionDataTests)
                .GetMethod(nameof(ExtensionProperty_IgnoresCustomSerializerWithOptionsInternal), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(overflowType, elementType, converterType)
                .Invoke(null, null);
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
        public static void ExtensionProperty_IgnoresCustomSerializerWithExplicitConverter(Type attributedType, Type dictionaryType, Type elementType)
        {
            typeof(ExtensionDataTests)
                .GetMethod(nameof(ExtensionProperty_IgnoresCustomSerializerWithExplicitConverterInternal), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(attributedType, dictionaryType, elementType)
                .Invoke(null, null);
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
        public static void ExtensionPropertyObjectValue_Empty()
        {
            ClassWithExtensionPropertyAlreadyInstantiated obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAlreadyInstantiated>(@"{}");
            Assert.Equal(@"{}", JsonSerializer.Serialize(obj));
        }

        [Fact]
        public static void ExtensionPropertyObjectValue_SameAsExtensionPropertyName()
        {
            const string json = @"{""MyOverflow"":{""Key1"":""V""}}";

            // Deserializing directly into the overflow is not supported by design.
            ClassWithExtensionPropertyAsObject obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(json);

            // The JSON is treated as normal overflow.
            Assert.NotNull(obj.MyOverflow["MyOverflow"]);
            Assert.Equal(json, JsonSerializer.Serialize(obj));
        }

        private class ClassWithExtensionPropertyAsObjectAndNameProperty
        {
            public string Name { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }
        }

        [Fact]
        public static void ExtensionPropertyDuplicateNames()
        {
            var obj = new ClassWithExtensionPropertyAsObjectAndNameProperty();
            obj.Name = "Name1";

            obj.MyOverflow = new Dictionary<string, object>();
            obj.MyOverflow["Name"] = "Name2";

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{""Name"":""Name1"",""Name"":""Name2""}", json);

            // The overflow value comes last in the JSOn so it overwrites the original value.
            obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObjectAndNameProperty>(json);
            Assert.Equal("Name2", obj.Name);

            // Since there was no overflow, this should be null.
            Assert.Null(obj.MyOverflow);
        }

        [Fact]
        public static void NullAsNullObjectOrJsonValueKindNull()
        {
            const string json = @"{""MissingProperty"":null}";

            {
                ClassWithExtensionPropertyAsObject obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(json);

                // A null value maps to <object>, so the value is null.
                object elem = obj.MyOverflow["MissingProperty"];
                Assert.Null(elem);
            }

            {
                ClassWithExtensionPropertyAsJsonElement obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsJsonElement>(json);

                // Since JsonElement is a struct, it treats null as JsonValueKind.Null.
                object elem = obj.MyOverflow["MissingProperty"];
                Assert.IsType<JsonElement>(elem);
                Assert.Equal(JsonValueKind.Null, ((JsonElement)elem).ValueKind);
            }
        }

        [Fact]
        public static void ExtensionPropertyObjectValue()
        {
            // Baseline
            ClassWithExtensionPropertyAlreadyInstantiated obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAlreadyInstantiated>(@"{}");
            obj.MyOverflow.Add("test", new object());
            obj.MyOverflow.Add("test1", 1);

            Assert.Equal(@"{""test"":{},""test1"":1}", JsonSerializer.Serialize(obj));
        }

        private class DummyObj
        {
            public string Prop { get; set; }
        }

        private struct DummyStruct
        {
            public string Prop { get; set; }
        }

        [Fact]
        public static void ExtensionPropertyObjectValue_RoundTrip()
        {
            // Baseline
            ClassWithExtensionPropertyAlreadyInstantiated obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAlreadyInstantiated>(@"{}");
            obj.MyOverflow.Add("test", new object());
            obj.MyOverflow.Add("test1", 1);
            obj.MyOverflow.Add("test2", "text");
            obj.MyOverflow.Add("test3", new DummyObj() { Prop = "ObjectProp" });
            obj.MyOverflow.Add("test4", new DummyStruct() { Prop = "StructProp" });
            obj.MyOverflow.Add("test5", new Dictionary<string, object>() { { "Key", "Value" }, { "Key1", "Value1" }, });

            string json = JsonSerializer.Serialize(obj);
            ClassWithExtensionPropertyAlreadyInstantiated roundTripObj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAlreadyInstantiated>(json);

            Assert.Equal(6, roundTripObj.MyOverflow.Count);

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

        private class ClassWithReference
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> MyOverflow { get; set; }

            public ClassWithExtensionProperty MyReference { get; set; }
        }

        [Theory]
        [InlineData(@"{""MyIntMissing"":2,""MyReference"":{""MyIntMissingChild"":3}}")]
        [InlineData(@"{""MyReference"":{""MyIntMissingChild"":3},""MyIntMissing"":2}")]
        [InlineData(@"{""MyReference"":{""MyNestedClass"":null,""MyInt"":0,""MyIntMissingChild"":3},""MyIntMissing"":2}")]
        public static void NestedClass(string json)
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

            obj = JsonSerializer.Deserialize<ClassWithReference>(json);
            Verify();

            // Round-trip the json and verify.
            json = JsonSerializer.Serialize(obj);
            obj = JsonSerializer.Deserialize<ClassWithReference>(json);
            Verify();
        }

        private class ParentClassWithObject
        {
            public string Text { get; set; }
            public ChildClassWithObject Child { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
        }

        private class ChildClassWithObject
        {
            public int Number { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();
        }

        [Fact]
        public static void NestedClassWithObjectExtensionDataProperty()
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
            string json = JsonSerializer.Serialize(parent);
            parent = JsonSerializer.Deserialize<ParentClassWithObject>(json);

            Assert.Equal("Hello World", parent.Text);
            Assert.IsType<JsonElement>(parent.ExtensionData["SpecialInformation"]);
            Assert.Equal("I am parent class", ((JsonElement)parent.ExtensionData["SpecialInformation"]).GetString());
            Assert.Equal(2, parent.Child.Number);
            Assert.IsType<JsonElement>(parent.Child.ExtensionData["SpecialInformation"]);
            Assert.Equal("I am child class", ((JsonElement)parent.Child.ExtensionData["SpecialInformation"]).GetString());
        }

        private class ParentClassWithJsonElement
        {
            public string Text { get; set; }

            public List<ChildClassWithJsonElement> Children { get; set; } = new List<ChildClassWithJsonElement>();

            [JsonExtensionData]
            // Use SortedDictionary as verification of supporting derived dictionaries.
            public SortedDictionary<string, JsonElement> ExtensionData { get; set; } = new SortedDictionary<string, JsonElement>();
        }

        private class ChildClassWithJsonElement
        {
            public int Number { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; } = new Dictionary<string, JsonElement>();
        }

        [Fact]
        public static void NestedClassWithJsonElementExtensionDataProperty()
        {
            var child = new ChildClassWithJsonElement { Number = 4 };
            child.ExtensionData.Add("SpecialInformation", JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes("I am child class")).RootElement);

            var parent = new ParentClassWithJsonElement { Text = "Hello World" };
            parent.ExtensionData.Add("SpecialInformation", JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes("I am parent class")).RootElement);
            parent.Children.Add(child);

            Verify();

            // Round-trip and verify.
            string json = JsonSerializer.Serialize(parent);
            parent = JsonSerializer.Deserialize<ParentClassWithJsonElement>(json);
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
        public static void DeserializeIntoObjectProperty()
        {
            ClassWithExtensionPropertyAsObject obj;
            string json;

            // Baseline dictionary.
            json = @"{""MyDict"":{""Property1"":1}}";
            obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(1, ((JsonElement)obj.MyOverflow["MyDict"]).EnumerateObject().First().Value.GetInt32());

            // Attempt to deserialize directly into the overflow property; this is just added as a normal missing property like MyDict above.
            json = @"{""MyOverflow"":{""Property1"":1}}";
            obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(1, ((JsonElement)obj.MyOverflow["MyOverflow"]).EnumerateObject().First().Value.GetInt32());

            // Attempt to deserialize null into the overflow property. This is also treated as a missing property.
            json = @"{""MyOverflow"":null}";
            obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Null(obj.MyOverflow["MyOverflow"]);

            // Attempt to deserialize object into the overflow property. This is also treated as a missing property.
            json = @"{""MyOverflow"":{}}";
            obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(JsonValueKind.Object, ((JsonElement)obj.MyOverflow["MyOverflow"]).ValueKind);
        }

        [Fact]
        public static void DeserializeIntoMultipleDictionaries()
        {
            ClassWithMultipleDictionaries obj;
            string json;

            // Baseline dictionary.
            json = @"{""ActualDictionary"":{""Key"": {""Property0"":-1}},""MyDict"":{""Property1"":1}}";
            obj = JsonSerializer.Deserialize<ClassWithMultipleDictionaries>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(1, ((JsonElement)obj.MyOverflow["MyDict"]).EnumerateObject().First().Value.GetInt32());
            Assert.Equal(1, obj.ActualDictionary.Count);
            Assert.Equal(-1, ((JsonElement)obj.ActualDictionary["Key"]).EnumerateObject().First().Value.GetInt32());

            // Attempt to deserialize null into the dictionary and overflow property. This is also treated as a missing property.
            json = @"{""ActualDictionary"":null,""MyOverflow"":null}";
            obj = JsonSerializer.Deserialize<ClassWithMultipleDictionaries>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Null(obj.MyOverflow["MyOverflow"]);
            Assert.Null(obj.ActualDictionary);

            // Attempt to deserialize object into the dictionary and overflow property. This is also treated as a missing property.
            json = @"{""ActualDictionary"":{},""MyOverflow"":{}}";
            obj = JsonSerializer.Deserialize<ClassWithMultipleDictionaries>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(JsonValueKind.Object, ((JsonElement)obj.MyOverflow["MyOverflow"]).ValueKind);
            Assert.Equal(0, obj.ActualDictionary.Count);
        }

        [Fact]
        public static void DeserializeIntoJsonElementProperty()
        {
            ClassWithExtensionPropertyAsJsonElement obj;
            string json;

            // Baseline dictionary.
            json = @"{""MyDict"":{""Property1"":1}}";
            obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsJsonElement>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(1, obj.MyOverflow["MyDict"].EnumerateObject().First().Value.GetInt32());

            // Attempt to deserialize directly into the overflow property; this is just added as a normal missing property like MyDict above.
            json = @"{""MyOverflow"":{""Property1"":1}}";
            obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsJsonElement>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(1, obj.MyOverflow["MyOverflow"].EnumerateObject().First().Value.GetInt32());

            // Attempt to deserialize null into the overflow property. This is also treated as a missing property.
            json = @"{""MyOverflow"":null}";
            obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsJsonElement>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(JsonValueKind.Null, obj.MyOverflow["MyOverflow"].ValueKind);

            // Attempt to deserialize object into the overflow property. This is also treated as a missing property.
            json = @"{""MyOverflow"":{}}";
            obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsJsonElement>(json);
            Assert.Equal(1, obj.MyOverflow.Count);
            Assert.Equal(JsonValueKind.Object, obj.MyOverflow["MyOverflow"].ValueKind);
        }

        [Fact]
        public static void SerializerOutputRoundtripsWhenEscaping()
        {
            string jsonString = "{\"\u6C49\u5B57\":\"abc\",\"Class\":{\"\u6F22\u5B57\":\"xyz\"},\"\u62DC\u6258\":{\"\u62DC\u6258\u62DC\u6258\":1}}";

            ClassWithEscapedProperty input = JsonSerializer.Deserialize<ClassWithEscapedProperty>(jsonString);

            Assert.Equal("abc", input.\u6C49\u5B57);
            Assert.Equal("xyz", input.Class.\u6F22\u5B57);

            string normalizedString = JsonSerializer.Serialize(input);

            Assert.Equal(normalizedString, JsonSerializer.Serialize(JsonSerializer.Deserialize<ClassWithEscapedProperty>(normalizedString)));
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

        private class ClassWithInvalidExtensionPropertyStringString
        {
            [JsonExtensionData]
            public Dictionary<string, string> MyOverflow { get; set; }
        }

        private class ClassWithInvalidExtensionPropertyObjectString
        {
            [JsonExtensionData]
            public Dictionary<DummyObj, string> MyOverflow { get; set; }
        }

        [Fact]
        public static void ExtensionProperty_InvalidDictionary()
        {
            ClassWithInvalidExtensionPropertyStringString obj1 = new ClassWithInvalidExtensionPropertyStringString();
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(obj1));

            ClassWithInvalidExtensionPropertyObjectString obj2 = new ClassWithInvalidExtensionPropertyObjectString();
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(obj2));
        }

        private class ClassWithExtensionPropertyAlreadyInstantiated
        {
            public ClassWithExtensionPropertyAlreadyInstantiated()
            {
                MyOverflow = new Dictionary<string, object>();
            }

            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }
        }

        private class ClassWithExtensionPropertyAsObject
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }
        }

        private class ClassWithExtensionPropertyAsJsonElement
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> MyOverflow { get; set; }
        }

        private class ClassWithMultipleDictionaries
        {
            [JsonExtensionData]
            public Dictionary<string, object> MyOverflow { get; set; }

            public Dictionary<string, object> ActualDictionary { get; set; }
        }

        [Fact]
        public static void DeserializeIntoImmutableDictionaryProperty()
        {
            // baseline
            JsonSerializer.Deserialize<ClassWithExtensionPropertyAsImmutable>(@"{}");
            JsonSerializer.Deserialize<ClassWithExtensionPropertyAsImmutableJsonElement>(@"{}");
            JsonSerializer.Deserialize<ClassWithExtensionPropertyPrivateConstructor>(@"{}");
            JsonSerializer.Deserialize<ClassWithExtensionPropertyPrivateConstructorJsonElement>(@"{}");

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithExtensionPropertyAsImmutable>("{\"hello\":\"world\"}"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithExtensionPropertyAsImmutableJsonElement>("{\"hello\":\"world\"}"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithExtensionPropertyPrivateConstructor>("{\"hello\":\"world\"}"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithExtensionPropertyPrivateConstructorJsonElement>("{\"hello\":\"world\"}"));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<ClassWithExtensionPropertyCustomIImmutable>("{\"hello\":\"world\"}"));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<ClassWithExtensionPropertyCustomIImmutableJsonElement>("{\"hello\":\"world\"}"));
        }

        [Fact]
        public static void SerializeIntoImmutableDictionaryProperty()
        {
            // attempt to serialize a null immutable dictionary
            string expectedJson = "{}";
            var obj = new ClassWithExtensionPropertyAsImmutable();
            var json = JsonSerializer.Serialize(obj);
            Assert.Equal(expectedJson, json);

            // attempt to serialize an empty immutable dictionary
            expectedJson = "{}";
            obj = new ClassWithExtensionPropertyAsImmutable();
            obj.MyOverflow = ImmutableDictionary<string, object>.Empty;
            json = JsonSerializer.Serialize(obj);
            Assert.Equal(expectedJson, json);

            // attempt to serialize a populated immutable dictionary
            expectedJson = "{\"hello\":\"world\"}";
            obj = new ClassWithExtensionPropertyAsImmutable();
            var dictionaryStringObject = new Dictionary<string, object> { { "hello", "world" } };
            obj.MyOverflow = ImmutableDictionary.CreateRange(dictionaryStringObject);
            json = JsonSerializer.Serialize(obj);
            Assert.Equal(expectedJson, json);
        }

        private class ClassWithExtensionPropertyAsImmutable
        {
            [JsonExtensionData]
            public ImmutableDictionary<string, object> MyOverflow { get; set; }
        }

        private class ClassWithExtensionPropertyAsImmutableJsonElement
        {
            [JsonExtensionData]
            public ImmutableDictionary<string, JsonElement> MyOverflow { get; set; }
        }

        private class ClassWithExtensionPropertyPrivateConstructor
        {
            [JsonExtensionData]
            public GenericIDictionaryWrapperPrivateConstructor<string, object> MyOverflow { get; set; }
        }

        private class ClassWithExtensionPropertyPrivateConstructorJsonElement
        {
            [JsonExtensionData]
            public GenericIDictionaryWrapperPrivateConstructor<string, JsonElement> MyOverflow { get; set; }
        }

        private class ClassWithExtensionPropertyCustomIImmutable
        {
            [JsonExtensionData]
            public GenericIImmutableDictionaryWrapper<string, object> MyOverflow { get; set; }
        }

        private class ClassWithExtensionPropertyCustomIImmutableJsonElement
        {
            [JsonExtensionData]
            public GenericIImmutableDictionaryWrapper<string, JsonElement> MyOverflow { get; set; }
        }

        [Theory]
        [InlineData(typeof(ClassWithExtensionPropertyNoGenericParameters))]
        [InlineData(typeof(ClassWithExtensionPropertyOneGenericParameter))]
        [InlineData(typeof(ClassWithExtensionPropertyThreeGenericParameters))]
        public static void DeserializeIntoGenericDictionaryParameterCount(Type type)
        {
            object obj = JsonSerializer.Deserialize("{\"hello\":\"world\"}", type);

            IDictionary<string, object> extData = (IDictionary<string, object>)type.GetProperty("MyOverflow").GetValue(obj)!;
            Assert.Equal("world", ((JsonElement)extData["hello"]).GetString());
        }

        private class ClassWithExtensionPropertyNoGenericParameters
        {
            [JsonExtensionData]
            public StringToObjectIDictionaryWrapper MyOverflow { get; set; }
        }

        private class ClassWithExtensionPropertyOneGenericParameter
        {
            [JsonExtensionData]
            public StringToGenericIDictionaryWrapper<object> MyOverflow { get; set; }
        }

        private class ClassWithExtensionPropertyThreeGenericParameters
        {
            [JsonExtensionData]
            public GenericIDictonaryWrapperThreeGenericParameters<string, object, string> MyOverflow { get; set; }
        }

        [Fact]
        public static void CustomObjectConverterInExtensionProperty()
        {
            const string Json = "{\"hello\": \"world\"}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonObjectConverter());

            ClassWithExtensionPropertyAsObject obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsObject>(Json, options);
            object overflowProp = obj.MyOverflow["hello"];
            Assert.IsType<string>(overflowProp);
            Assert.Equal("world!!!", ((string)overflowProp));

            string newJson = JsonSerializer.Serialize(obj, options);
            Assert.Equal("{\"hello\":\"world!!!\"}", newJson);
        }

        private class JsonObjectConverter : JsonConverter<object>
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
        public static void CustomJsonElementConverterInExtensionProperty()
        {
            const string Json = "{\"hello\": \"world\"}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonElementConverter());

            ClassWithExtensionPropertyAsJsonElement obj = JsonSerializer.Deserialize<ClassWithExtensionPropertyAsJsonElement>(Json, options);
            JsonElement overflowProp = obj.MyOverflow["hello"];
            Assert.Equal(JsonValueKind.Undefined, overflowProp.ValueKind);

            string newJson = JsonSerializer.Serialize(obj, options);
            Assert.Equal("{\"hello\":{\"Hi\":\"There\"}}", newJson);
        }

        private class JsonElementConverter : JsonConverter<JsonElement>
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
    }
}
