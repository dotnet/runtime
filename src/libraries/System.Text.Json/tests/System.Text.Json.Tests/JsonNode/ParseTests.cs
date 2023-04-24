// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Text.Json.Serialization.Tests;
using Xunit;

namespace System.Text.Json.Nodes.Tests
{
    public static class ParseTests
    {
        [Fact]
        public static void Parse()
        {
            JsonObject jObject = JsonNode.Parse(JsonNodeTests.ExpectedDomJson).AsObject();

            Assert.Equal("Hello!", jObject["MyString"].GetValue<string>());
            Assert.Null(jObject["MyNull"]);
            Assert.False(jObject["MyBoolean"].GetValue<bool>());
            Assert.Equal("ed957609-cdfe-412f-88c1-02daca1b4f51", jObject["MyGuid"].GetValue<string>());
            Assert.IsType<JsonArray>(jObject["MyArray"]);
            Assert.IsType<JsonObject>(jObject["MyObject"]);

            Assert.Equal(43, jObject["MyInt"].GetValue<int>());
            Assert.Equal<uint>(43, jObject["MyInt"].GetValue<uint>());
            Assert.Equal(43, jObject["MyInt"].GetValue<long>());
            Assert.Equal<ulong>(43, jObject["MyInt"].GetValue<ulong>());
            Assert.Equal(43, jObject["MyInt"].GetValue<short>());
            Assert.Equal<ushort>(43, jObject["MyInt"].GetValue<ushort>());
            Assert.Equal(43, jObject["MyInt"].GetValue<byte>());
            Assert.Equal(43, jObject["MyInt"].GetValue<sbyte>());
            Assert.Equal(43, jObject["MyInt"].GetValue<decimal>());
            Assert.Equal(43, jObject["MyInt"].GetValue<float>());

            DateTime dt = JsonNode.Parse("\"2020-07-08T01:02:03\"").GetValue<DateTime>();
            Assert.Equal(2020, dt.Year);
            Assert.Equal(7, dt.Month);
            Assert.Equal(8, dt.Day);
            Assert.Equal(1, dt.Hour);
            Assert.Equal(2, dt.Minute);
            Assert.Equal(3, dt.Second);

            DateTimeOffset dtOffset = JsonNode.Parse("\"2020-07-08T01:02:03+01:15\"").GetValue<DateTimeOffset>();
            Assert.Equal(2020, dtOffset.Year);
            Assert.Equal(7, dtOffset.Month);
            Assert.Equal(8, dtOffset.Day);
            Assert.Equal(1, dtOffset.Hour);
            Assert.Equal(2, dtOffset.Minute);
            Assert.Equal(3, dtOffset.Second);
            Assert.Equal(new TimeSpan(1,15,0), dtOffset.Offset);
        }

        [Fact]
        public static void Parse_TryGetPropertyValue()
        {
            JsonObject jObject = JsonNode.Parse(JsonNodeTests.ExpectedDomJson).AsObject();

            JsonNode? node;

            Assert.True(jObject.TryGetPropertyValue("MyString", out node));
            Assert.Equal("Hello!", node.GetValue<string>());

            Assert.True(jObject.TryGetPropertyValue("MyNull", out node));
            Assert.Null(node);

            Assert.True(jObject.TryGetPropertyValue("MyBoolean", out node));
            Assert.False(node.GetValue<bool>());

            Assert.True(jObject.TryGetPropertyValue("MyArray", out node));
            Assert.IsType<JsonArray>(node);

            Assert.True(jObject.TryGetPropertyValue("MyInt", out node));
            Assert.Equal(43, node.GetValue<int>());

            Assert.True(jObject.TryGetPropertyValue("MyDateTime", out node));
            Assert.Equal("2020-07-08T00:00:00", node.GetValue<string>());

            Assert.True(jObject.TryGetPropertyValue("MyGuid", out node));
            Assert.Equal("ed957609-cdfe-412f-88c1-02daca1b4f51", node.AsValue().GetValue<Guid>().ToString());

            Assert.True(jObject.TryGetPropertyValue("MyObject", out node));
            Assert.IsType<JsonObject>(node);
        }

