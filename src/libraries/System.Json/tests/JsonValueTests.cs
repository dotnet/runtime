// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Globalization;
using Xunit;
using System.Collections;
using System.Collections.Generic;

namespace System.Json.Tests
{
    public class JsonValueTests
    {
        [Fact]
        public void JsonValue_Load_LoadWithTrailingCommaInDictionary()
        {
            Parse("{ \"a\": \"b\",}", value =>
            {
                Assert.Equal(1, value.Count);
                Assert.Equal(JsonType.String, value["a"].JsonType);
                Assert.Equal("b", value["a"]);

                JsonValue.Parse("[{ \"a\": \"b\",}]");
            });
        }

        [Theory]
        [InlineData("[]")]
        [InlineData(" \t \r \n  [  \t \r \n ]  \t \r \n ")]
        public void Parse_EmptyArray(string jsonString)
        {
            Parse(jsonString, value =>
            {
                Assert.Equal(0, value.Count);
                Assert.Equal(JsonType.Array, value.JsonType);
            });
        }

        [Theory]
        [InlineData("{}")]
        [InlineData(" \t \r \n  {  \t \r \n }  \t \r \n ")]
        public void Parse_EmptyDictionary(string jsonString)
        {
            Parse(jsonString, value =>
            {
                Assert.Equal(0, value.Count);
                Assert.Equal(JsonType.Object, value.JsonType);
            });
        }

        public static IEnumerable<object[]> ParseIntegralBoundaries_TestData()
        {
            yield return new object[] { "2147483649", "2147483649" };
            yield return new object[] { "4294967297", "4294967297" };
            yield return new object[] { "9223372036854775807", "9223372036854775807" };
            yield return new object[] { "18446744073709551615", "18446744073709551615" };
            yield return new object[] { "79228162514264337593543950336", "7.9228162514264338E+28" };
        }

        [Theory]
        [MemberData(nameof(ParseIntegralBoundaries_TestData))]
        public void Parse_IntegralBoundaries_LessThanMaxDouble_Works(string jsonString, string expectedToString)
        {
            Parse(jsonString, value =>
            {
                Assert.Equal(JsonType.Number, value.JsonType);
                Assert.Equal(expectedToString, value.ToString());
            });
        }

        [Fact]
        public void Parse_TrueFalse()
        {
            Parse("[true, false]", value =>
            {
                Assert.Equal(2, value.Count);
                Assert.Equal("true", value[0].ToString());
                Assert.Equal("false", value[1].ToString());
            });
        }

        [Fact]
        public void JsonValue_Load_ToString_JsonArrayWithNulls()
        {
            Parse("[1,2,3,null]", value =>
            {
                Assert.Equal(4, value.Count);
                Assert.Equal(JsonType.Array, value.JsonType);
                Assert.Equal("[1, 2, 3, null]", value.ToString());
            });
        }
        
        [Fact]
        public void JsonValue_ToString_JsonObjectWithNulls()
        {
            Parse("{\"a\":null,\"b\":2}", value =>
            {
                Assert.Equal(2, value.Count);
                Assert.Equal(JsonType.Object, value.JsonType);
                Assert.Equal("{\"a\": null, \"b\": 2}", value.ToString());
            });
        }

        [Fact]
        public void JsonObject_ToString_OrderingMaintained()
        {
            var obj = new JsonObject();
            obj["a"] = 1;
            obj["c"] = 3;
            obj["b"] = 2;
            Assert.Equal("{\"a\": 1, \"b\": 2, \"c\": 3}", obj.ToString());
        }

        [Fact]
        public void JsonPrimitive_QuoteEscape()
        {
            Assert.Equal((new JsonPrimitive("\"\"")).ToString(), "\"\\\"\\\"\"");
        }

        [Fact]
        public void Load_NullStream_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("stream", () => JsonValue.Load((Stream)null));
        }

