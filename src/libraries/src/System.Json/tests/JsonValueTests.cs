// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Globalization;
using Xunit;

namespace System.Json.Tests
{
    public class JsonValueTests
    {
        // Facts that a trailing comma is allowed in dictionary definitions
        [Fact]
        public void JsonValue_Load_LoadWithTrailingComma()
        {
            JsonValue j = JsonValue.Load(new StringReader("{ \"a\": \"b\",}"));
            Assert.Equal(1, j.Count);
            Assert.Equal(JsonType.String, j["a"].JsonType);
            Assert.Equal("b", j["a"]);

            JsonValue.Parse("[{ \"a\": \"b\",}]");
        }

        // Fact that we correctly serialize JsonArray with null elements.
        [Fact]
        public void JsonValue_Load_ToString_JsonArrayWithNulls()
        {
            JsonValue j = JsonValue.Load(new StringReader("[1,2,3,null]"));
            Assert.Equal(4, j.Count);
            Assert.Equal(JsonType.Array, j.JsonType);
            Assert.Equal("[1, 2, 3, null]", j.ToString());
        }

        // Fact that we correctly serialize JsonObject with null elements.
        [Fact]
        public void JsonValue_ToString_JsonObjectWithNulls()
        {
            JsonValue j = JsonValue.Load(new StringReader("{\"a\":null,\"b\":2}"));
            Assert.Equal(2, j.Count);
            Assert.Equal(JsonType.Object, j.JsonType);
            Assert.Equal("{\"a\": null, \"b\": 2}", j.ToString());
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

        // Fact whether an exception is thrown for invalid JSON
        [Theory]
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
        public void JsonValue_Parse_InvalidInput_ThrowsArgumentException(string value)
        {
            Assert.Throws<ArgumentException>(() => JsonValue.Parse(value));
        }

        // Parse a json string and compare to the expected value
        [Theory]
        [InlineData(0, "0")]
        [InlineData(0, "-0")]
        [InlineData(0, "0.00")]
        [InlineData(0, "-0.00")]
        [InlineData(1, "1")]
        [InlineData(1.1, "1.1")]
        [InlineData(-1, "-1")]
        [InlineData(-1.1, "-1.1")]
        [InlineData(1e-10, "1e-10")]
        [InlineData(1e+10, "1e+10")]
        [InlineData(1e-30, "1e-30")]
        [InlineData(1e+30, "1e+30")]
        [InlineData(1, "\"1\"")]
        [InlineData(1.1, "\"1.1\"")]
        [InlineData(-1, "\"-1\"")]
        [InlineData(-1.1, "\"-1.1\"")]
        [InlineData(double.NaN, "\"NaN\"")]
        [InlineData(double.PositiveInfinity, "\"Infinity\"")]
        [InlineData(double.NegativeInfinity, "\"-Infinity\"")]
        [InlineData(1.1E-29, "0.000000000000000000000000000011")]
        public void JsonValue_Parse_Double(double expected, string json)
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
        }

        [Fact]
        public void JsonPrimitive_ToString()
        {
            Assert.Equal("1.1", new JsonPrimitive(1.1).ToString());
            Assert.Equal("-1.1", new JsonPrimitive(-1.1).ToString());
            Assert.Equal("1E-20", new JsonPrimitive(1e-20).ToString());
            Assert.Equal("1E+20", new JsonPrimitive(1e+20).ToString());
            Assert.Equal("1E-30", new JsonPrimitive(1e-30).ToString());
            Assert.Equal("1E+30", new JsonPrimitive(1e+30).ToString());

            Assert.Equal("\"NaN\"", new JsonPrimitive(double.NaN).ToString());
            Assert.Equal("\"Infinity\"", new JsonPrimitive(double.PositiveInfinity).ToString());
            Assert.Equal("\"-Infinity\"", new JsonPrimitive(double.NegativeInfinity).ToString());

            Assert.Equal("1E-30", JsonValue.Parse("1e-30").ToString());
            Assert.Equal("1E+30", JsonValue.Parse("1e+30").ToString());
        }

        // Convert a string to json and parse the string, then compare the result to the original value
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
        }
    }
}