        [Fact]
        public static void Parse_TryGetValue()
        {
            Assert.True(JsonNode.Parse("\"Hello\"").AsValue().TryGetValue(out string? _));
            Assert.True(JsonNode.Parse("true").AsValue().TryGetValue(out bool? _));
            Assert.True(JsonNode.Parse("42").AsValue().TryGetValue(out byte? _));
            Assert.True(JsonNode.Parse("42").AsValue().TryGetValue(out sbyte? _));
            Assert.True(JsonNode.Parse("42").AsValue().TryGetValue(out short? _));
            Assert.True(JsonNode.Parse("42").AsValue().TryGetValue(out ushort? _));
            Assert.True(JsonNode.Parse("42").AsValue().TryGetValue(out int? _));
            Assert.True(JsonNode.Parse("42").AsValue().TryGetValue(out uint? _));
            Assert.True(JsonNode.Parse("42").AsValue().TryGetValue(out long? _));
            Assert.True(JsonNode.Parse("42").AsValue().TryGetValue(out ulong? _));
            Assert.True(JsonNode.Parse("42").AsValue().TryGetValue(out decimal? _));
            Assert.True(JsonNode.Parse("42").AsValue().TryGetValue(out float? _));
            Assert.True(JsonNode.Parse("42").AsValue().TryGetValue(out double? _));
            Assert.True(JsonNode.Parse("\"2020-07-08T00:00:00\"").AsValue().TryGetValue(out DateTime? _));
            Assert.True(JsonNode.Parse("\"ed957609-cdfe-412f-88c1-02daca1b4f51\"").AsValue().TryGetValue(out Guid? _));
            Assert.True(JsonNode.Parse("\"2020-07-08T01:02:03+01:15\"").AsValue().TryGetValue(out DateTimeOffset? _));

            JsonValue? jValue = JsonNode.Parse("\"Hello!\"").AsValue();
            Assert.False(jValue.TryGetValue(out int _));
            Assert.False(jValue.TryGetValue(out DateTime _));
            Assert.False(jValue.TryGetValue(out DateTimeOffset _));
            Assert.False(jValue.TryGetValue(out Guid _));
        }

        [Fact]
        public static void Parse_Fail()
        {
            JsonObject jObject = JsonNode.Parse(JsonNodeTests.ExpectedDomJson).AsObject();

            Assert.Throws<InvalidOperationException>(() => jObject["MyString"].GetValue<int>());
            Assert.Throws<InvalidOperationException>(() => jObject["MyBoolean"].GetValue<int>());
            Assert.Throws<InvalidOperationException>(() => jObject["MyGuid"].GetValue<int>());
            Assert.Throws<InvalidOperationException>(() => jObject["MyInt"].GetValue<string>());
            Assert.Throws<InvalidOperationException>(() => jObject["MyDateTime"].GetValue<int>());
            Assert.Throws<InvalidOperationException>(() => jObject["MyObject"].GetValue<int>());
            Assert.Throws<InvalidOperationException>(() => jObject["MyArray"].GetValue<int>());
        }

