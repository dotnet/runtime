// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json.Tests;
using System.Threading.Tasks;
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
        public static void Number_AsBoxed_RootType()
        {
            string numberAsString = @"""2""";

            int @int = 2;
            float @float = 2;
            int? nullableInt = 2;
            float? nullableFloat = 2;

            Assert.Equal(numberAsString, JsonSerializer.Serialize((object)@int, s_optionReadAndWriteFromStr));
            Assert.Equal(numberAsString, JsonSerializer.Serialize((object)@float, s_optionReadAndWriteFromStr));
            Assert.Equal(numberAsString, JsonSerializer.Serialize((object)nullableInt, s_optionReadAndWriteFromStr));
            Assert.Equal(numberAsString, JsonSerializer.Serialize((object)nullableFloat, s_optionReadAndWriteFromStr));

            Assert.Equal(2, (int)JsonSerializer.Deserialize(numberAsString, typeof(int), s_optionReadAndWriteFromStr));
            Assert.Equal(2, (float)JsonSerializer.Deserialize(numberAsString, typeof(float), s_optionReadAndWriteFromStr));
            Assert.Equal(2, (int?)JsonSerializer.Deserialize(numberAsString, typeof(int?), s_optionReadAndWriteFromStr));
            Assert.Equal(2, (float?)JsonSerializer.Deserialize(numberAsString, typeof(float?), s_optionReadAndWriteFromStr));
        }

        [Fact]
        public static void Number_AsBoxed_Property()
        {
            int @int = 1;
            float? nullableFloat = 2;

            string expected = @"{""MyInt"":""1"",""MyNullableFloat"":""2""}";

            var obj = new Class_With_BoxedNumbers
            {
                MyInt = @int,
                MyNullableFloat = nullableFloat
            };

            string serialized = JsonSerializer.Serialize(obj);
            JsonTestHelper.AssertJsonEqual(expected, serialized);

            obj = JsonSerializer.Deserialize<Class_With_BoxedNumbers>(serialized);

            JsonElement el = Assert.IsType<JsonElement>(obj.MyInt);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal("1", el.GetString());

            el = Assert.IsType<JsonElement>(obj.MyNullableFloat);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal("2", el.GetString());
        }

        public class Class_With_BoxedNumbers
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public object MyInt { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public object MyNullableFloat { get; set; }
        }

        [Fact]
        public static void Number_AsBoxed_CollectionRootType_Element()
        {
            int @int = 1;
            float? nullableFloat = 2;

            string expected = @"[""1""]";

            var obj = new List<object> { @int };
            string serialized = JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr);
            Assert.Equal(expected, serialized);

            obj = JsonSerializer.Deserialize<List<object>>(serialized, s_optionReadAndWriteFromStr);

            JsonElement el = Assert.IsType<JsonElement>(obj[0]);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal("1", el.GetString());

            expected = @"[""2""]";

            IList obj2 = new object[] { nullableFloat };
            serialized = JsonSerializer.Serialize(obj2, s_optionReadAndWriteFromStr);
            Assert.Equal(expected, serialized);

            obj2 = JsonSerializer.Deserialize<IList>(serialized, s_optionReadAndWriteFromStr);

            el = Assert.IsType<JsonElement>(obj2[0]);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal("2", el.GetString());
        }

        [Fact]
        public static void Number_AsBoxed_CollectionProperty_Element()
        {
            int @int = 2;
            float? nullableFloat = 2;

            string expected = @"{""MyInts"":[""2""],""MyNullableFloats"":[""2""]}";

            var obj = new Class_With_ListsOfBoxedNumbers
            {
                MyInts = new List<object> { @int },
                MyNullableFloats = new object[] { nullableFloat }
            };

            string serialized = JsonSerializer.Serialize(obj);
            JsonTestHelper.AssertJsonEqual(expected, serialized);

            obj = JsonSerializer.Deserialize<Class_With_ListsOfBoxedNumbers>(serialized);

            JsonElement el = Assert.IsType<JsonElement>(obj.MyInts[0]);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal("2", el.GetString());

            el = Assert.IsType<JsonElement>(obj.MyNullableFloats[0]);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal("2", el.GetString());
        }

        public class Class_With_ListsOfBoxedNumbers
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public List<object> MyInts { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public IList MyNullableFloats { get; set; }
        }

        [Fact]
        public static void NonNumber_AsBoxed_Property()
        {
            DateTime dateTime = DateTimeTestHelpers.FixedDateTimeValue;
            Guid? nullableGuid = Guid.NewGuid();

            string expected = @$"{{""MyDateTime"":{JsonSerializer.Serialize(dateTime)},""MyNullableGuid"":{JsonSerializer.Serialize(nullableGuid)}}}";

            var obj = new Class_With_BoxedNonNumbers
            {
                MyDateTime = dateTime,
                MyNullableGuid = nullableGuid
            };

            string serialized = JsonSerializer.Serialize(obj);
            JsonTestHelper.AssertJsonEqual(expected, serialized);

            obj = JsonSerializer.Deserialize<Class_With_BoxedNonNumbers>(serialized);

            JsonElement el = Assert.IsType<JsonElement>(obj.MyDateTime);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal(dateTime, el.GetDateTime());

            el = Assert.IsType<JsonElement>(obj.MyNullableGuid);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal(nullableGuid.Value, el.GetGuid());
        }

        public class Class_With_BoxedNonNumbers
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public object MyDateTime { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public object MyNullableGuid { get; set; }
        }

        [Fact]
        public static void NonNumber_AsBoxed_CollectionRootType_Element()
        {
            DateTime dateTime = DateTimeTestHelpers.FixedDateTimeValue;
            Guid? nullableGuid = Guid.NewGuid();

            string expected = @$"[{JsonSerializer.Serialize(dateTime)}]";

            var obj = new List<object> { dateTime };
            string serialized = JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr);
            Assert.Equal(expected, serialized);

            obj = JsonSerializer.Deserialize<List<object>>(serialized, s_optionReadAndWriteFromStr);

            JsonElement el = Assert.IsType<JsonElement>(obj[0]);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal(dateTime, el.GetDateTime());

            expected = @$"[{JsonSerializer.Serialize(nullableGuid)}]";

            IList obj2 = new object[] { nullableGuid };
            serialized = JsonSerializer.Serialize(obj2, s_optionReadAndWriteFromStr);
            Assert.Equal(expected, serialized);

            obj2 = JsonSerializer.Deserialize<IList>(serialized, s_optionReadAndWriteFromStr);

            el = Assert.IsType<JsonElement>(obj2[0]);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal(nullableGuid.Value, el.GetGuid());
        }

        [Fact]
        public static void NonNumber_AsBoxed_CollectionProperty_Element()
        {
            DateTime dateTime = DateTimeTestHelpers.FixedDateTimeValue;
            Guid? nullableGuid = Guid.NewGuid();

            string expected = @$"{{""MyDateTimes"":[{JsonSerializer.Serialize(dateTime)}],""MyNullableGuids"":[{JsonSerializer.Serialize(nullableGuid)}]}}";

            var obj = new Class_With_ListsOfBoxedNonNumbers
            {
                MyDateTimes = new List<object> { dateTime },
                MyNullableGuids = new object[] { nullableGuid }
            };

            string serialized = JsonSerializer.Serialize(obj);
            JsonTestHelper.AssertJsonEqual(expected, serialized);

            obj = JsonSerializer.Deserialize<Class_With_ListsOfBoxedNonNumbers>(serialized);

            JsonElement el = Assert.IsType<JsonElement>(obj.MyDateTimes[0]);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal(dateTime, el.GetDateTime());

            el = Assert.IsType<JsonElement>(obj.MyNullableGuids[0]);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal(nullableGuid, el.GetGuid());
        }

        public class Class_With_ListsOfBoxedNonNumbers
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public List<object> MyDateTimes { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public IList MyNullableGuids { get; set; }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49936", TestPlatforms.Android)]
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

            string jsonNumbersAsStrings = jsonBuilder_NumbersAsStrings.ToString();

            PerformAsCollectionElementSerialization(
                numbers,
                jsonBuilder_NumbersAsNumbers.ToString(),
                jsonNumbersAsStrings,
                jsonBuilder_NumbersAsNumbersAndStrings.ToString(),
                jsonBuilder_NumbersAsNumbersAndStrings_Alternate.ToString());

            // Reflection based tests for every collection type.
            RunAllCollectionsRoundTripTest<T>(jsonNumbersAsStrings);
        }

        private static void PerformAsCollectionElementSerialization<T>(
            List<T> numbers,
            string json_NumbersAsNumbers,
            string json_NumbersAsStrings,
            string json_NumbersAsNumbersAndStrings,
            string json_NumbersAsNumbersAndStrings_Alternate)
        {
            List<T> deserialized;

            // Option: read from string

            // Deserialize
            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbers, s_optionReadFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsStrings, s_optionReadFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbersAndStrings, s_optionReadFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbersAndStrings_Alternate, s_optionReadFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            // Serialize
            Assert.Equal(json_NumbersAsNumbers, JsonSerializer.Serialize(numbers, s_optionReadFromStr));

            // Option: write as string

            // Deserialize
            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbers, s_optionWriteAsStr);
            AssertIEnumerableEqual(numbers, deserialized);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<T>>(json_NumbersAsStrings, s_optionWriteAsStr));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbersAndStrings, s_optionWriteAsStr));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbersAndStrings_Alternate, s_optionWriteAsStr));

            // Serialize
            Assert.Equal(json_NumbersAsStrings, JsonSerializer.Serialize(numbers, s_optionWriteAsStr));

            // Option: read and write from/to string

            // Deserialize
            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbers, s_optionReadAndWriteFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsStrings, s_optionReadAndWriteFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbersAndStrings, s_optionReadAndWriteFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            deserialized = JsonSerializer.Deserialize<List<T>>(json_NumbersAsNumbersAndStrings_Alternate, s_optionReadAndWriteFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            // Serialize
            Assert.Equal(json_NumbersAsStrings, JsonSerializer.Serialize(numbers, s_optionReadAndWriteFromStr));
        }

        private static void AssertIEnumerableEqual<T>(IEnumerable<T> list1, IEnumerable<T> list2)
        {
            IEnumerator<T> enumerator1 = list1.GetEnumerator();
            IEnumerator<T> enumerator2 = list2.GetEnumerator();

            while (enumerator1.MoveNext())
            {
                enumerator2.MoveNext();
                Assert.Equal(enumerator1.Current, enumerator2.Current);
            }

            Assert.False(enumerator2.MoveNext());
        }

        private static void RunAllCollectionsRoundTripTest<T>(string json)
        {
            foreach (Type type in CollectionTestTypes.DeserializableGenericEnumerableTypes<T>())
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    HashSet<T> obj1 = (HashSet<T>)JsonSerializer.Deserialize(json, type, s_optionReadAndWriteFromStr);
                    string serialized = JsonSerializer.Serialize(obj1, s_optionReadAndWriteFromStr);

                    HashSet<T> obj2 = (HashSet<T>)JsonSerializer.Deserialize(serialized, type, s_optionReadAndWriteFromStr);

                    Assert.Equal(obj1.Count, obj2.Count);
                    foreach (T element in obj1)
                    {
                        Assert.True(obj2.Contains(element));
                    }
                }
                else if (type != typeof(byte[]))
                {
                    object obj = JsonSerializer.Deserialize(json, type, s_optionReadAndWriteFromStr);
                    string serialized = JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr);
                    Assert.Equal(json, serialized);
                }
            }

            foreach (Type type in CollectionTestTypes.DeserializableNonGenericEnumerableTypes())
            {
                // Deserialized as collection of JsonElements.
                object obj = JsonSerializer.Deserialize(json, type, s_optionReadAndWriteFromStr);
                // Serialized as strings with escaping.
                string serialized = JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr);

                // Ensure escaped values were serialized accurately
                List<T> list = JsonSerializer.Deserialize<List<T>>(serialized, s_optionReadAndWriteFromStr);
                serialized = JsonSerializer.Serialize(list, s_optionReadAndWriteFromStr);
                Assert.Equal(json, serialized);

                // Serialize instance which is a collection of numbers (not JsonElements).
                obj = Activator.CreateInstance(type, new[] { list });
                serialized = JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr);
                Assert.Equal(json, serialized);
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
            AssertDictionaryElements_StringValues(serialized);

            // Deserialize
            dict = JsonSerializer.Deserialize<Dictionary<int, float>>(serialized, s_optionReadAndWriteFromStr);

            // Test roundtrip
            JsonTestHelper.AssertJsonEqual(serialized, JsonSerializer.Serialize(dict, s_optionReadAndWriteFromStr));
        }

        private static void AssertDictionaryElements_StringValues(string serialized)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(serialized));
            reader.Read();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    Assert.True(reader.ValueSpan.IndexOf((byte)'\\') == -1);
                }
                else
                {
                    Assert.Equal(JsonTokenType.PropertyName, reader.TokenType);
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/39674", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter))]
        [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/45464", ~RuntimeConfiguration.Release)]
        public static void DictionariesRoundTrip()
        {
            RunAllDictionariessRoundTripTest(JsonNumberTestData.ULongs);
            RunAllDictionariessRoundTripTest(JsonNumberTestData.Floats);
            RunAllDictionariessRoundTripTest(JsonNumberTestData.Doubles);
        }

        private static void RunAllDictionariessRoundTripTest<T>(List<T> numbers)
        {
            StringBuilder jsonBuilder_NumbersAsStrings = new StringBuilder();

            jsonBuilder_NumbersAsStrings.Append("{");

            foreach (T number in numbers)
            {
                string numberAsString = GetNumberAsString(number);
                string jsonWithNumberAsString = @$"""{numberAsString}""";

                jsonBuilder_NumbersAsStrings.Append($"{jsonWithNumberAsString}:");
                jsonBuilder_NumbersAsStrings.Append($"{jsonWithNumberAsString},");
            }

            jsonBuilder_NumbersAsStrings.Remove(jsonBuilder_NumbersAsStrings.Length - 1, 1);
            jsonBuilder_NumbersAsStrings.Append("}");

            string jsonNumbersAsStrings = jsonBuilder_NumbersAsStrings.ToString();

            foreach (Type type in CollectionTestTypes.DeserializableDictionaryTypes<string, T>())
            {
                object obj = JsonSerializer.Deserialize(jsonNumbersAsStrings, type, s_optionReadAndWriteFromStr);
                JsonTestHelper.AssertJsonEqual(jsonNumbersAsStrings, JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr));
            }

            foreach (Type type in CollectionTestTypes.DeserializableNonGenericDictionaryTypes())
            {
                Dictionary<T, T> dict = JsonSerializer.Deserialize<Dictionary<T, T>>(jsonNumbersAsStrings, s_optionReadAndWriteFromStr);

                // Serialize instance which is a dictionary of numbers (not JsonElements).
                object obj = Activator.CreateInstance(type, new[] { dict });
                string serialized = JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr);
                JsonTestHelper.AssertJsonEqual(jsonNumbersAsStrings, serialized);
            }
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
        public static void Number_AsKeyValuePairValue_RoundTrip()
        {
            var obj = new KeyValuePair<ulong?, List<float>>(JsonNumberTestData.NullableULongs.LastOrDefault(), JsonNumberTestData.Floats);

            // Serialize
            string serialized = JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr);

            // Deserialize
            obj = JsonSerializer.Deserialize<KeyValuePair<ulong?, List<float>>>(serialized, s_optionReadAndWriteFromStr);

            // Test roundtrip
            JsonTestHelper.AssertJsonEqual(serialized, JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr));
        }

        [Fact]
        public static void Number_AsObjectWithParameterizedCtor_RoundTrip()
        {
            var obj = new MyClassWithNumbers(JsonNumberTestData.NullableULongs.LastOrDefault(), JsonNumberTestData.Floats);

            // Serialize
            string serialized = JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr);

            // Deserialize
            obj = JsonSerializer.Deserialize<MyClassWithNumbers>(serialized, s_optionReadAndWriteFromStr);

            // Test roundtrip
            JsonTestHelper.AssertJsonEqual(serialized, JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr));
        }

        private class MyClassWithNumbers
        {
            public ulong? Ulong { get; }
            public List<float> ListOfFloats { get; }

            public MyClassWithNumbers(ulong? @ulong, List<float> listOfFloats)
            {
                Ulong = @ulong;
                ListOfFloats = listOfFloats;
            }
        }

        [Fact]
        public static void Number_AsObjectWithParameterizedCtor_PropHasAttribute()
        {
            string json = @"{""ListOfFloats"":[""1""]}";
            // Strict handling on property overrides loose global policy.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MyClassWithNumbers_PropsHasAttribute>(json, s_optionReadFromStr));

            // Serialize
            json = @"{""ListOfFloats"":[1]}";
            MyClassWithNumbers_PropsHasAttribute obj = JsonSerializer.Deserialize<MyClassWithNumbers_PropsHasAttribute>(json);

            // Number serialized as JSON number due to strict handling on property which overrides loose global policy.
            Assert.Equal(json, JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr));
        }

        private class MyClassWithNumbers_PropsHasAttribute
        {
            [JsonNumberHandling(JsonNumberHandling.Strict)]
            public List<float> ListOfFloats { get; }

            public MyClassWithNumbers_PropsHasAttribute(List<float> listOfFloats)
            {
                ListOfFloats = listOfFloats;
            }
        }

        [Fact]
        public static void FloatingPointConstants_Pass()
        {
            // Valid values
            PerformFloatingPointSerialization("NaN");
            PerformFloatingPointSerialization("Infinity");
            PerformFloatingPointSerialization("-Infinity");

            PerformFloatingPointSerialization("\u004EaN"); // NaN
            PerformFloatingPointSerialization("Inf\u0069ni\u0074y"); // Infinity
            PerformFloatingPointSerialization("\u002DInf\u0069nity"); // -Infinity

            static void PerformFloatingPointSerialization(string testString)
            {
                string testStringAsJson = $@"""{testString}""";
                string testJson = @$"{{""FloatNumber"":{testStringAsJson},""DoubleNumber"":{testStringAsJson}}}";

                StructWithNumbers obj;
                switch (testString)
                {
                    case "NaN":
                        obj = JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionsAllowFloatConstants);
                        Assert.Equal(float.NaN, obj.FloatNumber);
                        Assert.Equal(double.NaN, obj.DoubleNumber);

                        obj = JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionReadFromStr);
                        Assert.Equal(float.NaN, obj.FloatNumber);
                        Assert.Equal(double.NaN, obj.DoubleNumber);
                        break;
                    case "Infinity":
                        obj = JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionsAllowFloatConstants);
                        Assert.Equal(float.PositiveInfinity, obj.FloatNumber);
                        Assert.Equal(double.PositiveInfinity, obj.DoubleNumber);

                        obj = JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionReadFromStr);
                        Assert.Equal(float.PositiveInfinity, obj.FloatNumber);
                        Assert.Equal(double.PositiveInfinity, obj.DoubleNumber);
                        break;
                    case "-Infinity":
                        obj = JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionsAllowFloatConstants);
                        Assert.Equal(float.NegativeInfinity, obj.FloatNumber);
                        Assert.Equal(double.NegativeInfinity, obj.DoubleNumber);

                        obj = JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionReadFromStr);
                        Assert.Equal(float.NegativeInfinity, obj.FloatNumber);
                        Assert.Equal(double.NegativeInfinity, obj.DoubleNumber);
                        break;
                    default:
                        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionsAllowFloatConstants));
                        return;
                }

                JsonTestHelper.AssertJsonEqual(testJson, JsonSerializer.Serialize(obj, s_optionsAllowFloatConstants));
                JsonTestHelper.AssertJsonEqual(testJson, JsonSerializer.Serialize(obj, s_optionWriteAsStr));
            }
        }

        [Theory]
        [InlineData("naN")]
        [InlineData("Nan")]
        [InlineData("NAN")]
        [InlineData("+Infinity")]
        [InlineData("+infinity")]
        [InlineData("infinity")]
        [InlineData("infinitY")]
        [InlineData("INFINITY")]
        [InlineData("+INFINITY")]
        [InlineData("-infinity")]
        [InlineData("-infinitY")]
        [InlineData("-INFINITY")]
        [InlineData(" NaN")]
        [InlineData("NaN ")]
        [InlineData(" Infinity")]
        [InlineData(" -Infinity")]
        [InlineData("Infinity ")]
        [InlineData("-Infinity ")]
        [InlineData("a-Infinity")]
        [InlineData("NaNa")]
        [InlineData("Infinitya")]
        [InlineData("-Infinitya")]
