// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json.Tests;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class NumberHandlingTests
    {
        private static readonly JsonSerializerOptions s_optionReadFromStr = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private static readonly JsonSerializerOptions s_optionWriteAsStr = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.WriteAsString
        };

        private static readonly JsonSerializerOptions s_optionReadAndWriteFromStr = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
        };

        private static readonly JsonSerializerOptions s_optionsAllowFloatConstants = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        private static readonly JsonSerializerOptions s_optionReadFromStrAllowFloatConstants = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        private static readonly JsonSerializerOptions s_optionWriteAsStrAllowFloatConstants = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        [Fact]
        public static void Number_AsRootType_RoundTrip()
        {
            RunAsRootTypeTest(JsonNumberTestData.Bytes);
            RunAsRootTypeTest(JsonNumberTestData.SBytes);
            RunAsRootTypeTest(JsonNumberTestData.Shorts);
            RunAsRootTypeTest(JsonNumberTestData.Ints);
            RunAsRootTypeTest(JsonNumberTestData.Longs);
            RunAsRootTypeTest(JsonNumberTestData.UShorts);
            RunAsRootTypeTest(JsonNumberTestData.UInts);
            RunAsRootTypeTest(JsonNumberTestData.ULongs);
            RunAsRootTypeTest(JsonNumberTestData.Floats);
            RunAsRootTypeTest(JsonNumberTestData.Doubles);
            RunAsRootTypeTest(JsonNumberTestData.Decimals);
            RunAsRootTypeTest(JsonNumberTestData.NullableBytes);
            RunAsRootTypeTest(JsonNumberTestData.NullableSBytes);
            RunAsRootTypeTest(JsonNumberTestData.NullableShorts);
            RunAsRootTypeTest(JsonNumberTestData.NullableInts);
            RunAsRootTypeTest(JsonNumberTestData.NullableLongs);
            RunAsRootTypeTest(JsonNumberTestData.NullableUShorts);
            RunAsRootTypeTest(JsonNumberTestData.NullableUInts);
            RunAsRootTypeTest(JsonNumberTestData.NullableULongs);
            RunAsRootTypeTest(JsonNumberTestData.NullableFloats);
            RunAsRootTypeTest(JsonNumberTestData.NullableDoubles);
            RunAsRootTypeTest(JsonNumberTestData.NullableDecimals);
        }

        private static void RunAsRootTypeTest<T>(List<T> numbers)
        {
            foreach (T number in numbers)
            {
                string numberAsString = GetNumberAsString(number);
                string json = $"{numberAsString}";
                string jsonWithNumberAsString = @$"""{numberAsString}""";
                PerformAsRootTypeSerialization(number, json, jsonWithNumberAsString);
            }
        }

        private static string GetNumberAsString<T>(T number)
        {
            return number switch
            {
                double @double => @double.ToString(JsonTestHelper.DoubleFormatString, CultureInfo.InvariantCulture),
                float @float => @float.ToString(JsonTestHelper.SingleFormatString, CultureInfo.InvariantCulture),
                decimal @decimal => @decimal.ToString(CultureInfo.InvariantCulture),
                _ => number.ToString()
            };
        }

        private static void PerformAsRootTypeSerialization<T>(T number, string jsonWithNumberAsNumber, string jsonWithNumberAsString)
        {
            // Option: read from string

            // Deserialize
            Assert.Equal(number, JsonSerializer.Deserialize<T>(jsonWithNumberAsNumber, s_optionReadFromStr));
            Assert.Equal(number, JsonSerializer.Deserialize<T>(jsonWithNumberAsString, s_optionReadFromStr));

            // Serialize
            Assert.Equal(jsonWithNumberAsNumber, JsonSerializer.Serialize(number, s_optionReadFromStr));

            // Option: write as string

            // Deserialize
            Assert.Equal(number, JsonSerializer.Deserialize<T>(jsonWithNumberAsNumber, s_optionWriteAsStr));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>(jsonWithNumberAsString, s_optionWriteAsStr));

            // Serialize
            Assert.Equal(jsonWithNumberAsString, JsonSerializer.Serialize(number, s_optionWriteAsStr));

            // Option: read and write from/to string

            // Deserialize
            Assert.Equal(number, JsonSerializer.Deserialize<T>(jsonWithNumberAsNumber, s_optionReadAndWriteFromStr));
            Assert.Equal(number, JsonSerializer.Deserialize<T>(jsonWithNumberAsString, s_optionReadAndWriteFromStr));

            // Serialize
            Assert.Equal(jsonWithNumberAsString, JsonSerializer.Serialize(number, s_optionReadAndWriteFromStr));
        }

        [Fact]
        public static void Number_AsCollectionElement_RoundTrip()
        {
            RunAsCollectionElementTest(JsonNumberTestData.Bytes);
            RunAsCollectionElementTest(JsonNumberTestData.SBytes);
            RunAsCollectionElementTest(JsonNumberTestData.Shorts);
            RunAsCollectionElementTest(JsonNumberTestData.Ints);
            RunAsCollectionElementTest(JsonNumberTestData.Longs);
            RunAsCollectionElementTest(JsonNumberTestData.UShorts);
            RunAsCollectionElementTest(JsonNumberTestData.UInts);
            RunAsCollectionElementTest(JsonNumberTestData.ULongs);
            RunAsCollectionElementTest(JsonNumberTestData.Floats);
            RunAsCollectionElementTest(JsonNumberTestData.Doubles);
            RunAsCollectionElementTest(JsonNumberTestData.Decimals);
            RunAsCollectionElementTest(JsonNumberTestData.NullableBytes);
            RunAsCollectionElementTest(JsonNumberTestData.NullableSBytes);
            RunAsCollectionElementTest(JsonNumberTestData.NullableShorts);
            RunAsCollectionElementTest(JsonNumberTestData.NullableInts);
            RunAsCollectionElementTest(JsonNumberTestData.NullableLongs);
            RunAsCollectionElementTest(JsonNumberTestData.NullableUShorts);
            RunAsCollectionElementTest(JsonNumberTestData.NullableUInts);
            RunAsCollectionElementTest(JsonNumberTestData.NullableULongs);
            RunAsCollectionElementTest(JsonNumberTestData.NullableFloats);
            RunAsCollectionElementTest(JsonNumberTestData.NullableDoubles);
            RunAsCollectionElementTest(JsonNumberTestData.NullableDecimals);
        }

        private static void RunAsCollectionElementTest<T>(List<T> numbers)
        {
            StringBuilder jsonBuilder_NumbersAsNumbers = new StringBuilder();
            StringBuilder jsonBuilder_NumbersAsStrings = new StringBuilder();
            StringBuilder jsonBuilder_NumbersAsNumbersAndStrings = new StringBuilder();
            StringBuilder jsonBuilder_NumbersAsNumbersAndStrings_Alternate = new StringBuilder();
            bool asNumber = false;

            jsonBuilder_NumbersAsNumbers.Append("[");
            jsonBuilder_NumbersAsStrings.Append("[");
            jsonBuilder_NumbersAsNumbersAndStrings.Append("[");
            jsonBuilder_NumbersAsNumbersAndStrings_Alternate.Append("[");

            foreach (T number in numbers)
            {
                string numberAsString = GetNumberAsString(number);

                string jsonWithNumberAsString = @$"""{numberAsString}""";

                jsonBuilder_NumbersAsNumbers.Append($"{numberAsString},");
                jsonBuilder_NumbersAsStrings.Append($"{jsonWithNumberAsString},");
                jsonBuilder_NumbersAsNumbersAndStrings.Append(asNumber
                    ? $"{numberAsString},"
                    : $"{jsonWithNumberAsString},");
                jsonBuilder_NumbersAsNumbersAndStrings_Alternate.Append(!asNumber
                    ? $"{numberAsString},"
                    : $"{jsonWithNumberAsString},");

                asNumber = !asNumber;
            }

            jsonBuilder_NumbersAsNumbers.Remove(jsonBuilder_NumbersAsNumbers.Length - 1, 1);
            jsonBuilder_NumbersAsStrings.Remove(jsonBuilder_NumbersAsStrings.Length - 1, 1);
            jsonBuilder_NumbersAsNumbersAndStrings.Remove(jsonBuilder_NumbersAsNumbersAndStrings.Length - 1, 1);
            jsonBuilder_NumbersAsNumbersAndStrings_Alternate.Remove(jsonBuilder_NumbersAsNumbersAndStrings_Alternate.Length - 1, 1);

            jsonBuilder_NumbersAsNumbers.Append("]");
            jsonBuilder_NumbersAsStrings.Append("]");
            jsonBuilder_NumbersAsNumbersAndStrings.Append("]");
            jsonBuilder_NumbersAsNumbersAndStrings_Alternate.Append("]");

            PerformAsCollectionElementSerialization(
                numbers,
                jsonBuilder_NumbersAsNumbers.ToString(),
                jsonBuilder_NumbersAsStrings.ToString(),
                jsonBuilder_NumbersAsNumbersAndStrings.ToString(),
                jsonBuilder_NumbersAsNumbersAndStrings_Alternate.ToString());
        }

        private static void PerformAsCollectionElementSerialization<T>(
            List<T> numbers,
            string json_NumbersAsNumbers,
            string json_NumbersAsStrings,
            string json_NumbersAsNumbersAndStrings,
            string json_NumbersAsNumbersAndStrings_Alternate)
        {
            // Option: read from string

            // Deserialize
            List<T> deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbers, s_optionReadFromStr);
            AssertListsEqual(numbers, deserialized);

            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsStrings, s_optionReadFromStr);
            AssertListsEqual(numbers, deserialized);

            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbersAndStrings, s_optionReadFromStr);
            AssertListsEqual(numbers, deserialized);

            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbersAndStrings_Alternate, s_optionReadFromStr);
            AssertListsEqual(numbers, deserialized);

            // Serialize
            Assert.Equal(json_NumbersAsNumbers, JsonSerializer.Serialize(numbers, s_optionReadFromStr));

            // Option: write as string

            // Deserialize
            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbers, s_optionWriteAsStr);
            AssertListsEqual(numbers, deserialized);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<T>>(json_NumbersAsStrings, s_optionWriteAsStr));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbersAndStrings, s_optionWriteAsStr));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbersAndStrings_Alternate, s_optionWriteAsStr));

            // Serialize
            Assert.Equal(json_NumbersAsStrings, JsonSerializer.Serialize(numbers, s_optionWriteAsStr));

            // Option: read and write from/to string

            // Deserialize
            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbers, s_optionReadAndWriteFromStr);
            AssertListsEqual(numbers, deserialized);

            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsStrings, s_optionReadAndWriteFromStr);
            AssertListsEqual(numbers, deserialized);

            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbersAndStrings, s_optionReadAndWriteFromStr);
            AssertListsEqual(numbers, deserialized);

            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbersAndStrings_Alternate, s_optionReadAndWriteFromStr);
            AssertListsEqual(numbers, deserialized);

            // Serialize
            Assert.Equal(json_NumbersAsStrings, JsonSerializer.Serialize(numbers, s_optionReadAndWriteFromStr));
        }

        private static void AssertListsEqual<T>(List<T> list1, List<T> list2)
        {
            Assert.Equal(list1.Count, list2.Count);
            for (int i = 0; i < list1.Count; i++)
            {
                Assert.Equal(list1[i], list2[i]);
            }
        }

        [Fact]
        public static void Number_AsDictionaryElement_RoundTrip()
        {
            var dict = new Dictionary<int, float>();
            for (int i = 0; i < 10; i++)
            {
                dict[JsonNumberTestData.Ints[i]] = JsonNumberTestData.Floats[i];
            }

            // Serialize
            string serialized = JsonSerializer.Serialize(dict, s_optionReadAndWriteFromStr);

            // Deserialize
            dict = JsonSerializer.Deserialize<Dictionary<int, float>>(serialized, s_optionReadAndWriteFromStr);

            // Test roundtrip
            JsonTestHelper.AssertJsonEqual(serialized, JsonSerializer.Serialize(dict, s_optionReadAndWriteFromStr));
        }

        [Fact]
        public static void Number_AsPropertyValue_RoundTrip()
        {
            var obj = new Class_With_NullableUInt64_And_Float()
            {
                NullableUInt64Number = JsonNumberTestData.NullableULongs.LastOrDefault(),
                FloatNumbers = JsonNumberTestData.Floats
            };

            // Serialize
            string serialized = JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr);

            // Deserialize
            obj = JsonSerializer.Deserialize<Class_With_NullableUInt64_And_Float>(serialized, s_optionReadAndWriteFromStr);

            // Test roundtrip
            JsonTestHelper.AssertJsonEqual(serialized, JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr));
        }

        private class Class_With_NullableUInt64_And_Float
        {
            public ulong? NullableUInt64Number { get; set; }
            [JsonInclude]
            public List<float> FloatNumbers;
        }

        [Fact]
        public static void FloatingPointConstants()
        {
            // Valid values
            PerformFloatingPointSerialization("NaN", "NaN");
            PerformFloatingPointSerialization("Infinity", "Infinity");
            PerformFloatingPointSerialization("-Infinity", "-Infinity");

            // Invalid values
            PerformFloatingPointSerialization("naN");
            PerformFloatingPointSerialization("Nan");
            PerformFloatingPointSerialization("NAN");

            PerformFloatingPointSerialization("+Infinity");
            PerformFloatingPointSerialization("+infinity");
            PerformFloatingPointSerialization("infinity");
            PerformFloatingPointSerialization("infinitY");
            PerformFloatingPointSerialization("INFINITY");
            PerformFloatingPointSerialization("+INFINITY");

            PerformFloatingPointSerialization("-infinity");
            PerformFloatingPointSerialization("-infinitY");
            PerformFloatingPointSerialization("-INFINITY");

            PerformFloatingPointSerialization(" NaN");
            PerformFloatingPointSerialization(" Infinity");
            PerformFloatingPointSerialization(" -Infinity");
            PerformFloatingPointSerialization("NaN ");
            PerformFloatingPointSerialization("Infinity ");
            PerformFloatingPointSerialization("-Infinity ");
            PerformFloatingPointSerialization("a-Infinity");
            PerformFloatingPointSerialization("NaNa");
            PerformFloatingPointSerialization("Infinitya");
            PerformFloatingPointSerialization("-Infinitya");
        }

        private static void PerformFloatingPointSerialization(string testString, string constantAsString = null)
        {
            string constantAsJson = $@"""{constantAsString}""";
            string expectedJson = @$"{{""FloatNumber"":{constantAsJson},""DoubleNumber"":{constantAsJson}}}";

            string testStringAsJson = $@"""{testString}""";
            string testJson = @$"{{""FloatNumber"":{testStringAsJson},""DoubleNumber"":{testStringAsJson}}}";

            StructWithNumbers obj;
            switch (constantAsString)
            {
                case "NaN":
                    obj = JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionsAllowFloatConstants);
                    Assert.Equal(float.NaN, obj.FloatNumber);
                    Assert.Equal(double.NaN, obj.DoubleNumber);
                    break;
                case "Infinity":
                    obj = JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionsAllowFloatConstants);
                    Assert.Equal(float.PositiveInfinity, obj.FloatNumber);
                    Assert.Equal(double.PositiveInfinity, obj.DoubleNumber);
                    break;
                case "-Infinity":
                    obj = JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionsAllowFloatConstants);
                    Assert.Equal(float.NegativeInfinity, obj.FloatNumber);
                    Assert.Equal(double.NegativeInfinity, obj.DoubleNumber);
                    break;
                default:
                    Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionsAllowFloatConstants));
                    return;
            }

            JsonTestHelper.AssertJsonEqual(expectedJson, JsonSerializer.Serialize(obj, s_optionsAllowFloatConstants));
        }

        private struct StructWithNumbers
        {
            public float FloatNumber { get; set; }
            public double DoubleNumber { get; set; }
        }

        [Fact]
        public static void ReadFromString_AllowFloatingPoint()
        {
            string json = @"{""IntNumber"":""1"",""FloatNumber"":""NaN""}";
            ClassWithNumbers obj = JsonSerializer.Deserialize<ClassWithNumbers>(json, s_optionReadFromStrAllowFloatConstants);

            Assert.Equal(1, obj.IntNumber);
            Assert.Equal(float.NaN, obj.FloatNumber);

            JsonTestHelper.AssertJsonEqual(@"{""IntNumber"":1,""FloatNumber"":""NaN""}", JsonSerializer.Serialize(obj, s_optionReadFromStrAllowFloatConstants));
        }

        [Fact]
        public static void WriteAsString_AllowFloatingPoint()
        {
            string json = @"{""IntNumber"":""1"",""FloatNumber"":""NaN""}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithNumbers>(json, s_optionWriteAsStrAllowFloatConstants));

            var obj = new ClassWithNumbers
            {
                IntNumber = 1,
                FloatNumber = float.NaN
            };

            JsonTestHelper.AssertJsonEqual(json, JsonSerializer.Serialize(obj, s_optionWriteAsStrAllowFloatConstants));
        }

        public class ClassWithNumbers
        {
            public int IntNumber { get; set; }
            public float FloatNumber { get; set; }
        }

        [Fact]
        public static void FloatingPointConstants_IncompatibleNumber()
        {
            AssertFloatingPointIncompatible_Fails<byte>();
            AssertFloatingPointIncompatible_Fails<sbyte>();
            AssertFloatingPointIncompatible_Fails<short>();
            AssertFloatingPointIncompatible_Fails<int>();
            AssertFloatingPointIncompatible_Fails<long>();
            AssertFloatingPointIncompatible_Fails<ushort>();
            AssertFloatingPointIncompatible_Fails<uint>();
            AssertFloatingPointIncompatible_Fails<ulong>();
            AssertFloatingPointIncompatible_Fails<float>();
            AssertFloatingPointIncompatible_Fails<decimal>();
            AssertFloatingPointIncompatible_Fails<byte?>();
            AssertFloatingPointIncompatible_Fails<sbyte?>();
            AssertFloatingPointIncompatible_Fails<short?>();
            AssertFloatingPointIncompatible_Fails<int?>();
            AssertFloatingPointIncompatible_Fails<long?>();
            AssertFloatingPointIncompatible_Fails<ushort?>();
            AssertFloatingPointIncompatible_Fails<uint?>();
            AssertFloatingPointIncompatible_Fails<ulong?>();
            AssertFloatingPointIncompatible_Fails<float?>();
            AssertFloatingPointIncompatible_Fails<decimal?>();
        }

        private static void AssertFloatingPointIncompatible_Fails<T>()
        {
            string[] testCases = new[]
            {
                "NaN",
                "Infinity",
                "-Infinity",
            };

            foreach (string test in testCases)
            {
                Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>(test, s_optionReadFromStrAllowFloatConstants));
            }
        }

        [Fact]
        public static void UnsupportedFormats()
        {
            AssertUnsupportedFormatThrows<byte>();
            AssertUnsupportedFormatThrows<sbyte>();
            AssertUnsupportedFormatThrows<short>();
            AssertUnsupportedFormatThrows<int>();
            AssertUnsupportedFormatThrows<long>();
            AssertUnsupportedFormatThrows<ushort>();
            AssertUnsupportedFormatThrows<uint>();
            AssertUnsupportedFormatThrows<ulong>();
            AssertUnsupportedFormatThrows<float>();
            AssertUnsupportedFormatThrows<decimal>();
            AssertUnsupportedFormatThrows<byte?>();
            AssertUnsupportedFormatThrows<sbyte?>();
            AssertUnsupportedFormatThrows<short?>();
            AssertUnsupportedFormatThrows<int?>();
            AssertUnsupportedFormatThrows<long?>();
            AssertUnsupportedFormatThrows<ushort?>();
            AssertUnsupportedFormatThrows<uint?>();
            AssertUnsupportedFormatThrows<ulong?>();
            AssertUnsupportedFormatThrows<float?>();
            AssertUnsupportedFormatThrows<decimal?>();
        }

        private static void AssertUnsupportedFormatThrows<T>()
        {
            string[] testCases = new[]
            {
                " $123.46", // Currency
                "100.00 %", // Percent
                 "1234,57", // Fixed point
                 "00FF", // Hexadecimal
            };

            foreach (string test in testCases)
            {
                Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>(test, s_optionReadFromStr));
            }
        }

        [Fact]
        public static void CustomConverterOverridesBuiltInLogic()
        {
            var options = new JsonSerializerOptions(s_optionReadAndWriteFromStr)
            {
                Converters = { new ConverterForInt32() }
            };

            string json = @"""32""";

            // Converter returns 25 regardless of input.
            Assert.Equal(25, JsonSerializer.Deserialize<int>(json, options));

            // Converter throws this exception regardless of input.
            Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(4, options));
        }

        [Fact]
        public static void EncodingTest()
        {
            // Cause all characters to be escaped.
            var encoderSettings = new TextEncoderSettings();
            encoderSettings.ForbidCharacters('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '+', '-', 'e', 'E');

            JavaScriptEncoder encoder = JavaScriptEncoder.Create(encoderSettings);
            var options = new JsonSerializerOptions(s_optionReadAndWriteFromStr)
            {
                Encoder = encoder
            };

            PerformEncodingTest(JsonNumberTestData.Bytes, options);
            PerformEncodingTest(JsonNumberTestData.SBytes, options);
            PerformEncodingTest(JsonNumberTestData.Shorts, options);
            PerformEncodingTest(JsonNumberTestData.Ints, options);
            PerformEncodingTest(JsonNumberTestData.Longs, options);
            PerformEncodingTest(JsonNumberTestData.UShorts, options);
            PerformEncodingTest(JsonNumberTestData.UInts, options);
            PerformEncodingTest(JsonNumberTestData.ULongs, options);
            PerformEncodingTest(JsonNumberTestData.Floats, options);
            PerformEncodingTest(JsonNumberTestData.Doubles, options);
            PerformEncodingTest(JsonNumberTestData.Decimals, options);
        }

        private static void PerformEncodingTest<T>(List<T> numbers, JsonSerializerOptions options)
        {
            // All input characters are escaped
            IEnumerable<string> numbersAsStrings = numbers.Select(num => GetNumberAsString(num));
            string input = JsonSerializer.Serialize(numbersAsStrings, options);

            // Unescaping works
            List<T> deserialized = JsonSerializer.Deserialize<List<T>>(input, options);
            Assert.Equal(numbers.Count, deserialized.Count);
            for (int i = 0; i < numbers.Count; i++)
            {
                Assert.Equal(numbers[i], deserialized[i]);
            }

            // Every number is written as a string, and custom escaping is not honored.
            string serialized = JsonSerializer.Serialize(deserialized, options);
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(serialized));
            reader.Read();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }
                else
                {
                    Assert.Equal(JsonTokenType.String, reader.TokenType);
#if BUILDING_INBOX_LIBRARY
                    Assert.False(reader.ValueSpan.Contains((byte)'\\'));
#else
                    foreach (byte val in reader.ValueSpan)
                    {
                        if (val == (byte)'\\')
                        {
                            Assert.True(false, "Unexpected escape token.");
                        }
                    }
#endif
                }
            }
        }

        [Fact]
        public static void NullableNumber_RoundtripNull()
        {
            PerformNullabilityTest<byte>();
            PerformNullabilityTest<sbyte>();
            PerformNullabilityTest<short>();
            PerformNullabilityTest<int>();
            PerformNullabilityTest<long>();
            PerformNullabilityTest<ushort>();
            PerformNullabilityTest<uint>();
            PerformNullabilityTest<ulong>();
            PerformNullabilityTest<float>();
            PerformNullabilityTest<decimal>();
            PerformNullabilityTest<byte?>();
            PerformNullabilityTest<sbyte?>();
            PerformNullabilityTest<short?>();
            PerformNullabilityTest<int?>();
            PerformNullabilityTest<long?>();
            PerformNullabilityTest<ushort?>();
            PerformNullabilityTest<uint?>();
            PerformNullabilityTest<ulong?>();
            PerformNullabilityTest<float?>();
            PerformNullabilityTest<decimal?>();
        }

        private static void PerformNullabilityTest<T>()
        {
            string nullAsJson = "null";
            string nullAsQuotedJson = $@"""{nullAsJson}""";

            if (Nullable.GetUnderlyingType(typeof(T)) != null)
            {
                Assert.Null(JsonSerializer.Deserialize<T>(nullAsJson, s_optionReadAndWriteFromStr));
                Assert.Equal(nullAsJson, JsonSerializer.Serialize(default(T)));
            }
            else
            {
                Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>(nullAsJson, s_optionReadAndWriteFromStr));
                Assert.Equal("0", JsonSerializer.Serialize(default(T)));
            }

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>(nullAsQuotedJson, s_optionReadAndWriteFromStr));
        }

        [Fact]
        public static void ConvertersHaveNullChecks()
        {
            RunConvertersHaveNullChecksTest(JsonNumberTestData.Bytes[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.SBytes[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.Shorts[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.Ints[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.Longs[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.UShorts[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.UInts[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.ULongs[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.Floats[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.Doubles[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.Decimals[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.NullableBytes[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.NullableSBytes[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.NullableShorts[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.NullableInts[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.NullableLongs[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.NullableUShorts[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.NullableUInts[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.NullableULongs[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.NullableFloats[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.NullableDoubles[0]);
            RunConvertersHaveNullChecksTest(JsonNumberTestData.NullableDecimals[0]);
        }

        private static void RunConvertersHaveNullChecksTest<T>(T number)
        {
            string numberAsJsonNumber = $"{number}";
            string numberAsJsonString = $@"""{number}""";
            var options = new JsonSerializerOptions();

            var converter = (JsonConverter<T>)options.GetConverter(typeof(T));

            var reader_JsonNumber = new Utf8JsonReader(Encoding.UTF8.GetBytes(numberAsJsonNumber));
            var reader_JsonString = new Utf8JsonReader(Encoding.UTF8.GetBytes(numberAsJsonString));

            reader_JsonNumber.Read();
            reader_JsonString.Read();

            T val = converter.Read(ref reader_JsonNumber, typeof(T), options: null);
            Assert.Equal(number, val);

            try
            {
                converter.Read(ref reader_JsonString, typeof(T), options: null);
                Assert.True(false, "InvalidOperationException expected.");
            }
            catch (InvalidOperationException) { }

            using (MemoryStream stream = new MemoryStream())
            {
                using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
                {
                    converter.Write(writer, number, options: null);
                }
                Assert.Equal(numberAsJsonNumber, Encoding.UTF8.GetString(stream.ToArray()));
            }
        }
    }
}
