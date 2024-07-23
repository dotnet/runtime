// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class JsonElementTests
    {
        [Fact]
        public void SerializeJsonElement()
        {
            JsonElementClass obj = JsonSerializer.Deserialize<JsonElementClass>(JsonElementClass.s_json);
            obj.Verify();
            string reserialized = JsonSerializer.Serialize(obj);

            // Properties in the exported json will be in the order that they were reflected, doing a quick check to see that
            // we end up with the same length (i.e. same amount of data) to start.
            Assert.Equal(JsonElementClass.s_json.StripWhitespace().Length, reserialized.Length);

            // Shoving it back through the parser should validate round tripping.
            obj = JsonSerializer.Deserialize<JsonElementClass>(reserialized);
            obj.Verify();
        }

        [Fact]
        public void SerializeJsonElementArray()
        {
            JsonElementArrayClass obj = JsonSerializer.Deserialize<JsonElementArrayClass>(JsonElementArrayClass.s_json);
            obj.Verify();
            string reserialized = JsonSerializer.Serialize(obj);

            // Properties in the exported json will be in the order that they were reflected, doing a quick check to see that
            // we end up with the same length (i.e. same amount of data) to start.
            Assert.Equal(JsonElementArrayClass.s_json.StripWhitespace().Length, reserialized.Length);

            // Shoving it back through the parser should validate round tripping.
            obj = JsonSerializer.Deserialize<JsonElementArrayClass>(reserialized);
            obj.Verify();
        }

        [Theory,
            InlineData(5),
            InlineData(10),
            InlineData(20),
            InlineData(1024)]
        public void ReadJsonElementFromStream(int defaultBufferSize)
        {
            // Streams need to read ahead when they hit objects or arrays that are assigned to JsonElement or object.

            byte[] data = Encoding.UTF8.GetBytes(@"{""Data"":[1,true,{""City"":""MyCity""},null,""foo""]}");
            MemoryStream stream = new MemoryStream(data);
            JsonElement obj = JsonSerializer.DeserializeAsync<JsonElement>(stream, new JsonSerializerOptions { DefaultBufferSize = defaultBufferSize }).Result;

            data = Encoding.UTF8.GetBytes(@"[1,true,{""City"":""MyCity""},null,""foo""]");
            stream = new MemoryStream(data);
            obj = JsonSerializer.DeserializeAsync<JsonElement>(stream, new JsonSerializerOptions { DefaultBufferSize = defaultBufferSize }).Result;

            // Ensure we fail with incomplete data
            data = Encoding.UTF8.GetBytes(@"{""Data"":[1,true,{""City"":""MyCity""},null,""foo""]");
            stream = new MemoryStream(data);
            Assert.Throws<JsonException>(() => JsonSerializer.DeserializeAsync<JsonElement>(stream, new JsonSerializerOptions { DefaultBufferSize = defaultBufferSize }).Result);

            data = Encoding.UTF8.GetBytes(@"[1,true,{""City"":""MyCity""},null,""foo""");
            stream = new MemoryStream(data);
            Assert.Throws<JsonException>(() => JsonSerializer.DeserializeAsync<JsonElement>(stream, new JsonSerializerOptions { DefaultBufferSize = defaultBufferSize }).Result);
        }

        [Theory]
        [InlineData("null", "  null       ")]
        [InlineData("false", "   false  ")]
        [InlineData("true", "   true  ")]
        [InlineData("-0.0", "0")]
        [InlineData("0", "0.0000e4")]
        [InlineData("0", "0.0000e-4")]
        [InlineData("1", "1.0")]
        [InlineData("1", "1e0")]
        [InlineData("1", "1.0000")]
        [InlineData("1", "1.0000e0")]
        [InlineData("1", "0.10000e1")]
        [InlineData("1", "10.0000e-1")]
        [InlineData("10001", "1.0001e4")]
        [InlineData("10001e-3", "1.0001e1")]
        [InlineData("1", "0.1e1")]
        [InlineData("0.1", "1e-1")]
        [InlineData("0.001", "1e-3")]
        [InlineData("1e9", "1000000000")]
        [InlineData("11", "1.100000000e1")]
        [InlineData("3.141592653589793", "3141592653589793E-15")]
        [InlineData("0.000000000000000000000000000000000000000001", "1e-42")]
        [InlineData("1000000000000000000000000000000000000000000", "1e42")]
        [InlineData("-1.1e3", "-1100")]
        [InlineData("79228162514264337593543950336", "792281625142643375935439503360e-1")] // decimal.MaxValue + 1
        [InlineData("79228162514.264337593543950336", "792281625142643375935439503360e-19")]
        [InlineData("1.75e+300", "1.75E+300")] // Variations in exponent casing
        [InlineData( // > 256 digits
            "1.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001",

            "100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001" + "E-512")]
        [InlineData("\"\"", "   \"\"")]
        [InlineData("\"ABC\"", "\"\\u0041\\u0042\\u0043\"")]
        [InlineData("{ }", " {    }")]
        [InlineData("""{ "x" : 1, "y" : 2 }""", """{ "x" : 1, "y" : 2 }""")]
        [InlineData("""{ "x" : 1, "y" : 2 }""", """{ "y" : 2, "x" : 1 }""")]
        [InlineData("""{ "x" : 1, "x" : 2 }""", """{ "x" : 1, "x" : 2 }""")]
        [InlineData("""{ "x" : 1, "y" : null, "x" : 2 }""", """{ "y" : null, "x" : 1, "x" : 2 }""")]
        [InlineData("""{ "x" : 1, "y" : null, "x" : 2 }""", """{ "x" : 1, "x" : 2, "y" : null }""")]
        [InlineData("""[]""", """ [ ]""")]
        [InlineData("""[1, 2, 3]""", """ [1,  2,  3  ]""")]
        [InlineData("""[null, false, 3.14, "ABC", { "x" : 1, "y" : 2 }, []]""",
                    """[null, false, 314e-2, "\u0041\u0042\u0043", { "y" : 2, "x" : 1 }, [ ] ]""")]
        public static void DeepEquals_EqualValuesReturnTrue(string value1, string value2)
        {
            JsonElement element1 = JsonDocument.Parse(value1).RootElement;
            JsonElement element2 = JsonDocument.Parse(value2).RootElement;

            // Reflexivity
            Assert.True(JsonElement.DeepEquals(element1, element2));
            Assert.True(JsonElement.DeepEquals(element2, element2));

            // Core assertion
            Assert.True(JsonElement.DeepEquals(element1, element2));

            // Symmetry
            Assert.True(JsonElement.DeepEquals(element2, element1));
        }

        [Theory]
        // Kind mismatch
        [InlineData("null", "false")]
        [InlineData("null", "42")]
        [InlineData("null", "\"str\"")]
        [InlineData("null", "{}")]
        [InlineData("null", "[]")]
        [InlineData("false", "42")]
        [InlineData("false", "\"str\"")]
        [InlineData("false", "{}")]
        [InlineData("false", "[]")]
        [InlineData("42", "\"str\"")]
        [InlineData("42", "{}")]
        [InlineData("42", "[]")]
        [InlineData("\"str\"", "{}")]
        [InlineData("\"str\"", "[]")]
        [InlineData("{}", "[]")]
        // Value mismatch
        [InlineData("false", "true")]
        [InlineData("0", "1")]
        [InlineData("1", "-1")]
        [InlineData("1.1", "-1.1")]
        [InlineData("1.1e5", "-1.1e5")]
        [InlineData("0", "1e-1024")]
        [InlineData("1", "0.1")]
        [InlineData("1", "1.1")]
        [InlineData("1", "1e1")]
        [InlineData("1", "1.00001")]
        [InlineData("1", "1.0000e1")]
        [InlineData("1", "0.1000e-1")]
        [InlineData("1", "10.0000e-2")]
        [InlineData("10001", "1.0001e3")]
        [InlineData("10001e-3", "1.0001e2")]
        [InlineData("1", "0.1e2")]
        [InlineData("0.1", "1e-2")]
        [InlineData("0.001", "1e-4")]
        [InlineData("1e9", "1000000001")]
        [InlineData("11", "1.100000001e1")]
        [InlineData("0.000000000000000000000000000000000000000001", "1e-43")]
        [InlineData("1000000000000000000000000000000000000000000", "1e43")]
        [InlineData("-1.1e3", "-1100.1")]
        [InlineData("79228162514264337593543950336", "7922816251426433759354395033600e-1")] // decimal.MaxValue + 1
        [InlineData("79228162514.264337593543950336", "7922816251426433759354395033601e-19")]
        [InlineData("1.75e+300", "1.75E+301")] // Variations in exponent casing
        [InlineData("1e2147483647", "1e-2147483648")] // int.MaxValue, int.MinValue exponents
        [InlineData( // > 256 digits
            "1.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
              "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001",

            "100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
             "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000003" + "E-512")]
        [InlineData("\"\"", "   \" \"")]
        [InlineData("\"ABC\"", "   \"ABc\"")]
        [InlineData("[1]", "[]")]
        [InlineData("[1]", "[2]")]
        [InlineData("[1,2,3]", "[1,2,3,4]")]
        [InlineData("[1,2,3]", "[1,3,2]")]
        [InlineData("{}", """{ "Prop" : null }""")]
        [InlineData("""{ "Prop" : 1 }""", """{ "Prop" : null }""")]
        [InlineData("""{ "Prop1" : 1 }""", """{ "Prop1" : 1, "Prop2" : 2 }""")]
        [InlineData("""{ "Prop1" : 1, "Prop2": {} }""", """{ "Prop1" : 1, "Prop2" : 2 }""")]
        [InlineData("""{ "Prop1" : 1, "Prop2": {}, "Prop3": false }""", """{ "Prop1" : 1, "Prop2" : { "c" : null }, "Prop3" : false }""")]
        [InlineData("""{ "Prop1" : 1, "Prop2": {}, "Prop3": false }""", """{ "Prop1" : 1, "Prop3" : true, "Prop2" : {} }""")]
        [InlineData("""{ "Prop3" : null, "Prop1" : 1, "Prop1" : 2 }""", """{ "Prop1" : 2, "Prop1" : 1, "Prop3" : null }""")]
        public static void DeepEquals_NotEqualValuesReturnFalse(string value1, string value2)
        {
            JsonElement element1 = JsonDocument.Parse(value1).RootElement;
            JsonElement element2 = JsonDocument.Parse(value2).RootElement;

            // Reflexivity
            Assert.True(JsonElement.DeepEquals(element1, element1));
            Assert.True(JsonElement.DeepEquals(element2, element2));

            // Core assertion
            Assert.False(JsonElement.DeepEquals(element1, element2));

            // Symmetry
            Assert.False(JsonElement.DeepEquals(element2, element1));
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(500)]
        public static void DeepEquals_DeepJsonDocument(int depth)
        {
            ArrayBufferWriter<byte> bufferWriter = new();
            using Utf8JsonWriter writer = new(bufferWriter);

            for (int i = 0; i < depth; i++)
            {
                if (i % 2 == 0)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("prop");
                }
                else
                {
                    writer.WriteStartArray();
                }
            }

            writer.WriteNumberValue(42);

            for (int i = depth - 1; i >= 0; i--)
            {
                if (i % 2 == 0)
                {
                    writer.WriteEndObject();
                }
                else
                {
                    writer.WriteEndArray();
                }
            }

            writer.Flush();

            JsonDocumentOptions options = new JsonDocumentOptions { MaxDepth = depth };
            using JsonDocument jDoc = JsonDocument.Parse(bufferWriter.WrittenSpan.ToArray(), options);
            JsonElement element = jDoc.RootElement;

            Assert.True(JsonElement.DeepEquals(element, element));
        }
    }
}