#pragma warning disable xUnit1025 // Theory method 'FloatingPointConstants_Fail' on test class 'NumberHandlingTests' has InlineData duplicate(s)
        [InlineData("\u006EaN")] // "naN"
        [InlineData("\u0020Inf\u0069ni\u0074y")] // " Infinity"
        [InlineData("\u002BInf\u0069nity")] // "+Infinity"
#pragma warning restore xUnit1025
        public static void FloatingPointConstants_Fail(string testString)
        {
            string testStringAsJson = $@"""{testString}""";

            string testJson = @$"{{""FloatNumber"":{testStringAsJson}}}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionReadFromStr));

            testJson = @$"{{""DoubleNumber"":{testStringAsJson}}}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<StructWithNumbers>(testJson, s_optionReadFromStr));
        }

        [Fact]
        public static void AllowFloatingPointConstants_WriteAsNumber_IfNotConstant()
        {
            float @float = 1;
            // Not written as "1"
            Assert.Equal("1", JsonSerializer.Serialize(@float, s_optionsAllowFloatConstants));

            double @double = 1;
            // Not written as "1"
            Assert.Equal("1", JsonSerializer.Serialize(@double, s_optionsAllowFloatConstants));
        }

        [Theory]
        [InlineData("NaN")]
        [InlineData("Infinity")]
        [InlineData("-Infinity")]
        public static void Unquoted_FloatingPointConstants_Read_Fail(string testString)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<float>(testString, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double?>(testString, s_optionReadFromStr));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double>(testString, s_optionReadFromStrAllowFloatConstants));
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
            AssertFloatingPointIncompatible_Fails<decimal>();
            AssertFloatingPointIncompatible_Fails<byte?>();
            AssertFloatingPointIncompatible_Fails<sbyte?>();
            AssertFloatingPointIncompatible_Fails<short?>();
            AssertFloatingPointIncompatible_Fails<int?>();
            AssertFloatingPointIncompatible_Fails<long?>();
            AssertFloatingPointIncompatible_Fails<ushort?>();
            AssertFloatingPointIncompatible_Fails<uint?>();
            AssertFloatingPointIncompatible_Fails<ulong?>();
            AssertFloatingPointIncompatible_Fails<decimal?>();
        }

        private static void AssertFloatingPointIncompatible_Fails<T>()
        {
            string[] testCases = new[]
            {
                @"""NaN""",
                @"""Infinity""",
                @"""-Infinity""",
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
                "$123.46", // Currency
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49936", TestPlatforms.Android)]
        public static void EscapingTest()
        {
            // Cause all characters to be escaped.
            var encoderSettings = new TextEncoderSettings();
            encoderSettings.ForbidCharacters('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '+', '-', 'e', 'E');

            JavaScriptEncoder encoder = JavaScriptEncoder.Create(encoderSettings);
            var options = new JsonSerializerOptions(s_optionReadAndWriteFromStr)
            {
                Encoder = encoder
            };

            PerformEscapingTest(JsonNumberTestData.Bytes, options);
            PerformEscapingTest(JsonNumberTestData.SBytes, options);
            PerformEscapingTest(JsonNumberTestData.Shorts, options);
            PerformEscapingTest(JsonNumberTestData.Ints, options);
            PerformEscapingTest(JsonNumberTestData.Longs, options);
            PerformEscapingTest(JsonNumberTestData.UShorts, options);
            PerformEscapingTest(JsonNumberTestData.UInts, options);
            PerformEscapingTest(JsonNumberTestData.ULongs, options);
            PerformEscapingTest(JsonNumberTestData.Floats, options);
            PerformEscapingTest(JsonNumberTestData.Doubles, options);
            PerformEscapingTest(JsonNumberTestData.Decimals, options);
        }

        private static void PerformEscapingTest<T>(List<T> numbers, JsonSerializerOptions options)
        {
            // All input characters are escaped
            IEnumerable<string> numbersAsStrings = numbers.Select(num => GetNumberAsString(num));
            string input = JsonSerializer.Serialize(numbersAsStrings, options);
            AssertListNumbersEscaped(input);

            // Unescaping works
            List<T> deserialized = JsonSerializer.Deserialize<List<T>>(input, options);
            Assert.Equal(numbers.Count, deserialized.Count);
            for (int i = 0; i < numbers.Count; i++)
            {
                Assert.Equal(numbers[i], deserialized[i]);
            }

            // Every number is written as a string, and custom escaping is not honored.
            string serialized = JsonSerializer.Serialize(deserialized, options);
            AssertListNumbersUnescaped(serialized);
        }

        private static void AssertListNumbersEscaped(string json)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
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
                    Assert.True(reader.ValueSpan.IndexOf((byte)'\\') != -1);
                }
            }
        }

        private static void AssertListNumbersUnescaped(string json)
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
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
                    Assert.True(reader.ValueSpan.IndexOf((byte)'\\') == -1);
                }
            }
        }

        [Fact]
        public static void Number_RoundtripNull()
        {
            Perform_Number_RoundTripNull_Test<byte>();
            Perform_Number_RoundTripNull_Test<sbyte>();
            Perform_Number_RoundTripNull_Test<short>();
            Perform_Number_RoundTripNull_Test<int>();
            Perform_Number_RoundTripNull_Test<long>();
            Perform_Number_RoundTripNull_Test<ushort>();
            Perform_Number_RoundTripNull_Test<uint>();
            Perform_Number_RoundTripNull_Test<ulong>();
            Perform_Number_RoundTripNull_Test<float>();
            Perform_Number_RoundTripNull_Test<decimal>();
        }

        private static void Perform_Number_RoundTripNull_Test<T>()
        {
            string nullAsJson = "null";
            string nullAsQuotedJson = $@"""{nullAsJson}""";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>(nullAsJson, s_optionReadAndWriteFromStr));
            Assert.Equal("0", JsonSerializer.Serialize(default(T)));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>(nullAsQuotedJson, s_optionReadAndWriteFromStr));
        }

        [Fact]
        public static void NullableNumber_RoundtripNull()
        {
            Perform_NullableNumber_RoundTripNull_Test<byte?>();
            Perform_NullableNumber_RoundTripNull_Test<sbyte?>();
            Perform_NullableNumber_RoundTripNull_Test<short?>();
            Perform_NullableNumber_RoundTripNull_Test<int?>();
            Perform_NullableNumber_RoundTripNull_Test<long?>();
            Perform_NullableNumber_RoundTripNull_Test<ushort?>();
            Perform_NullableNumber_RoundTripNull_Test<uint?>();
            Perform_NullableNumber_RoundTripNull_Test<ulong?>();
            Perform_NullableNumber_RoundTripNull_Test<float?>();
            Perform_NullableNumber_RoundTripNull_Test<decimal?>();
        }

        private static void Perform_NullableNumber_RoundTripNull_Test<T>()
        {
            string nullAsJson = "null";
            string nullAsQuotedJson = $@"""{nullAsJson}""";

            Assert.Null(JsonSerializer.Deserialize<T>(nullAsJson, s_optionReadAndWriteFromStr));
            Assert.Equal(nullAsJson, JsonSerializer.Serialize(default(T)));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<T>(nullAsQuotedJson, s_optionReadAndWriteFromStr));
        }

        [Fact]
        public static void Disallow_ArbritaryStrings_On_AllowFloatingPointConstants()
        {
            string json = @"""12345""";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<sbyte>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<short>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ushort>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<uint>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ulong>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<float>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<decimal>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte?>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<sbyte?>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<short?>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int?>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long?>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ushort?>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<uint?>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ulong?>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<float?>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double?>(json, s_optionsAllowFloatConstants));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<decimal?>(json, s_optionsAllowFloatConstants));
        }

        [Fact]
        public static void Attributes_OnMembers_Work()
        {
            // Bad JSON because Int should not be string.
            string intIsString = @"{""Float"":""1234.5"",""Int"":""12345""}";

            // Good JSON because Float can be string.
            string floatIsString = @"{""Float"":""1234.5"",""Int"":12345}";

            // Good JSON because Float can be number.
            string floatIsNumber = @"{""Float"":1234.5,""Int"":12345}";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWith_Attribute_OnNumber>(intIsString));

            ClassWith_Attribute_OnNumber obj = JsonSerializer.Deserialize<ClassWith_Attribute_OnNumber>(floatIsString);
            Assert.Equal(1234.5, obj.Float);
            Assert.Equal(12345, obj.Int);

            obj = JsonSerializer.Deserialize<ClassWith_Attribute_OnNumber>(floatIsNumber);
            Assert.Equal(1234.5, obj.Float);
            Assert.Equal(12345, obj.Int);

            // Per options, float should be written as string.
            JsonTestHelper.AssertJsonEqual(floatIsString, JsonSerializer.Serialize(obj));
        }

        private class ClassWith_Attribute_OnNumber
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public float Float { get; set; }

            public int Int { get; set; }
        }

        [Fact]
        public static void Attribute_OnRootType_Works()
        {
            // Not allowed
            string floatIsString = @"{""Float"":""1234"",""Int"":123}";

            // Allowed
            string floatIsNan = @"{""Float"":""NaN"",""Int"":123}";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Type_AllowFloatConstants>(floatIsString));

            Type_AllowFloatConstants obj = JsonSerializer.Deserialize<Type_AllowFloatConstants>(floatIsNan);
            Assert.Equal(float.NaN, obj.Float);
            Assert.Equal(123, obj.Int);

            JsonTestHelper.AssertJsonEqual(floatIsNan, JsonSerializer.Serialize(obj));
        }

        [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
        private class Type_AllowFloatConstants
        {
            public float Float { get; set; }

            public int Int { get; set; }
        }

        [Fact]
        public static void AttributeOnType_WinsOver_GlobalOption()
        {
            // Global options strict, type options loose
            string json = @"{""Float"":""12345""}";
            var obj1 = JsonSerializer.Deserialize<ClassWith_LooseAttribute>(json);

            Assert.Equal(@"{""Float"":""12345""}", JsonSerializer.Serialize(obj1));

            // Global options loose, type options strict
            json = @"{""Float"":""12345""}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWith_StrictAttribute>(json, s_optionReadAndWriteFromStr));

            var obj2 = new ClassWith_StrictAttribute() { Float = 12345 };
            Assert.Equal(@"{""Float"":12345}", JsonSerializer.Serialize(obj2, s_optionReadAndWriteFromStr));
        }

        [JsonNumberHandling(JsonNumberHandling.Strict)]
        public class ClassWith_StrictAttribute
        {
            public float Float { get; set; }
        }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        private class ClassWith_LooseAttribute
        {
            public float Float { get; set; }
        }

        [Fact]
        public static void AttributeOnMember_WinsOver_AttributeOnType()
        {
            string json = @"{""Double"":""NaN""}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWith_Attribute_On_TypeAndMember>(json));

            var obj = new ClassWith_Attribute_On_TypeAndMember { Double = float.NaN };
            Assert.Throws<ArgumentException>(() => JsonSerializer.Serialize(obj));
        }

        [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
        private class ClassWith_Attribute_On_TypeAndMember
        {
            [JsonNumberHandling(JsonNumberHandling.Strict)]
            public double Double { get; set; }
        }

        [Fact]
        public static void Attribute_OnNestedType_Works()
        {
            string jsonWithShortProperty = @"{""Short"":""1""}";
            ClassWith_ReadAsStringAttribute obj = JsonSerializer.Deserialize<ClassWith_ReadAsStringAttribute>(jsonWithShortProperty);
            Assert.Equal(1, obj.Short);

            string jsonWithMyObjectProperty = @"{""MyObject"":{""Float"":""1""}}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWith_ReadAsStringAttribute>(jsonWithMyObjectProperty));
        }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public class ClassWith_ReadAsStringAttribute
        {
            public short Short { get; set; }

            public ClassWith_StrictAttribute MyObject { get; set; }
        }

        [Fact]
        public static void MemberAttributeAppliesToCollection_SimpleElements()
        {
            RunTest<int[]>();
            RunTest<ConcurrentQueue<int>>();
            RunTest<GenericICollectionWrapper<int>>();
            RunTest<IEnumerable<int>>();
            RunTest<Collection<int>>();
            RunTest<ImmutableList<int>>();
            RunTest<HashSet<int>>();
            RunTest<List<int>>();
            RunTest<IList<int>>();
            RunTest<IList>();
            RunTest<Queue<int>>();

            static void RunTest<T>()
            {
                string json = @"{""MyList"":[""1"",""2""]}";
                ClassWithSimpleCollectionProperty<T> obj = global::System.Text.Json.JsonSerializer.Deserialize<ClassWithSimpleCollectionProperty<T>>(json);
                Assert.Equal(json, global::System.Text.Json.JsonSerializer.Serialize(obj));
            }
        }

        public class ClassWithSimpleCollectionProperty<T>
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public T MyList { get; set; }
        }

        [Fact]
        public static void NestedCollectionElementTypeHandling_Overrides_GlobalOption()
        {
            // Strict policy on the collection element type overrides read-as-string on the collection property
            string json = @"{""MyList"":[{""Float"":""1""}]}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithComplexListProperty>(json, s_optionReadAndWriteFromStr));

            // Strict policy on the collection element type overrides write-as-string on the collection property
            var obj = new ClassWithComplexListProperty
            {
                MyList = new List<ClassWith_StrictAttribute> { new ClassWith_StrictAttribute { Float = 1 } }
            };
            Assert.Equal(@"{""MyList"":[{""Float"":1}]}", JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr));
        }

        public class ClassWithComplexListProperty
        {
            public List<ClassWith_StrictAttribute> MyList { get; set; }
        }

        [Fact]
        public static void NumberHandlingAttribute_NotAllowedOn_CollectionOfNonNumbers()
        {
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<ClassWith_AttributeOnComplexListProperty>(""));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new ClassWith_AttributeOnComplexListProperty()));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<ClassWith_AttributeOnComplexDictionaryProperty>(""));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new ClassWith_AttributeOnComplexDictionaryProperty()));
        }

        public class ClassWith_AttributeOnComplexListProperty
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public List<ClassWith_StrictAttribute> MyList { get; set; }
        }

        public class ClassWith_AttributeOnComplexDictionaryProperty
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            public Dictionary<string, ClassWith_StrictAttribute> MyDictionary { get; set; }
        }

        [Fact]
        public static void MemberAttributeAppliesToDictionary_SimpleElements()
        {
            string json = @"{""First"":""1"",""Second"":""2""}";
            ClassWithSimpleDictionaryProperty obj = JsonSerializer.Deserialize<ClassWithSimpleDictionaryProperty>(json);
        }

        public class ClassWithSimpleDictionaryProperty
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public Dictionary<string, int> MyDictionary { get; set; }
        }

        [Fact]
        public static void NestedDictionaryElementTypeHandling_Overrides_GlobalOption()
        {
            // Strict policy on the dictionary element type overrides read-as-string on the collection property.
            string json = @"{""MyDictionary"":{""Key"":{""Float"":""1""}}}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithComplexDictionaryProperty>(json, s_optionReadFromStr));

            // Strict policy on the collection element type overrides write-as-string on the collection property
            var obj = new ClassWithComplexDictionaryProperty
            {
                MyDictionary = new Dictionary<string, ClassWith_StrictAttribute> { ["Key"] = new ClassWith_StrictAttribute { Float = 1 } }
            };
            Assert.Equal(@"{""MyDictionary"":{""Key"":{""Float"":1}}}", JsonSerializer.Serialize(obj, s_optionReadFromStr));
        }

        public class ClassWithComplexDictionaryProperty
        {
            public Dictionary<string, ClassWith_StrictAttribute> MyDictionary { get; set; }
        }

        [Fact]
        public static void TypeAttributeAppliesTo_CustomCollectionElements()
        {
            string json = @"[""1""]";
            MyCustomList obj = JsonSerializer.Deserialize<MyCustomList>(json);
            Assert.Equal(json, JsonSerializer.Serialize(obj));
        }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public class MyCustomList : List<int> { }

        [Fact]
        public static void TypeAttributeAppliesTo_CustomCollectionElements_HonoredWhenProperty()
        {
            string json = @"{""List"":[""1""]}";
            ClassWithCustomList obj = JsonSerializer.Deserialize<ClassWithCustomList>(json);
            Assert.Equal(json, JsonSerializer.Serialize(obj));
        }

        public class ClassWithCustomList
        {
            public MyCustomList List { get; set; }
        }

        [Fact]
        public static void TypeAttributeAppliesTo_CustomDictionaryElements()
        {
            string json = @"{""Key"":""1""}";
            MyCustomDictionary obj = JsonSerializer.Deserialize<MyCustomDictionary>(json);
            Assert.Equal(json, JsonSerializer.Serialize(obj));
        }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public class MyCustomDictionary : Dictionary<string, int> { }

        [Fact]
        public static void TypeAttributeAppliesTo_CustomDictionaryElements_HonoredWhenProperty()
        {
            string json = @"{""Dictionary"":{""Key"":""1""}}";
            ClassWithCustomDictionary obj = JsonSerializer.Deserialize<ClassWithCustomDictionary>(json);
            Assert.Equal(json, JsonSerializer.Serialize(obj));
        }

        public class ClassWithCustomDictionary
        {
            public MyCustomDictionary Dictionary { get; set; }
        }

        [Fact]
        public static void Attribute_OnType_NotRecursive()
        {
            // Recursive behavior, where number handling setting on a property is applied to subsequent
            // properties in its type closure, would allow a string number. This is not supported.
            string json = @"{""NestedClass"":{""MyInt"":""1""}}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<AttributeAppliedToFirstLevelProp>(json));

            var obj = new AttributeAppliedToFirstLevelProp
            {
                NestedClass = new NonNumberType { MyInt = 1 }
            };
            Assert.Equal(@"{""NestedClass"":{""MyInt"":1}}", JsonSerializer.Serialize(obj));
        }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public class AttributeAppliedToFirstLevelProp
        {
            public NonNumberType NestedClass { get; set; }
        }

        public class NonNumberType
        {
            public int MyInt { get; set; }
        }

        [Fact]
        public static void HandlingOnMemberOverridesHandlingOnType_Enumerable()
        {
            string json = @"{""List"":[""1""]}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MyCustomListWrapper>(json));

            var obj = new MyCustomListWrapper
            {
                List = new MyCustomList { 1 }
            };
            Assert.Equal(@"{""List"":[1]}", JsonSerializer.Serialize(obj));
        }

        public class MyCustomListWrapper
        {
            [JsonNumberHandling(JsonNumberHandling.Strict)]
            public MyCustomList List { get; set; }
        }

        [Fact]
        public static void HandlingOnMemberOverridesHandlingOnType_Dictionary()
        {
            string json = @"{""Dictionary"":{""Key"":""1""}}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MyCustomDictionaryWrapper>(json));

            var obj1 = new MyCustomDictionaryWrapper
            {
                Dictionary = new MyCustomDictionary { ["Key"] = 1 }
            };
            Assert.Equal(@"{""Dictionary"":{""Key"":1}}", JsonSerializer.Serialize(obj1));
        }

        public class MyCustomDictionaryWrapper
        {
            [JsonNumberHandling(JsonNumberHandling.Strict)]
            public MyCustomDictionary Dictionary { get; set; }
        }

        [Fact]
        public static void Attribute_Allowed_On_NonNumber_NonCollection_Property()
        {
            const string Json = @"{""MyProp"":{""MyInt"":1}}";

            ClassWith_NumberHandlingOn_ObjectProperty obj = JsonSerializer.Deserialize<ClassWith_NumberHandlingOn_ObjectProperty>(Json);
            Assert.Equal(1, obj.MyProp.MyInt);

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(Json, json);
        }

        public class ClassWith_NumberHandlingOn_ObjectProperty
        {
            [JsonNumberHandling(JsonNumberHandling.Strict)]
            public NonNumberType MyProp { get; set; }
        }

        [Fact]
        public static void Attribute_Allowed_On_Property_WithCustomConverter()
        {
            string json = @"{""Prop"":1}";

            // Converter returns 25 regardless of input.
            var obj = JsonSerializer.Deserialize<ClassWith_NumberHandlingOn_Property_WithCustomConverter>(json);
            Assert.Equal(25, obj.Prop);

            // Converter throws this exception regardless of input.
            NotImplementedException ex = Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(obj));
            Assert.Equal("Converter was called", ex.Message);
        }

        public class ClassWith_NumberHandlingOn_Property_WithCustomConverter
        {
            [JsonNumberHandling(JsonNumberHandling.Strict)]
            [JsonConverter(typeof(ConverterForInt32))]
            public int Prop { get; set; }
        }

        [Fact]
        public static void Attribute_Allowed_On_Type_WithCustomConverter()
        {
            string json = @"{}";
            NotImplementedException ex;

            // Assert regular Read/Write methods on custom converter are called.
            ex = Assert.Throws<NotImplementedException>(() => JsonSerializer.Deserialize<ClassWith_NumberHandlingOn_Type_WithCustomConverter>(json));
            Assert.Equal("Converter was called", ex.Message);

            ex = Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(new ClassWith_NumberHandlingOn_Type_WithCustomConverter()));
            Assert.Equal("Converter was called", ex.Message);
        }

        [JsonNumberHandling(JsonNumberHandling.Strict)]
        [JsonConverter(typeof(ConverterForMyType))]
        public class ClassWith_NumberHandlingOn_Type_WithCustomConverter
        {
        }

        private class ConverterForMyType : JsonConverter<ClassWith_NumberHandlingOn_Type_WithCustomConverter>
        {
            public override ClassWith_NumberHandlingOn_Type_WithCustomConverter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException("Converter was called");
            }

            public override void Write(Utf8JsonWriter writer, ClassWith_NumberHandlingOn_Type_WithCustomConverter value, JsonSerializerOptions options)
            {
                throw new NotImplementedException("Converter was called");
            }
        }

        [Fact]
        public static void CustomConverterOverridesBuiltInLogic()
        {
            var options = new JsonSerializerOptions(s_optionReadAndWriteFromStr)
            {
                Converters = { new ConverterForInt32(), new ConverterForFloat() }
            };

            string json = @"""32""";

            // Converter returns 25 regardless of input.
            Assert.Equal(25, JsonSerializer.Deserialize<int>(json, options));

            // Converter throws this exception regardless of input.
            NotImplementedException ex = Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(4, options));
            Assert.Equal("Converter was called", ex.Message);

            json = @"""NaN""";

            // Converter returns 25 if NaN.
            Assert.Equal(25, JsonSerializer.Deserialize<float?>(json, options));

            // Converter writes 25 if NaN.
            Assert.Equal("25", JsonSerializer.Serialize((float?)float.NaN, options));
        }

        public class ConverterForFloat : JsonConverter<float?>
        {
            public override float? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String && reader.GetString() == "NaN")
                {
                    return 25;
                }

                throw new NotSupportedException();
            }

            public override void Write(Utf8JsonWriter writer, float? value, JsonSerializerOptions options)
            {
                if (float.IsNaN(value.Value))
                {
                    writer.WriteNumberValue(25);
                    return;
                }

                throw new NotSupportedException();
            }
        }

        [Fact]
        public static void JsonNumberHandling_ArgOutOfRangeFail()
        {
            // Global options
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new JsonSerializerOptions { NumberHandling = (JsonNumberHandling)(-1) });
            Assert.Contains("value", ex.ToString());
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new JsonSerializerOptions { NumberHandling = (JsonNumberHandling)(8) });

            ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new JsonNumberHandlingAttribute((JsonNumberHandling)(-1)));
            Assert.Contains("handling", ex.ToString());
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new JsonNumberHandlingAttribute((JsonNumberHandling)(8)));
        }

        [Fact]
        public static void InternalCollectionConverter_CustomNumberConverter_GlobalOption()
        {
            NotImplementedException ex;

            var list = new List<int> { 1 };
            var options = new JsonSerializerOptions(s_optionReadAndWriteFromStr)
            {
                Converters = { new ConverterForInt32() }
            };

            // Assert converter methods are called and not Read/WriteWithNumberHandling (which would throw InvalidOperationException).
            // Converter returns 25 regardless of input.
            Assert.Equal(25, JsonSerializer.Deserialize<List<int>>(@"[""1""]", options)[0]);
            // Converter throws this exception regardless of input.
            ex = Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(list, options));
            Assert.Equal("Converter was called", ex.Message);

            var list2 = new List<int?> { 1 };
            Assert.Equal(25, JsonSerializer.Deserialize<List<int?>>(@"[""1""]", options)[0]);
            ex = Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(list2, options));
            Assert.Equal("Converter was called", ex.Message);

            // Okay to set number handling for number collection property when number is handled with custom converter;
            // converter Read/Write methods called.
            ClassWithListPropAndAttribute obj1 = JsonSerializer.Deserialize<ClassWithListPropAndAttribute>(@"{""Prop"":[""1""]}", options);
            Assert.Equal(25, obj1.Prop[0]);
            ex = Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(obj1, options));
            Assert.Equal("Converter was called", ex.Message);

            ClassWithDictPropAndAttribute obj2 = JsonSerializer.Deserialize<ClassWithDictPropAndAttribute>(@"{""Prop"":{""1"":""1""}}", options);
            Assert.Equal(25, obj2.Prop[1]);
            ex = Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(obj2, options));
            Assert.Equal("Converter was called", ex.Message);
        }

        private class ClassWithListPropAndAttribute
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public List<int> Prop { get; set; }
        }

        private class ClassWithDictPropAndAttribute
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public Dictionary<int, int?> Prop { get; set; }
        }

        [Fact]
        public static void InternalCollectionConverter_CustomNumberConverter_OnProperty()
        {
            // Invalid to set number handling for number collection property when number is handled with custom converter.
            var ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<ClassWithListPropAndAttribute_ConverterOnProp>(""));
            Assert.Contains(nameof(ClassWithListPropAndAttribute_ConverterOnProp), ex.ToString());
            Assert.Contains("IntProp", ex.ToString());

            ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new ClassWithListPropAndAttribute_ConverterOnProp()));
            Assert.Contains(nameof(ClassWithListPropAndAttribute_ConverterOnProp), ex.ToString());
            Assert.Contains("IntProp", ex.ToString());

            ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<ClassWithDictPropAndAttribute_ConverterOnProp>(""));
            Assert.Contains(nameof(ClassWithDictPropAndAttribute_ConverterOnProp), ex.ToString());
            Assert.Contains("IntProp", ex.ToString());

            ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new ClassWithDictPropAndAttribute_ConverterOnProp()));
            Assert.Contains(nameof(ClassWithDictPropAndAttribute_ConverterOnProp), ex.ToString());
            Assert.Contains("IntProp", ex.ToString());
        }

        private class ClassWithListPropAndAttribute_ConverterOnProp
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            [JsonConverter(typeof(ListOfIntConverter))]
            public List<int> IntProp { get; set; }
        }

        private class ClassWithDictPropAndAttribute_ConverterOnProp
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            [JsonConverter(typeof(ClassWithDictPropAndAttribute_ConverterOnProp))]
            public Dictionary<int, int?> IntProp { get; set; }
        }

        public class ListOfIntConverter : JsonConverter<List<int>>
        {
            public override List<int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();
            public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options) => throw new NotImplementedException();
        }

        [Fact]
        public static void InternalCollectionConverter_CustomNullableNumberConverter()
        {
            NotImplementedException ex;

            var dict = new Dictionary<int, int?> { [1] = 1 };
            var options = new JsonSerializerOptions(s_optionReadAndWriteFromStr)
            {
                Converters = { new ConverterForNullableInt32() }
            };

            // Assert converter methods are called and not Read/WriteWithNumberHandling (which would throw InvalidOperationException).
            // Converter returns 25 regardless of input.
            Assert.Equal(25, JsonSerializer.Deserialize<Dictionary<int, int?>>(@"{""1"":""1""}", options)[1]);
            ex = Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(dict, options));
            Assert.Equal("Converter was called", ex.Message);

            var obj = JsonSerializer.Deserialize<ClassWithDictPropAndAttribute>(@"{""Prop"":{""1"":""1""}}", options);
            Assert.Equal(25, obj.Prop[1]);
            ex = Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(obj, options));
            Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(dict, options));
            Assert.Equal("Converter was called", ex.Message);
        }

        public class ConverterForNullableInt32 : JsonConverter<int?>
        {
            public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return 25;
            }

            public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
            {
                throw new NotImplementedException("Converter was called");
            }
        }

        /// <summary>
        /// Example of a custom converter that uses the options to determine behavior.
        /// </summary>
        [Fact]
        public static void AdaptableCustomConverter()
        {
            // Baseline without custom converter
            PlainClassWithList obj = new() { Prop = new List<int>() { 1 } };
            string json = JsonSerializer.Serialize(obj, s_optionReadAndWriteFromStr);
            Assert.Equal("{\"Prop\":[\"1\"]}", json);

            obj = JsonSerializer.Deserialize<PlainClassWithList>(json, s_optionReadAndWriteFromStr);
            Assert.Equal(1, obj.Prop[0]);

            // First with numbers
            JsonSerializerOptions options = new()
            {
                Converters = { new AdaptableInt32Converter() }
            };

            obj = new PlainClassWithList() { Prop = new List<int>() { 1 } };
            json = JsonSerializer.Serialize(obj, options);
            Assert.Equal("{\"Prop\":[101]}", json);

            obj = JsonSerializer.Deserialize<PlainClassWithList>(json, options);
            Assert.Equal(1, obj.Prop[0]);

            // Then with strings
            options = new JsonSerializerOptions()
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
                Converters = { new AdaptableInt32Converter() }
            };

            obj = new PlainClassWithList() { Prop = new List<int>() { 1 } };
            json = JsonSerializer.Serialize(obj, options);
            Assert.Equal("{\"Prop\":[\"101\"]}", json);

            obj = JsonSerializer.Deserialize<PlainClassWithList>(json, options);
            Assert.Equal(1, obj.Prop[0]);
        }

        private class PlainClassWithList
        {
            public List<int> Prop { get; set; }
        }

        public class AdaptableInt32Converter : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if ((JsonNumberHandling.AllowReadingFromString & options.NumberHandling) != 0)
                {
                    // Assume it's a string; don't use TryParse().
                    return int.Parse(reader.GetString(), CultureInfo.InvariantCulture) - 100;
                }
                else
                {
                    return reader.GetInt32() - 100;
                }
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                if ((JsonNumberHandling.WriteAsString & options.NumberHandling) != 0)
                {
                    writer.WriteStringValue((value + 100).ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    writer.WriteNumberValue(value + 100);
                }
            }
        }
    }

    public class NumberHandlingTests_AsyncStreamOverload : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_AsyncStreamOverload() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    public class NumberHandlingTests_SyncStreamOverload : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_SyncStreamOverload() : base(JsonSerializerWrapper.SyncStreamSerializer) { }
    }

    public class NumberHandlingTests_SyncOverload : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_SyncOverload() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public class NumberHandlingTests_Document : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_Document() : base(JsonSerializerWrapper.DocumentSerializer) { }
    }

    public class NumberHandlingTests_Element : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_Element() : base(JsonSerializerWrapper.ElementSerializer) { }
    }

    public class NumberHandlingTests_Node : NumberHandlingTests_OverloadSpecific
    {
        public NumberHandlingTests_Node() : base(JsonSerializerWrapper.NodeSerializer) { }
    }

    public abstract class NumberHandlingTests_OverloadSpecific
    {
        private JsonSerializerWrapper Serializer { get; }

        public NumberHandlingTests_OverloadSpecific(JsonSerializerWrapper deserializer)
        {
            Serializer = deserializer;
        }

        [Theory]
        [MemberData(nameof(NumberHandling_ForPropsReadAfter_DeserializingCtorParams_TestData))]
        public async Task NumberHandling_ForPropsReadAfter_DeserializingCtorParams(string json)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            Result result = await Serializer.DeserializeWrapper<Result>(json, options);
            JsonTestHelper.AssertJsonEqual(json, JsonSerializer.Serialize(result, options));
        }

        public static IEnumerable<object[]> NumberHandling_ForPropsReadAfter_DeserializingCtorParams_TestData()
        {
            yield return new object[]
            {
                @"{
                    ""album"": {
                        ""userPlayCount"": ""123"",
                        ""name"": ""the name of the album"",
                        ""artist"": ""the name of the artist"",
                        ""wiki"": {
                            ""summary"": ""a summary of the album""
                        }
                    }
                }"
            };

            yield return new object[]
            {
                @"{
                    ""album"": {
                        ""name"": ""the name of the album"",
                        ""userPlayCount"": ""123"",
                        ""artist"": ""the name of the artist"",
                        ""wiki"": {
                            ""summary"": ""a summary of the album""
                        }
                    }
                }"
            };

            yield return new object[]
            {
                @"{
                    ""album"": {
                        ""name"": ""the name of the album"",
                        ""artist"": ""the name of the artist"",
                        ""userPlayCount"": ""123"",
                        ""wiki"": {
                            ""summary"": ""a summary of the album""
                        }
                    }
                }"
            };

            yield return new object[]
            {
                @"{
                    ""album"": {
                        ""name"": ""the name of the album"",
                        ""artist"": ""the name of the artist"",
                        ""wiki"": {
                            ""summary"": ""a summary of the album""
                        },
                        ""userPlayCount"": ""123""
                    }
                }"
            };
        }

        public class Result
        {
            public Album Album { get; init; }
        }

        public class Album
        {
            public Album(string name, string artist)
            {
                Name = name;
                Artist = artist;
            }

            public string Name { get; init; }
            public string Artist { get; init; }

            public long? userPlayCount { get; init; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }
        }
    }
}