        [Fact]
        public static void NullReference_Fail()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Deserialize<JsonNode>((string)null));
            Assert.Throws<ArgumentNullException>(() => JsonNode.Parse((string)null));
            Assert.Throws<ArgumentNullException>(() => JsonNode.Parse((Stream)null));
        }

        [Fact]
        public static void NullLiteral()
        {
            Assert.Null(JsonSerializer.Deserialize<JsonNode>("null"));
            Assert.Null(JsonNode.Parse("null"));

            using (MemoryStream stream = new MemoryStream("null"u8.ToArray()))
            {
                Assert.Null(JsonNode.Parse(stream));
            }
        }

        [Fact]
        public static void InternalValueFields()
        {
            // Use reflection to inspect the internal state of the 3 fields that hold values.
            // There is not another way to verify, and using a debug watch causes nodes to be created.
            FieldInfo elementField = typeof(JsonObject).GetField("_jsonElement", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(elementField);

            FieldInfo jsonDictionaryField = typeof(JsonObject).GetField("_dictionary", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(jsonDictionaryField);

            Type jsonPropertyDictionaryType = typeof(JsonObject).Assembly.GetType("System.Text.Json.JsonPropertyDictionary`1");
            Assert.NotNull(jsonPropertyDictionaryType);

            jsonPropertyDictionaryType = jsonPropertyDictionaryType.MakeGenericType(new Type[] { typeof(JsonNode) });

            FieldInfo listField = jsonPropertyDictionaryType.GetField("_propertyList", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(listField);

            FieldInfo dictionaryField = jsonPropertyDictionaryType.GetField("_propertyDictionary", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(dictionaryField);

            using (MemoryStream stream = new MemoryStream(SimpleTestClass.s_data))
            {
                // Only JsonElement is present.
                JsonNode node = JsonNode.Parse(stream);
                object jsonDictionary = jsonDictionaryField.GetValue(node);
                Assert.Null(jsonDictionary); // Value is null until converted from JsonElement.
                Assert.NotNull(elementField.GetValue(node));
                Test();

                // Cause the single JsonElement to expand into individual JsonElement nodes.
                Assert.Equal(1, node.AsObject()["MyInt16"].GetValue<int>());
                Assert.Null(elementField.GetValue(node));

                jsonDictionary = jsonDictionaryField.GetValue(node);
                Assert.NotNull(jsonDictionary);

                Assert.NotNull(listField.GetValue(jsonDictionary));
                Assert.NotNull(dictionaryField.GetValue(jsonDictionary)); // The dictionary threshold was reached.
                Test();

                void Test()
                {
                    string actual = node.ToJsonString();

                    // Replace the escaped "+" sign used with DateTimeOffset.
                    actual = actual.Replace("\\u002B", "+");

                    Assert.Equal(SimpleTestClass.s_json.StripWhitespace(), actual);
                }
            }
        }

        [Fact]
        public static void ReadSimpleObjectWithTrailingTrivia()
        {
            byte[] data = Encoding.UTF8.GetBytes(SimpleTestClass.s_json + " /* Multi\r\nLine Comment */\t");
            using (MemoryStream stream = new MemoryStream(data))
            {
                var options = new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip
                };

                JsonNode node = JsonNode.Parse(stream, nodeOptions: null, options);

                string actual = node.ToJsonString();
                // Replace the escaped "+" sign used with DateTimeOffset.
                actual = actual.Replace("\\u002B", "+");

                Assert.Equal(SimpleTestClass.s_json.StripWhitespace(), actual);
            }
        }

        [Fact]
        public static void ReadPrimitives()
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(@"1")))
            {
                int i = JsonNode.Parse(stream).AsValue().GetValue<int>();
                Assert.Equal(1, i);
            }
        }

        [Fact]
        public static void ParseThenEdit()
        {
            const string Expected = "{\"MyString\":null,\"Node\":42,\"Array\":[43],\"Value\":44,\"IntValue\":45,\"Object\":{\"Property\":46}}";

            JsonNode node = JsonNode.Parse(Expected);
            Assert.Equal(Expected, node.ToJsonString());

            // Change a primitive
            node["IntValue"] = 1;
            const string ExpectedAfterEdit1 = "{\"MyString\":null,\"Node\":42,\"Array\":[43],\"Value\":44,\"IntValue\":1,\"Object\":{\"Property\":46}}";
            Assert.Equal(ExpectedAfterEdit1, node.ToJsonString());

            // Change element
            node["Array"][0] = 2;
            const string ExpectedAfterEdit2 = "{\"MyString\":null,\"Node\":42,\"Array\":[2],\"Value\":44,\"IntValue\":1,\"Object\":{\"Property\":46}}";
            Assert.Equal(ExpectedAfterEdit2, node.ToJsonString());

            // Change property
            node["MyString"] = "3";
            const string ExpectedAfterEdit3 = "{\"MyString\":\"3\",\"Node\":42,\"Array\":[2],\"Value\":44,\"IntValue\":1,\"Object\":{\"Property\":46}}";
            Assert.Equal(ExpectedAfterEdit3, node.ToJsonString());
        }
    }
}