        [Fact]
        public void Load_NullTextReader_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("textReader", () => JsonValue.Load((TextReader)null));
        }

        [Fact]
        public void Parse_NullJsonString_ThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("jsonString", () => JsonValue.Parse(null));
        }
        
        [Theory]
        [InlineData("")]
        [InlineData("-")]
        [InlineData("- ")]
        [InlineData("1.")]
        [InlineData("1. ")]
        [InlineData("1e+")]
        [InlineData("1 2")]
        [InlineData("077")]
        [InlineData("[1,]")]
        [InlineData("NaN")]
        [InlineData("Infinity")]
        [InlineData("-Infinity")]
        [InlineData("[")]
        [InlineData("[1")]
        [InlineData("{")]
        [InlineData("{ ")]
        [InlineData("{1")]
        [InlineData("{\"")]
        [InlineData("{\"u")]
        [InlineData("{\"\\")]
        [InlineData("{\"\\u")]
        [InlineData("{\"\\uABC")]
        [InlineData("{\"\\/")]
        [InlineData("{\"\\\\")]
        [InlineData("{\"\\\"")]
        [InlineData("{\"\\!")]
        [InlineData("[tru]")]
        [InlineData("[fals]")]
        [InlineData("{\"name\"}")]
        [InlineData("{\"name\":}")]
        [InlineData("{\"name\":1")]
        [InlineData("1e")]
        [InlineData("1e-")]
        [InlineData("\0")]
        [InlineData("\u000B1")]
        [InlineData("\u000C1")]
        [InlineData("{\"\\a\"}")]
        [InlineData("{\"\\z\"}")]
        public void Parse_InvalidInput_ThrowsArgumentException(string value)
        {
            Assert.Throws<ArgumentException>(null, () => JsonValue.Parse(value));

            using (StringReader textReader = new StringReader(value))
            {
                Assert.Throws<ArgumentException>(null, () => JsonValue.Load(textReader));
            }

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(value)))
            {
                Assert.Throws<ArgumentException>(null, () => JsonValue.Load(stream));
            }
        }

        [Fact]
        public void Parse_DoubleTooLarge_ThrowsOverflowException()
        {
            Assert.Throws<OverflowException>(() => JsonValue.Parse("1.7976931348623157E+309"));
        }

        [Fact]
        public void Parse_InvalidNumericString_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => JsonValue.Parse("1E!"));
        }
        
        [Theory]
        [InlineData("0", 0)]
        [InlineData("-0", 0)]
        [InlineData("0.00", 0)]
        [InlineData("-0.00", 0)]
        [InlineData("1", 1)]
        [InlineData("1.1", 1.1)]
        [InlineData("-1", -1)]
        [InlineData("-1.1", -1.1)]
        [InlineData("1e-10", 1e-10)]
        [InlineData("1e+10", 1e+10)]
        [InlineData("1e-30", 1e-30)]
        [InlineData("1e+30", 1e+30)]
        [InlineData("\"1\"", 1)]
        [InlineData("\"1.1\"", 1.1)]
        [InlineData("\"-1\"", -1)]
        [InlineData("\"-1.1\"", -1.1)]
        [InlineData("\"NaN\"", double.NaN)]
        [InlineData("\"Infinity\"", double.PositiveInfinity)]
        [InlineData("\"-Infinity\"", double.NegativeInfinity)]
        [InlineData("0.000000000000000000000000000011", 1.1E-29)]
        public void JsonValue_Parse_Double(string json, double expected)
        {
            foreach (string culture in new[] { "en", "fr", "de" })
            {
                CultureInfo old = CultureInfo.CurrentCulture;
                try
                {
                    CultureInfo.CurrentCulture = new CultureInfo(culture);
                    Assert.Equal(expected, (double)JsonValue.Parse(json));
                }
                finally
                {
                    CultureInfo.CurrentCulture = old;
                }
            }
        }

        // Convert a number to json and parse the string, then compare the result to the original value
        [Theory]
        [InlineData(1)]
        [InlineData(1.1)]
        [InlineData(1.25)]
        [InlineData(-1)]
        [InlineData(-1.1)]
        [InlineData(-1.25)]
        [InlineData(1e-20)]
        [InlineData(1e+20)]
        [InlineData(1e-30)]
        [InlineData(1e+30)]
        [InlineData(3.1415926535897932384626433)]
        [InlineData(3.1415926535897932384626433e-20)]
        [InlineData(3.1415926535897932384626433e+20)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.MinValue)]
        [InlineData(double.MaxValue)]
        [InlineData(18014398509481982.0)] // A number which needs 17 digits (see http://stackoverflow.com/questions/6118231/why-do-i-need-17-significant-digits-and-not-16-to-represent-a-double)
        [InlineData(1.123456789e-29)]
        [InlineData(1.123456789e-28)] // Values around the smallest positive decimal value
        public void JsonValue_Parse_Double_ViaJsonPrimitive(double number)
        {
            foreach (string culture in new[] { "en", "fr", "de" })
            {
                CultureInfo old = CultureInfo.CurrentCulture;
                try
                {
                    CultureInfo.CurrentCulture = new CultureInfo(culture);
                    Assert.Equal(number, (double)JsonValue.Parse(new JsonPrimitive(number).ToString()));
                }
                finally
                {
                    CultureInfo.CurrentCulture = old;
                }
            }
        }

        [Fact]
        public void JsonValue_Parse_MinMax_Integers_ViaJsonPrimitive()
        {
            Assert.Equal(sbyte.MinValue, (sbyte)JsonValue.Parse(new JsonPrimitive(sbyte.MinValue).ToString()));
            Assert.Equal(sbyte.MaxValue, (sbyte)JsonValue.Parse(new JsonPrimitive(sbyte.MaxValue).ToString()));

            Assert.Equal(byte.MinValue, (byte)JsonValue.Parse(new JsonPrimitive(byte.MinValue).ToString()));
            Assert.Equal(byte.MaxValue, (byte)JsonValue.Parse(new JsonPrimitive(byte.MaxValue).ToString()));

            Assert.Equal(short.MinValue, (short)JsonValue.Parse(new JsonPrimitive(short.MinValue).ToString()));
            Assert.Equal(short.MaxValue, (short)JsonValue.Parse(new JsonPrimitive(short.MaxValue).ToString()));

            Assert.Equal(ushort.MinValue, (ushort)JsonValue.Parse(new JsonPrimitive(ushort.MinValue).ToString()));
            Assert.Equal(ushort.MaxValue, (ushort)JsonValue.Parse(new JsonPrimitive(ushort.MaxValue).ToString()));

            Assert.Equal(int.MinValue, (int)JsonValue.Parse(new JsonPrimitive(int.MinValue).ToString()));
            Assert.Equal(int.MaxValue, (int)JsonValue.Parse(new JsonPrimitive(int.MaxValue).ToString()));

            Assert.Equal(uint.MinValue, (uint)JsonValue.Parse(new JsonPrimitive(uint.MinValue).ToString()));
            Assert.Equal(uint.MaxValue, (uint)JsonValue.Parse(new JsonPrimitive(uint.MaxValue).ToString()));

            Assert.Equal(long.MinValue, (long)JsonValue.Parse(new JsonPrimitive(long.MinValue).ToString()));
            Assert.Equal(long.MaxValue, (long)JsonValue.Parse(new JsonPrimitive(long.MaxValue).ToString()));

            Assert.Equal(ulong.MinValue, (ulong)JsonValue.Parse(new JsonPrimitive(ulong.MinValue).ToString()));
            Assert.Equal(ulong.MaxValue, (ulong)JsonValue.Parse(new JsonPrimitive(ulong.MaxValue).ToString()));

            Assert.Equal("1E-30", JsonValue.Parse("1e-30").ToString());
            Assert.Equal("1E+30", JsonValue.Parse("1e+30").ToString());
        }
        
        [Theory]
        [InlineData("Fact\b\f\n\r\t\"\\/</\0x")]
        [InlineData("\ud800")]
        [InlineData("x\ud800")]
        [InlineData("\udfff\ud800")]
        [InlineData("\ude03\ud912")]
        [InlineData("\uc000\ubfff")]
        [InlineData("\udfffx")]
        public void JsonPrimitive_Roundtrip_ValidUnicode(string str)
        {
            string json = new JsonPrimitive(str).ToString();

            new UTF8Encoding(false, true).GetBytes(json);

            Assert.Equal(str, JsonValue.Parse(json));
        }

        [Fact]
        public void JsonPrimitive_Roundtrip_ValidUnicode_AllChars()
        {
            for (int i = 0; i <= char.MaxValue; i++)
            {
                JsonPrimitive_Roundtrip_ValidUnicode("x" + (char)i);
            }
        }

        // String handling: http://tools.ietf.org/html/rfc7159#section-7
        [Fact]
        public void JsonPrimitive_StringHandling()
        {
            Assert.Equal("\"Fact\"", new JsonPrimitive("Fact").ToString());
            
            // Handling of characters
            Assert.Equal("\"f\"", new JsonPrimitive('f').ToString());
            Assert.Equal('f', (char)JsonValue.Parse("\"f\""));

            // Control characters with special escape sequence
            Assert.Equal("\"\\b\\f\\n\\r\\t\"", new JsonPrimitive("\b\f\n\r\t").ToString());
            
            // Other characters which must be escaped
            Assert.Equal(@"""\""\\""", new JsonPrimitive("\"\\").ToString());

            // Control characters without special escape sequence
            for (int i = 0; i < 32; i++)
            {
                if (i != '\b' && i != '\f' && i != '\n' && i != '\r' && i != '\t')
                {
                    Assert.Equal("\"\\u" + i.ToString("x04") + "\"", new JsonPrimitive("" + (char)i).ToString());
                }
            }

            // JSON does not require U+2028 and U+2029 to be escaped, but
            // JavaScript does require this:
            // http://stackoverflow.com/questions/2965293/javascript-parse-error-on-u2028-unicode-character/9168133#9168133
            Assert.Equal("\"\\u2028\\u2029\"", new JsonPrimitive("\u2028\u2029").ToString());

            // '/' also does not have to be escaped, but escaping it when
            // preceeded by a '<' avoids problems with JSON in HTML <script> tags
            Assert.Equal("\"<\\/\"", new JsonPrimitive("</").ToString());

            // Don't escape '/' in other cases as this makes the JSON hard to read
            Assert.Equal("\"/bar\"", new JsonPrimitive("/bar").ToString());
            Assert.Equal("\"foo/bar\"", new JsonPrimitive("foo/bar").ToString());

            // Valid strings should not be escaped:
            Assert.Equal("\"\ud7ff\"", new JsonPrimitive("\ud7ff").ToString());
            Assert.Equal("\"\ue000\"", new JsonPrimitive("\ue000").ToString());
            Assert.Equal("\"\ud800\udc00\"", new JsonPrimitive("\ud800\udc00").ToString());
            Assert.Equal("\"\ud912\ude03\"", new JsonPrimitive("\ud912\ude03").ToString());
            Assert.Equal("\"\udbff\udfff\"", new JsonPrimitive("\udbff\udfff").ToString());

            Assert.Equal("\"{\\\"\\\\uD800\\\\uDC00\\\": 1}\"", new JsonPrimitive("{\"\\uD800\\uDC00\": 1}").ToString());
        }

        [Fact]
        public void GetEnumerator_ThrowsInvalidOperationException()
        {
            JsonValue value = JsonValue.Parse("1");
            Assert.Throws<InvalidOperationException>(() => ((IEnumerable)value).GetEnumerator());
        }

        [Fact]
        public void Count_ThrowsInvalidOperationException()
        {
            JsonValue value = JsonValue.Parse("1");
            Assert.Throws<InvalidOperationException>(() => value.Count);
        }

        [Fact]
        public void Item_Int_ThrowsInvalidOperationException()
        {
            JsonValue value = JsonValue.Parse("1");
            Assert.Throws<InvalidOperationException>(() => value[0]);
            Assert.Throws<InvalidOperationException>(() => value[0] = 0);
        }

        [Fact]
        public void Item_String_ThrowsInvalidOperationException()
        {
            JsonValue value = JsonValue.Parse("1");
            Assert.Throws<InvalidOperationException>(() => value["abc"]);
            Assert.Throws<InvalidOperationException>(() => value["abc"] = 0);
        }

        [Fact]
        public void ContainsKey_ThrowsInvalidOperationException()
        {
            JsonValue value = JsonValue.Parse("1");
            Assert.Throws<InvalidOperationException>(() => value.ContainsKey("key"));
        }

        [Fact]
        public void Save_Stream()
        {
            JsonSubValue value = new JsonSubValue();

            using (MemoryStream stream = new MemoryStream())
            {
                value.Save(stream);
                Assert.Empty(stream.ToArray());
            }
        }

        [Fact]
        public void Save_Null_ThrowsArgumentNullException()
        {
            JsonValue value = new JsonSubValue();
            Assert.Throws<ArgumentNullException>("stream", () => value.Save((Stream)null));
        }

        [Fact]
        public void ImplicitConversion_NullJsonValue_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("value", () => { bool i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { byte i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { char i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { decimal i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { double i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { float i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { int i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { long i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { sbyte i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { short i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { uint i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { ulong i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { ushort i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { DateTime i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { DateTimeOffset i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { TimeSpan i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { Guid i = (JsonValue)null; });
            Assert.Throws<ArgumentNullException>("value", () => { Uri i = (JsonValue)null; });
        }

        [Fact]
        public void ImplicitConversion_NotJsonPrimitive_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => { bool i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { byte i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { char i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { decimal i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { double i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { float i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { int i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { long i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { sbyte i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { short i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { string i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { uint i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { ulong i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { ushort i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { DateTime i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { DateTimeOffset i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { TimeSpan i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { Guid i = new JsonArray(); });
            Assert.Throws<InvalidCastException>(() => { Uri i = new JsonArray(); });
        }

        [Fact]
        public void ImplicitCast_Bool()
        {
            JsonPrimitive primitive = new JsonPrimitive(true);
            bool toPrimitive = primitive;
            Assert.True(toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_Byte()
        {
            JsonPrimitive primitive = new JsonPrimitive(1);
            byte toPrimitive = primitive;
            Assert.Equal(1, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_Char()
        {
            JsonPrimitive primitive = new JsonPrimitive(1);
            char toPrimitive = primitive;
            Assert.Equal(1, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_Decimal()
        {
            JsonPrimitive primitive = new JsonPrimitive(1m);
            decimal toPrimitive = primitive;
            Assert.Equal(1m, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }
        
        [Fact]
        public void ImplicitCast_Double()
        {
            JsonPrimitive primitive = new JsonPrimitive(1);
            double toPrimitive = primitive;
            Assert.Equal(1.0, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_Float()
        {
            JsonPrimitive primitive = new JsonPrimitive(1);
            float toPrimitive = primitive;
            Assert.Equal(1.0f, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_Int()
        {
            JsonPrimitive primitive = new JsonPrimitive(1);
            int toPrimitive = primitive;
            Assert.Equal(1, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_Long()
        {
            JsonPrimitive primitive = new JsonPrimitive(1);
            long toPrimitive = primitive;
            Assert.Equal(1, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_SByte()
        {
            JsonPrimitive primitive = new JsonPrimitive(1);
            sbyte toPrimitive = primitive;
            Assert.Equal(1, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_Short()
        {
            JsonPrimitive primitive = new JsonPrimitive(1);
            short toPrimitive = primitive;
            Assert.Equal(1, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData(null)]
        public void ImplicitCast_String(string value)
        {
            JsonPrimitive primitive = new JsonPrimitive(value);
            string toPrimitive = primitive;
            Assert.Equal(value, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_String_NullString()
        {
            string toPrimitive = (JsonPrimitive)null;
            Assert.Equal(null, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(new JsonPrimitive((string)null), toPrimitive);
        }

        [Fact]
        public void ImplicitCast_UInt()
        {
            JsonPrimitive primitive = new JsonPrimitive(1);
            uint toPrimitive = primitive;
            Assert.Equal((uint)1, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_ULong()
        {
            JsonPrimitive primitive = new JsonPrimitive(1);
            ulong toPrimitive = primitive;
            Assert.Equal((ulong)1, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_UShort()
        {
            JsonPrimitive primitive = new JsonPrimitive(1);
            ushort toPrimitive = primitive;
            Assert.Equal((ushort)1, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_DateTime()
        {
            JsonPrimitive primitive = new JsonPrimitive(DateTime.MinValue);
            DateTime toPrimitive = primitive;
            Assert.Equal(DateTime.MinValue, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_DateTimeOffset()
        {
            JsonPrimitive primitive = new JsonPrimitive(DateTimeOffset.MinValue);
            DateTimeOffset toPrimitive = primitive;
            Assert.Equal(DateTimeOffset.MinValue, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_TimeSpan()
        {
            JsonPrimitive primitive = new JsonPrimitive(TimeSpan.Zero);
            TimeSpan toPrimitive = primitive;
            Assert.Equal(TimeSpan.Zero, toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_Guid()
        {
            JsonPrimitive primitive = new JsonPrimitive(new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11));
            Guid toPrimitive = primitive;
            Assert.Equal(new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ImplicitCast_Uri()
        {
            JsonPrimitive primitive = new JsonPrimitive(new Uri("scheme://host/"));
            Uri toPrimitive = primitive;
            Assert.Equal(new Uri("scheme://host/"), toPrimitive);

            JsonValue fromPrimitive = toPrimitive;
            Assert.Equal(primitive, toPrimitive);
        }

        [Fact]
        public void ToString_InvalidJsonType_ThrowsInvalidCastException()
        {
            InvalidJsonValue value = new InvalidJsonValue();
            Assert.Throws<InvalidCastException>(() => value.ToString());
        }

        private static void Parse(string jsonString, Action<JsonValue> action)
        {
            action(JsonValue.Parse(jsonString));

            using (StringReader textReader = new StringReader(jsonString))
            {
                action(JsonValue.Load(textReader));
            }

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
            {
                action(JsonValue.Load(stream));
            }
        }

        public class JsonSubValue : JsonValue
        {
            public override JsonType JsonType => JsonType.String;

            public override void Save(TextWriter textWriter) => textWriter.Write("Hello");
        }

        public class InvalidJsonValue : JsonValue
        {
            public override JsonType JsonType => (JsonType)(-1);
        }
    }
}
