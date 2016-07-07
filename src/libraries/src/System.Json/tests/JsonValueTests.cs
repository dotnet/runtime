// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Json;
using System.Globalization;
using System.Threading;
using Xunit;

namespace System.Json.Tests
{
    public class JsonValueTests
    {
        // Facts that a trailing comma is allowed in dictionary definitions
        [Fact]
        public void LoadWithTrailingComma()
        {
            var j = JsonValue.Load(new StringReader("{ \"a\": \"b\",}"));
            Assert.Equal(1, j.Count);
            Assert.Equal(JsonType.String, j["a"].JsonType);
            Assert.Equal("b", (string)j["a"]);

            JsonValue.Parse("[{ \"a\": \"b\",}]");
        }

        [Fact]
        public void LoadWithTrailingComma2()
        {
            JsonValue.Parse("[{ \"a\": \"b\",}]");
        }

        // Fact that we correctly serialize JsonArray with null elements.
        [Fact]
        public void ToStringOnJsonArrayWithNulls()
        {
            var j = JsonValue.Load(new StringReader("[1,2,3,null]"));
            Assert.Equal(4, j.Count);
            Assert.Equal(JsonType.Array, j.JsonType);
            var str = j.ToString();
            Assert.Equal(str, "[1, 2, 3, null]");
        }

        // Fact that we correctly serialize JsonObject with null elements.
        [Fact]
        public void ToStringOnJsonObjectWithNulls()
        {
            var j = JsonValue.Load(new StringReader("{\"a\":null,\"b\":2}"));
            Assert.Equal(2, j.Count);
            Assert.Equal(JsonType.Object, j.JsonType);
            var str = j.ToString();
            Assert.Equal(str, "{\"a\": null, \"b\": 2}");
        }

        [Fact]
        public void JsonObjectOrder()
        {
            var obj = new JsonObject();
            obj["a"] = 1;
            obj["c"] = 3;
            obj["b"] = 2;
            var str = obj.ToString();
            Assert.Equal(str, "{\"a\": 1, \"b\": 2, \"c\": 3}");
        }

        [Fact]
        public void QuoteEscapeBug_20869()
        {
            Assert.Equal((new JsonPrimitive("\"\"")).ToString(), "\"\\\"\\\"\"");
        }

        // Fact whether an exception is thrown for invalid JSON
        [Fact]
        public void CheckErrors()
        {
            Assert.ThrowsAny<ArgumentException>(() => JsonValue.Parse(@"-"));
            Assert.ThrowsAny<ArgumentException>(() => JsonValue.Parse(@"- "));
            Assert.ThrowsAny<ArgumentException>(() => JsonValue.Parse(@"1."));
            Assert.ThrowsAny<ArgumentException>(() => JsonValue.Parse(@"1. "));
            Assert.ThrowsAny<ArgumentException>(() => JsonValue.Parse(@"1e+"));
            Assert.ThrowsAny<ArgumentException>(() => JsonValue.Parse(@"1 2"));
            Assert.ThrowsAny<ArgumentException>(() => JsonValue.Parse(@"077"));
            Assert.ThrowsAny<ArgumentException>(() => JsonValue.Parse(@"[1,]"));
        }

        // Parse a json string and compare to the expected value
        private void CheckDouble(double expected, string json)
        {
            double jvalue = (double)JsonValue.Parse(json);
            Assert.Equal(expected, jvalue);
        }

        // Convert a number to json and parse the string, then compare the result to the original value
        private void CheckDouble(double number)
        {
            double jvalue = (double)JsonValue.Parse(new JsonPrimitive(number).ToString());
            Assert.Equal(number, jvalue); // should be exactly the same
        }

