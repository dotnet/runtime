// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class Utf8JsonReaderTests
    {
        [Fact]
        public static void TestingNumbers()
        {
            var random = new Random(42);

            const int numberOfItems = 1_000;

            // Make sure we have 1_005 values in each numeric list.
            var ints = new List<int>
            {
                0,
                12345,
                -12345,
                int.MaxValue,
                int.MinValue
            };
            for (int i = 0; i < numberOfItems; i++)
            {
                int value = random.Next(int.MinValue, int.MaxValue);
                ints.Add(value);
            }

            var longs = new List<long>
            {
                0,
                12345678901,
                -12345678901,
                long.MaxValue,
                long.MinValue
            };
            for (int i = 0; i < numberOfItems; i++)
            {
                long value = random.Next(int.MinValue, int.MaxValue);
                if (value < 0)
                    value += int.MinValue;
                else
                    value += int.MaxValue;
                longs.Add(value);
            }

            var doubles = new List<double>
            {
                0.000,
                1.1234e1,
                -1.1234e1,
                1.79769313486231E+308,  // double.MaxValue doesn't round trip
                -1.79769313486231E+308  // double.MinValue doesn't round trip
            };
            for (int i = 0; i < numberOfItems / 2; i++)
            {
                double value = JsonTestHelper.NextDouble(random, double.MinValue / 10, double.MaxValue / 10);
                doubles.Add(value);
            }
            for (int i = 0; i < numberOfItems / 2; i++)
            {
                double value = JsonTestHelper.NextDouble(random, 1_000_000, -1_000_000);
                doubles.Add(value);
            }

            var floats = new List<float>
            {
                0.000f,
                1.1234e1f,
                -1.1234e1f,
                float.MaxValue,
                float.MinValue
            };
            for (int i = 0; i < numberOfItems; i++)
            {
                float value = JsonTestHelper.NextFloat(random);
                floats.Add(value);
            }

            var decimals = new List<decimal>
            {
                (decimal)0.000,
                (decimal)1.1234e1,
                (decimal)-1.1234e1,
                decimal.MaxValue,
                decimal.MinValue
            };
            for (int i = 0; i < numberOfItems / 2; i++)
            {
                decimal value = JsonTestHelper.NextDecimal(random, 78E14, -78E14);
                decimals.Add(value);
            }
            for (int i = 0; i < numberOfItems / 2; i++)
            {
                decimal value = JsonTestHelper.NextDecimal(random, 1_000_000, -1_000_000);
                decimals.Add(value);
            }

            var builder = new StringBuilder();
            builder.Append("{");

            for (int i = 0; i < ints.Count; i++)
            {
                builder.Append("\"int").Append(i).Append("\": ");
                builder.Append(ints[i]).Append(", ");
            }
            for (int i = 0; i < longs.Count; i++)
            {
                builder.Append("\"long").Append(i).Append("\": ");
                builder.Append(longs[i]).Append(", ");
            }
            for (int i = 0; i < doubles.Count; i++)
            {
                // Use InvariantCulture to format the numbers to make sure they retain the decimal point '.'
                builder.Append("\"double").Append(i).Append("\": ");
                var str = string.Format(CultureInfo.InvariantCulture, "{0}, ", doubles[i]);
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", str);
            }
            for (int i = 0; i < floats.Count; i++)
            {
                builder.Append("\"float").Append(i).Append("\": ");
                var str = string.Format(CultureInfo.InvariantCulture, "{0}, ", floats[i]);
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", str);
            }
            for (int i = 0; i < decimals.Count; i++)
            {
                builder.Append("\"decimal").Append(i).Append("\": ");
                var str = string.Format(CultureInfo.InvariantCulture, "{0}, ", decimals[i]);
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}", str);
            }

            builder.Append("\"intEnd\": 0}");

            string jsonString = builder.ToString();
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, true, default);
            string key = "";
            int count = 0;
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.PropertyName)
                {
                    key = json.GetStringValue();
                }
                if (json.TokenType == JsonTokenType.Number)
                {
                    if (key.StartsWith("int"))
                    {
                        Assert.True(json.TryGetInt32Value(out int numberInt));
                        if (count >= ints.Count)
                            count = 0;
                        Assert.Equal(ints[count], numberInt);
                        count++;
                    }
                    else if (key.StartsWith("long"))
                    {
                        Assert.True(json.TryGetInt64Value(out long numberLong));
                        if (count >= longs.Count)
                            count = 0;
                        Assert.Equal(longs[count], numberLong);
                        count++;
                    }
                    else if (key.StartsWith("float"))
                    {
                        Assert.True(json.TryGetSingleValue(out float numberFloat));
                        if (count >= floats.Count)
                            count = 0;

                        var str = string.Format(CultureInfo.InvariantCulture, "{0}", floats[count]);
                        float expected = float.Parse(str, CultureInfo.InvariantCulture);

                        Assert.Equal(expected, numberFloat);
                        count++;
                    }
                    else if (key.StartsWith("double"))
                    {
                        Assert.True(json.TryGetDoubleValue(out double numberDouble));
                        if (count >= doubles.Count)
                            count = 0;

                        string roundTripActual = numberDouble.ToString("R", CultureInfo.InvariantCulture);
                        double actual = double.Parse(roundTripActual, CultureInfo.InvariantCulture);

                        string roundTripExpected = doubles[count].ToString("R", CultureInfo.InvariantCulture);
                        double expected = double.Parse(roundTripExpected, CultureInfo.InvariantCulture);

                        // Temporary work around for precision/round-tripping issues with Utf8Parser
                        // https://github.com/dotnet/corefx/issues/33360
                        if (expected != actual)
                        {
                            double diff = Math.Abs(expected - actual);
                            Assert.True(diff < 1E-9 || diff > 1E288);
                        }
                        else
                        {
                            Assert.Equal(expected, actual);
                        }
                        count++;
                    }
                    else if (key.StartsWith("decimal"))
                    {
                        Assert.True(json.TryGetDecimalValue(out decimal numberDecimal));
                        if (count >= decimals.Count)
                            count = 0;

                        var str = string.Format(CultureInfo.InvariantCulture, "{0}", decimals[count]);
                        decimal expected = decimal.Parse(str, CultureInfo.InvariantCulture);
                        Assert.Equal(expected, numberDecimal);
                        count++;
                    }
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [InlineData("0.000", 0)]
        [InlineData("1e1", 10)]
        [InlineData("1.1e2", 110)]
        [InlineData("12345.1", 12345.1)]
        [InlineData("12345678901", 12345678901)]
        [InlineData("123456789012345678901", 123456789012345678901d)]
        public static void TestingNumbersInvalidConversionToInt32(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, true, default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.False(json.TryGetInt32Value(out int value));
                    Assert.True(json.TryGetDoubleValue(out double doubleValue));
                    Assert.Equal(expected, doubleValue);
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [InlineData("0.000", 0)]
        [InlineData("1e1", 10)]
        [InlineData("1.1e2", 110)]
        [InlineData("12345.1", 12345.1)]
        [InlineData("123456789012345678901", 123456789012345678901d)]
        public static void TestingNumbersInvalidConversionToInt64(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, true, default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.False(json.TryGetInt64Value(out long value));
                    Assert.True(json.TryGetDoubleValue(out double doubleValue));
                    Assert.Equal(expected, doubleValue);
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Fact]
        public static void TryGetInvalidConversion()
        {
            string jsonString = "[\"stringValue\", true, 1234]";
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, true, default);
            while (json.Read())
            {
                if (json.TokenType != JsonTokenType.String)
                {
                    try
                    {
                        string value = json.GetStringValue();
                        Assert.True(false, "Expected TryGetValueAsString to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }
                }

                if (json.TokenType != JsonTokenType.True && json.TokenType != JsonTokenType.False)
                {
                    try
                    {
                        bool value = json.GetBooleanValue();
                        Assert.True(false, "Expected TryGetValueAsBoolean to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }
                }

                if (json.TokenType != JsonTokenType.Number)
                {
                    try
                    {
                        json.TryGetInt32Value(out int value);
                        Assert.True(false, "Expected TryGetValueAsInt32 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetInt64Value(out long value);
                        Assert.True(false, "Expected TryGetValueAsInt64 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetSingleValue(out float value);
                        Assert.True(false, "Expected TryGetValueAsSingle to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetDoubleValue(out double value);
                        Assert.True(false, "Expected TryGetValueAsDouble to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetDecimalValue(out decimal value);
                        Assert.True(false, "Expected TryGetValueAsDecimal to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [InlineData("{\"message\":\"Hello, I am \\\"Ahson!\\\"\"}")]
        [InlineData("{\"nam\\\"e\":\"ah\\\"son\"}")]
        [InlineData("{\"Here is a string: \\\"\\\"\":\"Here is a\",\"Here is a back slash\\\\\":[\"Multiline\\r\\n String\\r\\n\",\"\\tMul\\r\\ntiline String\",\"\\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\\"],\"str\":\"\\\"\\\"\"}")]
        [InlineData("[\"\\u0030\\u0031\\u0032\\u0033\\u0034\\u0035\", \"\\u0000\\u002B\", \"a\\u005C\\u0072b\", \"a\\\\u005C\\u0072b\", \"a\\u008E\\u008Fb\", \"a\\uD803\\uDE6Db\", \"a\\uD834\\uDD1Eb\", \"a\\\\uD834\\\\uDD1Eb\"]")]
        [InlineData("{\"message\":\"Hello /a/b/c \\/ \\r\\b\\n\\f\\t\\/\"}")]
        [InlineData(null)]  // Large randomly generated string
        public static void TestingGetString(string jsonString)
        {
            if (jsonString == null)
            {
                var random = new Random(42);
                var charArray = new char[500];
                charArray[0] = '"';
                for (int i = 1; i < charArray.Length; i++)
                {
                    charArray[i] = (char)random.Next('?', '\\'); // ASCII values (between 63 and 91) that don't need to be escaped.
                }

                charArray[256] = '\\';
                charArray[257] = '"';
                charArray[charArray.Length - 1] = '"';
                jsonString = new string(charArray);
            }

            var expectedPropertyNames = new List<string>();
            var expectedValues = new List<string>();

            var jsonNewtonsoft = new JsonTextReader(new StringReader(jsonString));
            while (jsonNewtonsoft.Read())
            {
                if (jsonNewtonsoft.TokenType == JsonToken.String)
                {
                    expectedValues.Add(jsonNewtonsoft.Value.ToString());
                }
                else if (jsonNewtonsoft.TokenType == JsonToken.PropertyName)
                {
                    expectedPropertyNames.Add(jsonNewtonsoft.Value.ToString());
                }
            }

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var actualPropertyNames = new List<string>();
            var actualValues = new List<string>();

            var json = new Utf8JsonReader(dataUtf8, true, default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    actualValues.Add(json.GetStringValue());
                }
                else if (json.TokenType == JsonTokenType.PropertyName)
                {
                    actualPropertyNames.Add(json.GetStringValue());
                }
            }

            Assert.Equal(expectedPropertyNames.Count, actualPropertyNames.Count);
            for (int i = 0; i < expectedPropertyNames.Count; i++)
            {
                Assert.Equal(expectedPropertyNames[i], actualPropertyNames[i]);
            }

            Assert.Equal(expectedValues.Count, actualValues.Count);
            for (int i = 0; i < expectedValues.Count; i++)
            {
                Assert.Equal(expectedValues[i], actualValues[i]);
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [InlineData("\"a\\uDD1E\"")]
        [InlineData("\"a\\uDD1Eb\"")]
        [InlineData("\"a\\uD834\"")]
        [InlineData("\"a\\uD834\\u0030\"")]
        [InlineData("\"a\\uD834\\uD834\"")]
        [InlineData("\"a\\uD834b\"")]
        [InlineData("\"a\\uDD1E\\uD834b\"")]
        [InlineData("\"a\\\\uD834\\uDD1Eb\"")]
        [InlineData("\"a\\uDD1E\\\\uD834b\"")]
        public static void TestingGetStringInvalidUTF16(string jsonString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

                Assert.True(json.Read());
                Assert.Equal(JsonTokenType.String, json.TokenType);
                try
                {
                    string val = json.GetStringValue();
                    Assert.True(false, "Expected InvalidOperationException when trying to get string value for invalid UTF-16 JSON text.");
                }
                catch (InvalidOperationException) { }
            }
        }



        [Theory]
        [MemberData(nameof(InvalidUTF8Strings))]
        public static void TestingGetStringInvalidUTF8(byte[] dataUtf8)
        {
            foreach (JsonCommentHandling commentHandling in Enum.GetValues(typeof(JsonCommentHandling)))
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

                // It is expected that the Utf8JsonReader won't throw an exception here
                Assert.True(json.Read());
                Assert.Equal(JsonTokenType.String, json.TokenType);

                while (json.Read())
                    ;

                json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

                while (json.Read())
                {
                    if (json.TokenType == JsonTokenType.String)
                    {
                        try
                        {
                            string val = json.GetStringValue();
                            Assert.True(false, "Expected InvalidOperationException when trying to get string value for invalid UTF-8 JSON text.");
                        }
                        catch (InvalidOperationException ex)
                        {
                            Assert.Equal(ex.InnerException.GetType(), typeof(DecoderFallbackException));
                        }
                    }
                }
            }
        }
    }
}
