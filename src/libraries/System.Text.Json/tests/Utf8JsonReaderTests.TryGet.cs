// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Xunit;

namespace System.Text.Json.Tests
{
    public static partial class Utf8JsonReaderTests
    {
        [Fact]
        public static void TestingNumbers_TryGetMethods()
        {
            byte[] dataUtf8 = JsonNumberTestData.JsonData;
            List<int> ints = JsonNumberTestData.Ints;
            List<long> longs = JsonNumberTestData.Longs;
            List<uint> uints = JsonNumberTestData.UInts;
            List<ulong> ulongs = JsonNumberTestData.ULongs;
            List<float> floats = JsonNumberTestData.Floats;
            List<double> doubles = JsonNumberTestData.Doubles;
            List<decimal> decimals = JsonNumberTestData.Decimals;

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            string key = "";
            int count = 0;
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.PropertyName)
                {
                    key = json.GetString();
                }
                if (json.TokenType == JsonTokenType.Number)
                {
                    if (key.StartsWith("int"))
                    {
                        Assert.True(json.TryGetInt32(out int numberInt));
                        if (count >= ints.Count)
                            count = 0;
                        Assert.Equal(ints[count], numberInt);
                        count++;
                    }
                    else if (key.StartsWith("long"))
                    {
                        Assert.True(json.TryGetInt64(out long numberLong));
                        if (count >= longs.Count)
                            count = 0;
                        Assert.Equal(longs[count], numberLong);
                        count++;
                    }
                    else if (key.StartsWith("uint"))
                    {
                        Assert.True(json.TryGetUInt32(out uint numberUInt));
                        if (count >= ints.Count)
                            count = 0;
                        Assert.Equal(uints[count], numberUInt);
                        count++;
                    }
                    else if (key.StartsWith("ulong"))
                    {
                        Assert.True(json.TryGetUInt64(out ulong numberULong));
                        if (count >= ints.Count)
                            count = 0;
                        Assert.Equal(ulongs[count], numberULong);
                        count++;
                    }
                    else if (key.StartsWith("float"))
                    {
                        Assert.True(json.TryGetSingle(out float numberFloat));
                        if (count >= floats.Count)
                            count = 0;

                        var str = string.Format(CultureInfo.InvariantCulture, "{0}", floats[count]);
                        float expected = float.Parse(str, CultureInfo.InvariantCulture);

                        Assert.Equal(expected, numberFloat);
                        count++;
                    }
                    else if (key.StartsWith("double"))
                    {
                        Assert.True(json.TryGetDouble(out double numberDouble));
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
                        Assert.True(json.TryGetDecimal(out decimal numberDecimal));
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

        [Fact]
        public static void TestingNumbers_GetMethods()
        {
            byte[] dataUtf8 = JsonNumberTestData.JsonData;
            List<int> ints = JsonNumberTestData.Ints;
            List<long> longs = JsonNumberTestData.Longs;
            List<uint> uints = JsonNumberTestData.UInts;
            List<ulong> ulongs = JsonNumberTestData.ULongs;
            List<float> floats = JsonNumberTestData.Floats;
            List<double> doubles = JsonNumberTestData.Doubles;
            List<decimal> decimals = JsonNumberTestData.Decimals;

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            string key = "";
            int count = 0;
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.PropertyName)
                {
                    key = json.GetString();
                }
                if (json.TokenType == JsonTokenType.Number)
                {
                    if (key.StartsWith("int"))
                    {
                        if (count >= ints.Count)
                            count = 0;
                        Assert.Equal(ints[count], json.GetInt32());
                        count++;
                    }
                    else if (key.StartsWith("long"))
                    {
                        if (count >= longs.Count)
                            count = 0;
                        Assert.Equal(longs[count], json.GetInt64());
                        count++;
                    }
                    else if (key.StartsWith("uint"))
                    {
                        if (count >= uints.Count)
                            count = 0;
                        Assert.Equal(uints[count], json.GetUInt32());
                        count++;
                    }
                    else if (key.StartsWith("ulong"))
                    {
                        if (count >= ulongs.Count)
                            count = 0;
                        Assert.Equal(ulongs[count], json.GetUInt64());
                        count++;
                    }
                    else if (key.StartsWith("float"))
                    {
                        if (count >= floats.Count)
                            count = 0;

                        var str = string.Format(CultureInfo.InvariantCulture, "{0}", floats[count]);
                        float expected = float.Parse(str, CultureInfo.InvariantCulture);

                        Assert.Equal(expected, json.GetSingle());
                        count++;
                    }
                    else if (key.StartsWith("double"))
                    {
                        if (count >= doubles.Count)
                            count = 0;

                        string roundTripActual = json.GetDouble().ToString("R", CultureInfo.InvariantCulture);
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
                        try
                        {
                            if (count >= decimals.Count)
                                count = 0;

                            var str = string.Format(CultureInfo.InvariantCulture, "{0}", decimals[count]);
                            decimal expected = decimal.Parse(str, CultureInfo.InvariantCulture);
                            Assert.Equal(expected, json.GetDecimal());
                            count++;
                        }
                        catch (Exception except)
                        {
                            Assert.True(false, string.Format("Unexpected exception: {0}. Message: {1}", except.Source, except.Message));
                        }

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
        [InlineData("-2147483649", -2147483649)] // int.MinValue - 1
        public static void TestingNumbersInvalidConversionToInt32(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.False(json.TryGetInt32(out int value));
                    Assert.True(json.TryGetDouble(out double doubleValue));
                    Assert.Equal(expected, doubleValue);

                    try
                    {
                        json.GetInt32();
                        Assert.True(false, "Expected GetInt32 to throw FormatException.");
                    }
                    catch (FormatException)
                    {
                        /* Expected exception */
                    }
                    doubleValue = json.GetDouble();
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
        [InlineData("-9223372036854775809", -9223372036854775809d)] // long.MinValue - 1
        public static void TestingNumbersInvalidConversionToInt64(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.False(json.TryGetInt64(out long value));
                    Assert.True(json.TryGetDouble(out double doubleValue));
                    Assert.Equal(expected, doubleValue);

                    try
                    {
                        json.GetInt64();
                        Assert.True(false, "Expected GetInt64 to throw FormatException.");
                    }
                    catch (FormatException)
                    {
                        /* Expected exception */
                    }
                    doubleValue = json.GetDouble();
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
        [InlineData("12345678901", 12345678901)]
        [InlineData("123456789012345678901", 123456789012345678901d)]
        [InlineData("-1", -1)] // uint.MinValue - 1
        public static void TestingNumbersInvalidConversionToUInt32(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.False(json.TryGetUInt32(out uint value));
                    Assert.True(json.TryGetDouble(out double doubleValue));
                    Assert.Equal(expected, doubleValue);

                    try
                    {
                        json.GetUInt32();
                        Assert.True(false, "Expected GetUInt32 to throw FormatException.");
                    }
                    catch (FormatException)
                    {
                        /* Expected exception */
                    }
                    doubleValue = json.GetDouble();
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
        [InlineData("-1", -1)] // ulong.MinValue - 1
        public static void TestingNumbersInvalidConversionToUInt64(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.False(json.TryGetUInt64(out ulong value));
                    Assert.True(json.TryGetDouble(out double doubleValue));
                    Assert.Equal(expected, doubleValue);

                    try
                    {
                        json.GetUInt64();
                        Assert.True(false, "Expected GetInt64 to throw FormatException.");
                    }
                    catch (FormatException)
                    {
                        /* Expected exception */
                    }
                    doubleValue = json.GetDouble();
                    Assert.Equal(expected, doubleValue);
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [InlineData("-4.402823E+38", float.NegativeInfinity, -4.402823E+38)] // float.MinValue - 1
        [InlineData("4.402823E+38", float.PositiveInfinity, 4.402823E+38)]  // float.MaxValue + 1
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Utf8Parser does not support parsing really large float and double values to infinity.")]
        public static void TestingTooLargeSingleConversionToInfinity(string jsonString, float expectedFloat, double expectedDouble)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.True(json.TryGetSingle(out float floatValue));
                    Assert.Equal(expectedFloat, floatValue);
                    Assert.True(json.TryGetDouble(out double doubleValue));
                    Assert.Equal(expectedDouble, doubleValue);

                    Assert.Equal(expectedFloat, json.GetSingle());
                    Assert.Equal(expectedDouble, json.GetDouble());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [InlineData("-2.79769313486232E+308", double.NegativeInfinity)] // double.MinValue - 1
        [InlineData("2.79769313486232E+308", double.PositiveInfinity)]  // double.MaxValue + 1
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Utf8Parser does not support parsing really large float and double values to infinity.")]
        public static void TestingTooLargeDoubleConversionToInfinity(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.True(json.TryGetDouble(out double actual));
                    Assert.Equal(expected, actual);

                    Assert.Equal(expected, json.GetDouble());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [InlineData("-79228162514264337593543950336", -79228162514264337593543950336d)] // decimal.MinValue - 1
        [InlineData("79228162514264337593543950336", 79228162514264337593543950336d)]  // decimal.MaxValue + 1
        public static void TestingNumbersInvalidConversionToDecimal(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.False(json.TryGetDecimal(out decimal value));
                    Assert.True(json.TryGetDouble(out double doubleValue));
                    Assert.Equal(expected, doubleValue);

                    try
                    {
                        json.GetDecimal();
                        Assert.True(false, "Expected GetDecimal to throw FormatException.");
                    }
                    catch (FormatException)
                    {
                        /* Expected exception */
                    }
                    doubleValue = json.GetDouble();
                    Assert.Equal(expected, doubleValue);
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Fact]
        public static void InvalidConversion()
        {
            string jsonString = "[\"stringValue\", true, /* Comment within */ 1234] // Comment outside";
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);
            while (json.Read())
            {
                if (json.TokenType != JsonTokenType.String)
                {
                    try
                    {
                        string value = json.GetString();
                        Assert.True(false, "Expected GetString to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        DateTime value = json.GetDateTime();
                        Assert.True(false, "Expected GetDateTime to throw InvalidOperationException due to mismatched token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetDateTime(out DateTime value);
                        Assert.True(false, "Expected TryGetDateTime to throw InvalidOperationException due to mismatched token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        DateTimeOffset value = json.GetDateTimeOffset();
                        Assert.True(false, "Expected GetDateTimeOffset to throw InvalidOperationException due to mismatched token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetDateTimeOffset(out DateTimeOffset value);
                        Assert.True(false, "Expected TryGetDateTimeOffset to throw InvalidOperationException due to mismatched token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetGuid());

                    JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.TryGetGuid(out _));
                }

                if (json.TokenType != JsonTokenType.Comment)
                {
                    try
                    {
                        string value = json.GetComment();
                        Assert.True(false, "Expected GetComment to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }
                }

                if (json.TokenType != JsonTokenType.True && json.TokenType != JsonTokenType.False)
                {
                    try
                    {
                        bool value = json.GetBoolean();
                        Assert.True(false, "Expected GetBoolean to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }
                }

                if (json.TokenType != JsonTokenType.Number)
                {
                    try
                    {
                        json.TryGetInt32(out int value);
                        Assert.True(false, "Expected TryGetInt32 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.GetInt32();
                        Assert.True(false, "Expected GetInt32 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetInt64(out long value);
                        Assert.True(false, "Expected TryGetInt64 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.GetInt64();
                        Assert.True(false, "Expected GetInt64 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetUInt32(out uint value);
                        Assert.True(false, "Expected TryGetUInt32 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.GetUInt32();
                        Assert.True(false, "Expected GetUInt32 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetUInt64(out ulong value);
                        Assert.True(false, "Expected TryGetUInt64 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.GetUInt64();
                        Assert.True(false, "Expected GetUInt64 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetSingle(out float value);
                        Assert.True(false, "Expected TryGetSingle to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.GetSingle();
                        Assert.True(false, "Expected GetSingle to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetDouble(out double value);
                        Assert.True(false, "Expected TryGetDouble to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.GetDouble();
                        Assert.True(false, "Expected GetDouble to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetDecimal(out decimal value);
                        Assert.True(false, "Expected TryGetDecimal to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.GetDecimal();
                        Assert.True(false, "Expected GetDecimal to throw InvalidOperationException due to mismatch token type.");
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
                    actualValues.Add(json.GetString());
                }
                else if (json.TokenType == JsonTokenType.PropertyName)
                {
                    actualPropertyNames.Add(json.GetString());
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
                    string val = json.GetString();
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
                            string val = json.GetString();
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

        [Theory]
        [MemberData(nameof(GetCommentTestData))]
        public static void TestingGetComment(string jsonData, string expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonData);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            var reader = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);

            Assert.True(reader.Read());
            Assert.Equal(JsonTokenType.StartObject, reader.TokenType);

            Assert.True(reader.Read());
            Assert.Equal(JsonTokenType.Comment, reader.TokenType);
            Assert.Equal(expected, reader.GetComment());

            Assert.True(reader.Read());
            Assert.Equal(JsonTokenType.EndObject, reader.TokenType);

            Assert.False(reader.Read());
        }

        [Theory]
        [MemberData(nameof(GetCommentUnescapeData))]
        public static void TestGetCommentUnescape(string jsonData, string expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonData);
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            var reader = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);
            bool commentFound = false;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Comment:
                        commentFound = true;
                        string comment = reader.GetComment();
                        Assert.Equal(expected, comment);
                        Assert.NotEqual(Regex.Unescape(expected), comment);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
            Assert.True(commentFound);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.ValidISO8601Tests), MemberType = typeof(JsonDateTimeTestData))]
        public static void TestingStringsConversionToDateTime(string jsonString, string expectedString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    DateTime expected = DateTime.Parse(expectedString);

                    Assert.True(json.TryGetDateTime(out DateTime actual));
                    Assert.Equal(expected, actual);

                    Assert.Equal(expected, json.GetDateTime());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.ValidISO8601Tests), MemberType = typeof(JsonDateTimeTestData))]
        public static void TestingStringsConversionToDateTimeOffset(string jsonString, string expectedString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    DateTimeOffset expected = DateTimeOffset.Parse(expectedString);

                    Assert.True(json.TryGetDateTimeOffset(out DateTimeOffset actual));
                    Assert.Equal(expected, actual);

                    Assert.Equal(expected, json.GetDateTimeOffset());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.ValidISO8601TestsWithUtcOffset), MemberType = typeof(JsonDateTimeTestData))]
        public static void TestingStringsWithUTCOffsetToDateTime(string jsonString, string expectedString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    DateTime expected = DateTime.ParseExact(expectedString, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                    Assert.True(json.TryGetDateTime(out DateTime actual));
                    Assert.Equal(expected, actual);

                    Assert.Equal(expected, json.GetDateTime());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.ValidISO8601TestsWithUtcOffset), MemberType = typeof(JsonDateTimeTestData))]
        public static void TestingStringsWithUTCOffsetToDateTimeOffset(string jsonString, string expectedString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    DateTimeOffset expected = DateTimeOffset.ParseExact(expectedString, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

                    Assert.True(json.TryGetDateTimeOffset(out DateTimeOffset actual));
                    Assert.Equal(expected, actual);

                    Assert.Equal(expected, json.GetDateTimeOffset());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.InvalidISO8601Tests), MemberType = typeof(JsonDateTimeTestData))]
        public static void TestingStringsInvalidConversionToDateTime(string jsonString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                Assert.False(json.TryGetDateTime(out DateTime actualDateTime));

                try
                {
                    DateTime value = json.GetDateTime();
                    Assert.True(false, "Expected GetDateTime to throw FormatException due to invalid ISO 8601 input.");
                }
                catch (FormatException)
                { }
            }
        }

        [Theory]
        [MemberData(nameof(JsonDateTimeTestData.InvalidISO8601Tests), MemberType = typeof(JsonDateTimeTestData))]
        public static void TestingStringsInvalidConversionToDateTimeOffset(string jsonString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.String)
                {
                    Assert.False(json.TryGetDateTimeOffset(out DateTimeOffset actualDateTime));

                    try
                    {
                        DateTimeOffset value = json.GetDateTimeOffset();
                        Assert.True(false, "Expected GetDateTimeOffset to throw FormatException due to invalid ISO 8601 input.");
                    }
                    catch (FormatException)
                    { }
                }
            }
        }

        [Theory]
        [MemberData(nameof(JsonGuidTestData.ValidGuidTests), MemberType = typeof(JsonGuidTestData))]
        [MemberData(nameof(JsonGuidTestData.ValidHexGuidTests), MemberType = typeof(JsonGuidTestData))]
        public static void TestingStringsConversionToGuid(string testString, string expectedStr)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes($"\"{testString}\"");
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);

            Guid expected = new Guid(expectedStr);

            Assert.True(json.Read(), "Read string value");
            Assert.Equal(JsonTokenType.String, json.TokenType);

            Assert.True(json.TryGetGuid(out Guid actual));
            Assert.Equal(expected, actual);
            Assert.Equal(expected, json.GetGuid());

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
            Assert.Equal(json.BytesConsumed, json.CurrentState.BytesConsumed);
        }

        [Theory]
        [MemberData(nameof(JsonGuidTestData.InvalidGuidTests), MemberType = typeof(JsonGuidTestData))]
        public static void TestingStringsInvalidConversionToGuid(string testString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes($"\"{testString}\"");
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);

            Assert.True(json.Read(), "Read string value");
            Assert.Equal(JsonTokenType.String, json.TokenType);

            Assert.False(json.TryGetGuid(out Guid actual));
            Assert.Equal(Guid.Empty, actual);

            JsonTestHelper.AssertThrows<FormatException>(json, (jsonReader) => jsonReader.GetGuid());
        }
    }
}