        [Fact]
        public void CheckIntegers()
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
        public void CheckNumbers()
        {
            CheckDouble(0, "0");
            CheckDouble(0, "-0");
            CheckDouble(0, "0.00");
            CheckDouble(0, "-0.00");
            CheckDouble(1, "1");
            CheckDouble(1.1, "1.1");
            CheckDouble(-1, "-1");
            CheckDouble(-1.1, "-1.1");
            CheckDouble(1e-10, "1e-10");
            CheckDouble(1e+10, "1e+10");
            CheckDouble(1e-30, "1e-30");
            CheckDouble(1e+30, "1e+30");

            CheckDouble(1, "\"1\"");
            CheckDouble(1.1, "\"1.1\"");
            CheckDouble(-1, "\"-1\"");
            CheckDouble(-1.1, "\"-1.1\"");

            CheckDouble(double.NaN, "\"NaN\"");
            CheckDouble(double.PositiveInfinity, "\"Infinity\"");
            CheckDouble(double.NegativeInfinity, "\"-Infinity\"");

            Assert.ThrowsAny<ArgumentException>(() => JsonValue.Parse("NaN"));
            Assert.ThrowsAny<ArgumentException>(() => JsonValue.Parse("Infinity"));
            Assert.ThrowsAny<ArgumentException>(() => JsonValue.Parse("-Infinity"));

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

            CheckDouble(1);
            CheckDouble(1.1);
            CheckDouble(1.25);
            CheckDouble(-1);
            CheckDouble(-1.1);
            CheckDouble(-1.25);
            CheckDouble(1e-20);
            CheckDouble(1e+20);
            CheckDouble(1e-30);
            CheckDouble(1e+30);
            CheckDouble(3.1415926535897932384626433);
            CheckDouble(3.1415926535897932384626433e-20);
            CheckDouble(3.1415926535897932384626433e+20);
            CheckDouble(double.NaN);
            CheckDouble(double.PositiveInfinity);
            CheckDouble(double.NegativeInfinity);
            CheckDouble(double.MinValue);
            CheckDouble(double.MaxValue);

            // A number which needs 17 digits (see http://stackoverflow.com/questions/6118231/why-do-i-need-17-significant-digits-and-not-16-to-represent-a-double)
            CheckDouble(18014398509481982.0);

            // Values around the smallest positive decimal value
            CheckDouble(1.123456789e-29);
            CheckDouble(1.123456789e-28);

            CheckDouble(1.1E-29, "0.000000000000000000000000000011");
            // This is being parsed as a decimal and rounded to 1e-28, even though it can be more accurately be represented by a double
            //CheckDouble (1.1E-28, "0.00000000000000000000000000011");
        }

        // Retry the Fact with different locales
        [Fact]
        public void CheckNumbersCulture()
        {
            CultureInfo old = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en");
                CheckNumbers();
                CultureInfo.CurrentCulture = new CultureInfo("fr");
                CheckNumbers();
                CultureInfo.CurrentCulture = new CultureInfo("de");
                CheckNumbers();
            }
            finally
            {
                CultureInfo.CurrentCulture = old;
            }
        }

        // Convert a string to json and parse the string, then compare the result to the original value
        private void CheckString(string str)
        {
            var json = new JsonPrimitive(str).ToString();
            // Check whether the string is valid Unicode (will throw for broken surrogate pairs)
            new UTF8Encoding(false, true).GetBytes(json);
            string jvalue = (string)JsonValue.Parse(json);
            Assert.Equal(str, jvalue);
        }

        // String handling: http://tools.ietf.org/html/rfc7159#section-7
        [Fact]
        public void CheckStrings()
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
                if (i != '\b' && i != '\f' && i != '\n' && i != '\r' && i != '\t')
                    Assert.Equal("\"\\u" + i.ToString("x04") + "\"", new JsonPrimitive("" + (char)i).ToString());

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

            CheckString("Fact\b\f\n\r\t\"\\/</\0x");
            for (int i = 0; i < 65536; i++)
                CheckString("x" + ((char)i));

            // Check broken surrogate pairs
            CheckString("\ud800");
            CheckString("x\ud800");
            CheckString("\udfff\ud800");
            CheckString("\ude03\ud912");
            CheckString("\uc000\ubfff");
            CheckString("\udfffx");
            // Valid strings should not be escaped:
            Assert.Equal("\"\ud7ff\"", new JsonPrimitive("\ud7ff").ToString());
            Assert.Equal("\"\ue000\"", new JsonPrimitive("\ue000").ToString());
            Assert.Equal("\"\ud800\udc00\"", new JsonPrimitive("\ud800\udc00").ToString());
            Assert.Equal("\"\ud912\ude03\"", new JsonPrimitive("\ud912\ude03").ToString());
            Assert.Equal("\"\udbff\udfff\"", new JsonPrimitive("\udbff\udfff").ToString());
        }
    }
}