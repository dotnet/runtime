// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
            List<byte> bytes = JsonNumberTestData.Bytes;
            List<sbyte> sbytes = JsonNumberTestData.SBytes;
            List<short> shorts = JsonNumberTestData.Shorts;
            List<int> ints = JsonNumberTestData.Ints;
            List<long> longs = JsonNumberTestData.Longs;
            List<ushort> ushorts = JsonNumberTestData.UShorts;
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
                    if (key.StartsWith("byte"))
                    {
                        Assert.True(json.TryGetByte(out byte numberByte));
                        if (count >= bytes.Count)
                            count = 0;
                        Assert.Equal(bytes[count], numberByte);
                        count++;
                    }
                    else if (key.StartsWith("sbyte"))
                    {
                        Assert.True(json.TryGetSByte(out sbyte numberSByte));
                        if (count >= sbytes.Count)
                            count = 0;
                        Assert.Equal(sbytes[count], numberSByte);
                        count++;
                    }
                    else if (key.StartsWith("short"))
                    {
                        Assert.True(json.TryGetInt16(out short numberShort));
                        if (count >= shorts.Count)
                            count = 0;
                        Assert.Equal(shorts[count], numberShort);
                        count++;
                    }
                    else if (key.StartsWith("int"))
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
                    else if (key.StartsWith("ushort"))
                    {
                        Assert.True(json.TryGetUInt16(out ushort numberUShort));
                        if (count >= ushorts.Count)
                            count = 0;
                        Assert.Equal(ushorts[count], numberUShort);
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

                        string roundTripActual = numberFloat.ToString(JsonTestHelper.SingleFormatString, CultureInfo.InvariantCulture);
                        float actual = float.Parse(roundTripActual, CultureInfo.InvariantCulture);

                        string roundTripExpected = floats[count].ToString(JsonTestHelper.SingleFormatString, CultureInfo.InvariantCulture);
                        float expected = float.Parse(roundTripExpected, CultureInfo.InvariantCulture);

                        Assert.Equal(expected, actual);
                        count++;
                    }
                    else if (key.StartsWith("double"))
                    {
                        Assert.True(json.TryGetDouble(out double numberDouble));
                        if (count >= doubles.Count)
                            count = 0;

                        string roundTripActual = numberDouble.ToString(JsonTestHelper.DoubleFormatString, CultureInfo.InvariantCulture);
                        double actual = double.Parse(roundTripActual, CultureInfo.InvariantCulture);

                        string roundTripExpected = doubles[count].ToString(JsonTestHelper.DoubleFormatString, CultureInfo.InvariantCulture);
                        double expected = double.Parse(roundTripExpected, CultureInfo.InvariantCulture);

                        Assert.Equal(expected, actual);
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
        }

        [Fact]
        public static void TestingNumbers_GetMethods()
        {
            byte[] dataUtf8 = JsonNumberTestData.JsonData;
            List<byte> bytes = JsonNumberTestData.Bytes;
            List<sbyte> sbytes = JsonNumberTestData.SBytes;
            List<short> shorts = JsonNumberTestData.Shorts;
            List<int> ints = JsonNumberTestData.Ints;
            List<long> longs = JsonNumberTestData.Longs;
            List<ushort> ushorts = JsonNumberTestData.UShorts;
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
                    if (key.StartsWith("byte"))
                    {
                        if (count >= bytes.Count)
                            count = 0;
                        Assert.Equal(bytes[count], json.GetByte());
                        count++;
                    }
                    else if (key.StartsWith("sbyte"))
                    {
                        if (count >= sbytes.Count)
                            count = 0;
                        Assert.Equal(sbytes[count], json.GetSByte());
                        count++;
                    }
                    else if (key.StartsWith("short"))
                    {
                        if (count >= shorts.Count)
                            count = 0;
                        Assert.Equal(shorts[count], json.GetInt16());
                        count++;
                    }
                    else if (key.StartsWith("int"))
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
                    else if (key.StartsWith("ushort"))
                    {
                        if (count >= ushorts.Count)
                            count = 0;
                        Assert.Equal(ushorts[count], json.GetUInt16());
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

                        string roundTripActual = json.GetSingle().ToString(JsonTestHelper.SingleFormatString, CultureInfo.InvariantCulture);
                        float actual = float.Parse(roundTripActual, CultureInfo.InvariantCulture);

                        string roundTripExpected = floats[count].ToString(JsonTestHelper.SingleFormatString, CultureInfo.InvariantCulture);
                        float expected = float.Parse(roundTripExpected, CultureInfo.InvariantCulture);

                        Assert.Equal(expected, actual);
                        count++;
                    }
                    else if (key.StartsWith("double"))
                    {
                        if (count >= doubles.Count)
                            count = 0;

                        string roundTripActual = json.GetDouble().ToString(JsonTestHelper.DoubleFormatString, CultureInfo.InvariantCulture);
                        double actual = double.Parse(roundTripActual, CultureInfo.InvariantCulture);

                        string roundTripExpected = doubles[count].ToString(JsonTestHelper.DoubleFormatString, CultureInfo.InvariantCulture);
                        double expected = double.Parse(roundTripExpected, CultureInfo.InvariantCulture);

                        Assert.Equal(expected, actual);
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
        }

        [Theory]
        [InlineData("0.000", 0)]
        [InlineData("1e1", 10)]
        [InlineData("1.1e2", 110)]
        [InlineData("220.1", 220.1)]
        [InlineData("12345678901", 12345678901)]
        [InlineData("123456789012345678901", 123456789012345678901d)]
        [InlineData("-1", -1)] // byte.MinValue - 1
        public static void TestingNumbersInvalidConversionToByte(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.False(json.TryGetByte(out byte value));
                    Assert.Equal(default, value);
                    Assert.True(json.TryGetDouble(out double doubleValue));
                    Assert.Equal(expected, doubleValue);

                    try
                    {
                        json.GetByte();
                        Assert.True(false, "Expected GetByte to throw FormatException.");
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
        }

        [Theory]
        [InlineData("0.000", 0)]
        [InlineData("1e1", 10)]
        [InlineData("1.1e2", 110)]
        [InlineData("120.1", 120.1)]
        [InlineData("12345678901", 12345678901)]
        [InlineData("123456789012345678901", 123456789012345678901d)]
        [InlineData("-129", -129)] // sbyte.MinValue - 1
        public static void TestingNumbersInvalidConversionToSByte(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.False(json.TryGetSByte(out sbyte value));
                    Assert.Equal(default, value);
                    Assert.True(json.TryGetDouble(out double doubleValue));
                    Assert.Equal(expected, doubleValue);

                    try
                    {
                        json.GetSByte();
                        Assert.True(false, "Expected GetSByte to throw FormatException.");
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
        }

        [Theory]
        [InlineData("0.000", 0)]
        [InlineData("1e1", 10)]
        [InlineData("1.1e2", 110)]
        [InlineData("12345.1", 12345.1)]
        [InlineData("12345678901", 12345678901)]
        [InlineData("123456789012345678901", 123456789012345678901d)]
        [InlineData("-32769", -32769)] // short.MinValue - 1
        public static void TestingNumbersInvalidConversionToInt16(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.False(json.TryGetInt16(out short value));
                    Assert.Equal(default, value);
                    Assert.True(json.TryGetDouble(out double doubleValue));
                    Assert.Equal(expected, doubleValue);

                    try
                    {
                        json.GetInt16();
                        Assert.True(false, "Expected GetInt16 to throw FormatException.");
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
                    Assert.Equal(default, value);
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
                    Assert.Equal(default, value);
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
        }

        [Theory]
        [InlineData("0.000", 0)]
        [InlineData("1e1", 10)]
        [InlineData("1.1e2", 110)]
        [InlineData("12345.1", 12345.1)]
        [InlineData("12345678901", 12345678901)]
        [InlineData("123456789012345678901", 123456789012345678901d)]
        [InlineData("-1", -1)] // ushort.MinValue - 1
        public static void TestingNumbersInvalidConversionToUInt16(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    Assert.False(json.TryGetUInt16(out ushort value));
                    Assert.Equal(default, value);
                    Assert.True(json.TryGetDouble(out double doubleValue));
                    Assert.Equal(expected, doubleValue);

                    try
                    {
                        json.GetUInt16();
                        Assert.True(false, "Expected GetUInt16 to throw FormatException.");
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
                    Assert.Equal(default, value);
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
                    Assert.Equal(default, value);
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
        }

        [Theory]
        [InlineData("-4.402823E+38", float.NegativeInfinity, -4.402823E+38)] // float.MinValue - 1
        [InlineData("4.402823E+38", float.PositiveInfinity, 4.402823E+38)]  // float.MaxValue + 1
        public static void TestingTooLargeSingleConversionToInfinity(string jsonString, float expectedFloat, double expectedDouble)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    if (PlatformDetection.IsNetFramework)
                    {
                        // .NET Framework throws for overflow rather than returning Infinity
                        // This was fixed for .NET Core 3.0 in order to be IEEE 754 compliant

                        Assert.False(json.TryGetSingle(out float _));

                        try
                        {
                            json.GetSingle();
                            Assert.True(false, $"Expected {nameof(FormatException)}.");
                        }
                        catch (FormatException)
                        {
                            /* Expected exception */
                        }
                    }
                    else
                    {
                        Assert.True(json.TryGetSingle(out float floatValue));
                        Assert.Equal(expectedFloat, floatValue);

                        Assert.Equal(expectedFloat, json.GetSingle());
                    }

                    Assert.True(json.TryGetDouble(out double doubleValue));
                    Assert.Equal(expectedDouble, doubleValue);
                    Assert.Equal(expectedDouble, json.GetDouble());
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
        }

        [Theory]
        [InlineData("-2.79769313486232E+308", double.NegativeInfinity)] // double.MinValue - 1
        [InlineData("2.79769313486232E+308", double.PositiveInfinity)]  // double.MaxValue + 1
        public static void TestingTooLargeDoubleConversionToInfinity(string jsonString, double expected)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            while (json.Read())
            {
                if (json.TokenType == JsonTokenType.Number)
                {
                    if (PlatformDetection.IsNetFramework)
                    {
                        // .NET Framework throws for overflow rather than returning Infinity
                        // This was fixed for .NET Core 3.0 in order to be IEEE 754 compliant

                        Assert.False(json.TryGetDouble(out double _));

                        try
                        {
                            json.GetDouble();
                            Assert.True(false, $"Expected {nameof(FormatException)}.");
                        }
                        catch (FormatException)
                        {
                            /* Expected exception */
                        }
                    }
                    else
                    {
                        Assert.True(json.TryGetDouble(out double actual));
                        Assert.Equal(expected, actual);

                        Assert.Equal(expected, json.GetDouble());
                    }
                }
            }

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
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
                    Assert.Equal(default, value);
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
        }

        [Fact]
        public static void InvalidConversion()
        {
            string jsonString = "[\"stringValue\", true, /* Comment within */ 1234, null] // Comment outside";
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = JsonCommentHandling.Allow });
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state);
            while (json.Read())
            {
                if (json.TokenType != JsonTokenType.String)
                {
                    if (json.TokenType == JsonTokenType.Null)
                    {
                        Assert.Null(json.GetString());
                    }
                    else
                    {
                        JsonTestHelper.AssertThrows<InvalidOperationException>(json, (jsonReader) => jsonReader.GetString());
                    }

                    try
                    {
                        byte[] value = json.GetBytesFromBase64();
                        Assert.True(false, "Expected GetBytesFromBase64 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetBytesFromBase64(out byte[] value);
                        Assert.True(false, "Expected TryGetBytesFromBase64 to throw InvalidOperationException due to mismatch token type.");
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
                        json.TryGetByte(out byte value);
                        Assert.True(false, "Expected TryGetByte to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.GetByte();
                        Assert.True(false, "Expected GetByte to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetSByte(out sbyte value);
                        Assert.True(false, "Expected TryGetSByte to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.GetSByte();
                        Assert.True(false, "Expected GetSByte to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.TryGetInt16(out short value);
                        Assert.True(false, "Expected TryGetInt16 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.GetInt16();
                        Assert.True(false, "Expected GetInt16 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

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
                        json.TryGetUInt16(out ushort value);
                        Assert.True(false, "Expected TryGetUInt16 to throw InvalidOperationException due to mismatch token type.");
                    }
                    catch (InvalidOperationException)
                    { }

                    try
                    {
                        json.GetUInt16();
                        Assert.True(false, "Expected GetUInt16 to throw InvalidOperationException due to mismatch token type.");
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
                            Assert.Equal(typeof(DecoderFallbackException), ex.InnerException.GetType());
                        }
                    }
                }
            }
        }

        [Fact]
        public static void GetBase64Unescapes()
        {
            string jsonString = "\"\\u0031234\""; // equivalent to "\"1234\""

            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            Assert.True(json.Read());

            byte[] expected = Convert.FromBase64String("1234"); // new byte[3] { 215, 109, 248 }

            byte[] value = json.GetBytesFromBase64();
            Assert.Equal(expected, value);
            Assert.True(json.TryGetBytesFromBase64(out value));
            Assert.Equal(expected, value);
        }

        [Theory]
        [MemberData(nameof(JsonBase64TestData.ValidBase64Tests), MemberType = typeof(JsonBase64TestData))]
        public static void ValidBase64(string jsonString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            Assert.True(json.Read());

            byte[] expected = Convert.FromBase64String(jsonString.AsSpan(1, jsonString.Length - 2).ToString());

            byte[] value = json.GetBytesFromBase64();
            Assert.Equal(expected, value);
            Assert.True(json.TryGetBytesFromBase64(out value));
            Assert.Equal(expected, value);
        }

        [Theory]
        [MemberData(nameof(JsonBase64TestData.InvalidBase64Tests), MemberType = typeof(JsonBase64TestData))]
        public static void InvalidBase64(string jsonString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes(jsonString);

            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);
            Assert.True(json.Read());
            Assert.False(json.TryGetBytesFromBase64(out byte[] value));
            Assert.Null(value);

            try
            {
                byte[] val = json.GetBytesFromBase64();
                Assert.True(false, "Expected InvalidOperationException when trying to decode base 64 string for invalid UTF-16 JSON text.");
            }
            catch (FormatException) { }
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
        public static void TestingGetBase64InvalidUTF16(string jsonString)
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
                    byte[] val = json.GetBytesFromBase64();
                    Assert.True(false, "Expected InvalidOperationException when trying to decode base 64 string for invalid UTF-16 JSON text.");
                }
                catch (InvalidOperationException) { }

                try
                {
                    json.TryGetBytesFromBase64(out byte[] val);
                    Assert.True(false, "Expected InvalidOperationException when trying to decode base 64 string for invalid UTF-16 JSON text.");
                }
                catch (InvalidOperationException) { }
            }
        }

        [Theory]
        [MemberData(nameof(InvalidUTF8Strings))]
        public static void TestingGetBase64InvalidUTF8(byte[] dataUtf8)
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
                            byte[] val = json.GetBytesFromBase64();
                            Assert.True(false, "Expected InvalidOperationException when trying to decode base 64 string for invalid UTF-8 JSON text.");
                        }
                        catch (FormatException) { }

                        Assert.False(json.TryGetBytesFromBase64(out byte[] value));
                        Assert.Null(value);
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
        [MemberData(nameof(JsonGuidTestData.ValidGuidTests), MemberType = typeof(JsonGuidTestData))]
        [MemberData(nameof(JsonGuidTestData.ValidHexGuidTests), MemberType = typeof(JsonGuidTestData))]
        public static void TestingStringsConversionToGuid(string testString, string expectedString)
        {
            byte[] dataUtf8 = Encoding.UTF8.GetBytes($"\"{testString}\"");
            var json = new Utf8JsonReader(dataUtf8, isFinalBlock: true, state: default);

            Guid expected = new Guid(expectedString);

            Assert.True(json.Read(), "Read string value");
            Assert.Equal(JsonTokenType.String, json.TokenType);

            Assert.True(json.TryGetGuid(out Guid actual));
            Assert.Equal(expected, actual);
            Assert.Equal(expected, json.GetGuid());

            Assert.Equal(dataUtf8.Length, json.BytesConsumed);
        }

        [Theory]
        [MemberData(nameof(JsonGuidTestData.ValidGuidTests), MemberType = typeof(JsonGuidTestData))]
        public static void TryGetGuid_HasValueSequence_RetrievesGuid(string testString, string expectedString)
        {
            static void test(string testString, string expectedString, bool isFinalBlock)
            {
                byte[] dataUtf8 = Encoding.UTF8.GetBytes($"\"{testString}\"");
                ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, 1);
                var json = new Utf8JsonReader(sequence, isFinalBlock: isFinalBlock, state: default);

                Guid expected = new Guid(expectedString);

                Assert.True(json.Read(), "json.Read()");
                Assert.Equal(JsonTokenType.String, json.TokenType);

                Assert.True(json.HasValueSequence, "json.HasValueSequence");
                Assert.False(json.ValueSequence.IsEmpty, "json.ValueSequence.IsEmpty");
                Assert.True(json.ValueSpan.IsEmpty, "json.ValueSpan.IsEmpty");
                Assert.True(json.TryGetGuid(out Guid actual), "TryGetGuid");
                Assert.Equal(expected, actual);
                Assert.Equal(expected, json.GetGuid());
            }

            test(testString, expectedString, isFinalBlock: true);
            test(testString, expectedString, isFinalBlock: false);
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
            Assert.Equal(default, actual);

            JsonTestHelper.AssertThrows<FormatException>(json, (jsonReader) => jsonReader.GetGuid());
        }

        [Theory]
        [MemberData(nameof(JsonGuidTestData.InvalidGuidTests), MemberType = typeof(JsonGuidTestData))]
        public static void TryGetGuid_HasValueSequence_False(string testString)
        {
            static void test(string testString, bool isFinalBlock)
            {
                byte[] dataUtf8 = Encoding.UTF8.GetBytes($"\"{testString}\"");
                ReadOnlySequence<byte> sequence = JsonTestHelper.GetSequence(dataUtf8, 1);
                var json = new Utf8JsonReader(sequence, isFinalBlock: isFinalBlock, state: default);

                Assert.True(json.Read(), "json.Read()");
                Assert.Equal(JsonTokenType.String, json.TokenType);
                Assert.True(json.HasValueSequence, "json.HasValueSequence");
                // If the string is empty, the ValueSequence is empty, because it contains all 0 bytes between the two characters
                Assert.Equal(string.IsNullOrEmpty(testString), json.ValueSequence.IsEmpty);
                Assert.False(json.TryGetGuid(out Guid actual), "json.TryGetGuid(out Guid actual)");
                Assert.Equal(Guid.Empty, actual);

                JsonTestHelper.AssertThrows<FormatException>(json, (jsonReader) => jsonReader.GetGuid());
            }

            test(testString, isFinalBlock: true);
            test(testString, isFinalBlock: false);
        }
    }
}
