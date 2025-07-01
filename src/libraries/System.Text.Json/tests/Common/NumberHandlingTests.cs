// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract class NumberHandlingTests : SerializerTests
    {
        public NumberHandlingTests(JsonSerializerWrapper serializerWrapper) : base(serializerWrapper) { }

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
        public async Task Number_AsRootType_RoundTrip()
        {
            await RunAsRootTypeTest(JsonNumberTestData.Bytes);
            await RunAsRootTypeTest(JsonNumberTestData.SBytes);
            await RunAsRootTypeTest(JsonNumberTestData.Shorts);
            await RunAsRootTypeTest(JsonNumberTestData.Ints);
            await RunAsRootTypeTest(JsonNumberTestData.Longs);
            await RunAsRootTypeTest(JsonNumberTestData.UShorts);
            await RunAsRootTypeTest(JsonNumberTestData.UInts);
            await RunAsRootTypeTest(JsonNumberTestData.ULongs);
            await RunAsRootTypeTest(JsonNumberTestData.Floats);
            await RunAsRootTypeTest(JsonNumberTestData.Doubles);
            await RunAsRootTypeTest(JsonNumberTestData.Decimals);
#if NET
            await RunAsRootTypeTest(JsonNumberTestData.Int128s);
            await RunAsRootTypeTest(JsonNumberTestData.UInt128s);
            await RunAsRootTypeTest(JsonNumberTestData.Halfs);
#endif
            await RunAsRootTypeTest(JsonNumberTestData.NullableBytes);
            await RunAsRootTypeTest(JsonNumberTestData.NullableSBytes);
            await RunAsRootTypeTest(JsonNumberTestData.NullableShorts);
            await RunAsRootTypeTest(JsonNumberTestData.NullableInts);
            await RunAsRootTypeTest(JsonNumberTestData.NullableLongs);
            await RunAsRootTypeTest(JsonNumberTestData.NullableUShorts);
            await RunAsRootTypeTest(JsonNumberTestData.NullableUInts);
            await RunAsRootTypeTest(JsonNumberTestData.NullableULongs);
            await RunAsRootTypeTest(JsonNumberTestData.NullableFloats);
            await RunAsRootTypeTest(JsonNumberTestData.NullableDoubles);
            await RunAsRootTypeTest(JsonNumberTestData.NullableDecimals);
#if NET
            await RunAsRootTypeTest(JsonNumberTestData.NullableInt128s);
            await RunAsRootTypeTest(JsonNumberTestData.NullableUInt128s);
            await RunAsRootTypeTest(JsonNumberTestData.NullableHalfs);
#endif
        }

        private async Task RunAsRootTypeTest<T>(List<T> numbers)
        {
            foreach (T number in numbers)
            {
                string numberAsString = GetNumberAsString(number);
                string json = $"{numberAsString}";
                string jsonWithNumberAsString = @$"""{numberAsString}""";
                await PerformAsRootTypeSerialization(number, json, jsonWithNumberAsString);
            }
        }

        private static string GetNumberAsString<T>(T number)
        {
            return number switch
            {
                double @double => @double.ToString(JsonTestHelper.DoubleFormatString, CultureInfo.InvariantCulture),
                float @float => @float.ToString(JsonTestHelper.SingleFormatString, CultureInfo.InvariantCulture),
                decimal @decimal => @decimal.ToString(CultureInfo.InvariantCulture),
                _ => Convert.ToString(number, CultureInfo.InvariantCulture)
            };
        }

        private async Task PerformAsRootTypeSerialization<T>(T number, string jsonWithNumberAsNumber, string jsonWithNumberAsString)
        {
            // Option: read from string

            // Deserialize
            Assert.Equal(number, await Serializer.DeserializeWrapper<T>(jsonWithNumberAsNumber, s_optionReadFromStr));
            Assert.Equal(number, await Serializer.DeserializeWrapper<T>(jsonWithNumberAsString, s_optionReadFromStr));

            // Serialize
            Assert.Equal(jsonWithNumberAsNumber, await Serializer.SerializeWrapper(number, s_optionReadFromStr));

            // Option: write as string

            // Deserialize
            Assert.Equal(number, await Serializer.DeserializeWrapper<T>(jsonWithNumberAsNumber, s_optionWriteAsStr));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<T>(jsonWithNumberAsString, s_optionWriteAsStr));

            // Serialize
            Assert.Equal(jsonWithNumberAsString, await Serializer.SerializeWrapper(number, s_optionWriteAsStr));

            // Option: read and write from/to string

            // Deserialize
            Assert.Equal(number, await Serializer.DeserializeWrapper<T>(jsonWithNumberAsNumber, s_optionReadAndWriteFromStr));
            Assert.Equal(number, await Serializer.DeserializeWrapper<T>(jsonWithNumberAsString, s_optionReadAndWriteFromStr));

            // Serialize
            Assert.Equal(jsonWithNumberAsString, await Serializer.SerializeWrapper(number, s_optionReadAndWriteFromStr));
        }

        [Fact]
        public async Task Number_AsBoxed_RootType()
        {
            string numberAsString = @"""2""";

            int @int = 2;
            float @float = 2;
            int? nullableInt = 2;
            float? nullableFloat = 2;

            Assert.Equal(numberAsString, await Serializer.SerializeWrapper((object)@int, s_optionReadAndWriteFromStr));
            Assert.Equal(numberAsString, await Serializer.SerializeWrapper((object)@float, s_optionReadAndWriteFromStr));
            Assert.Equal(numberAsString, await Serializer.SerializeWrapper((object)nullableInt, s_optionReadAndWriteFromStr));
            Assert.Equal(numberAsString, await Serializer.SerializeWrapper((object)nullableFloat, s_optionReadAndWriteFromStr));

            Assert.Equal(2, (int)await Serializer.DeserializeWrapper(numberAsString, typeof(int), s_optionReadAndWriteFromStr));
            Assert.Equal(2, (float)await Serializer.DeserializeWrapper(numberAsString, typeof(float), s_optionReadAndWriteFromStr));
            Assert.Equal(2, (int?)await Serializer.DeserializeWrapper(numberAsString, typeof(int?), s_optionReadAndWriteFromStr));
            Assert.Equal(2, (float?)await Serializer.DeserializeWrapper(numberAsString, typeof(float?), s_optionReadAndWriteFromStr));
        }

        [Fact]
        public async Task Number_AsBoxed_Property()
        {
            int @int = 1;
            float? nullableFloat = 2;

            string expected = @"{""MyInt"":""1"",""MyNullableFloat"":""2""}";

            var obj = new Class_With_BoxedNumbers
            {
                MyInt = @int,
                MyNullableFloat = nullableFloat
            };

            string serialized = await Serializer.SerializeWrapper(obj);
            JsonTestHelper.AssertJsonEqual(expected, serialized);

            obj = await Serializer.DeserializeWrapper<Class_With_BoxedNumbers>(serialized);

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
        public async Task Number_AsBoxed_CollectionRootType_Element()
        {
            int @int = 1;
            float? nullableFloat = 2;

            string expected = @"[""1""]";

            var obj = new List<object> { @int };
            string serialized = await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr);
            Assert.Equal(expected, serialized);

            obj = await Serializer.DeserializeWrapper<List<object>>(serialized, s_optionReadAndWriteFromStr);

            JsonElement el = Assert.IsType<JsonElement>(obj[0]);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal("1", el.GetString());

            expected = @"[""2""]";

            IList obj2 = new object[] { nullableFloat };
            serialized = await Serializer.SerializeWrapper(obj2, s_optionReadAndWriteFromStr);
            Assert.Equal(expected, serialized);

            obj2 = await Serializer.DeserializeWrapper<IList>(serialized, s_optionReadAndWriteFromStr);

            el = Assert.IsType<JsonElement>(obj2[0]);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal("2", el.GetString());
        }

        [Fact]
        public async Task Number_AsBoxed_CollectionProperty_Element()
        {
            int @int = 2;
            float? nullableFloat = 2;

            string expected = @"{""MyInts"":[""2""],""MyNullableFloats"":[""2""]}";

            var obj = new Class_With_ListsOfBoxedNumbers
            {
                MyInts = new List<object> { @int },
                MyNullableFloats = new object[] { nullableFloat }
            };

            string serialized = await Serializer.SerializeWrapper(obj);
            JsonTestHelper.AssertJsonEqual(expected, serialized);

            obj = await Serializer.DeserializeWrapper<Class_With_ListsOfBoxedNumbers>(serialized);

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
        public async Task NonNumber_AsBoxed_Property()
        {
            DateTime dateTime = DateTimeTestHelpers.FixedDateTimeValue;
            Guid? nullableGuid = Guid.NewGuid();

            string expected = @$"{{""MyDateTime"":{await Serializer.SerializeWrapper(dateTime)},""MyNullableGuid"":{await Serializer.SerializeWrapper(nullableGuid)}}}";

            var obj = new Class_With_BoxedNonNumbers
            {
                MyDateTime = dateTime,
                MyNullableGuid = nullableGuid
            };

            string serialized = await Serializer.SerializeWrapper(obj);
            JsonTestHelper.AssertJsonEqual(expected, serialized);

            obj = await Serializer.DeserializeWrapper<Class_With_BoxedNonNumbers>(serialized);

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
        public async Task NonNumber_AsBoxed_CollectionRootType_Element()
        {
            DateTime dateTime = DateTimeTestHelpers.FixedDateTimeValue;
            Guid? nullableGuid = Guid.NewGuid();

            string expected = @$"[{await Serializer.SerializeWrapper(dateTime)}]";

            var obj = new List<object> { dateTime };
            string serialized = await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr);
            Assert.Equal(expected, serialized);

            obj = await Serializer.DeserializeWrapper<List<object>>(serialized, s_optionReadAndWriteFromStr);

            JsonElement el = Assert.IsType<JsonElement>(obj[0]);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal(dateTime, el.GetDateTime());

            expected = @$"[{await Serializer.SerializeWrapper(nullableGuid)}]";

            IList obj2 = new object[] { nullableGuid };
            serialized = await Serializer.SerializeWrapper(obj2, s_optionReadAndWriteFromStr);
            Assert.Equal(expected, serialized);

            obj2 = await Serializer.DeserializeWrapper<IList>(serialized, s_optionReadAndWriteFromStr);

            el = Assert.IsType<JsonElement>(obj2[0]);
            Assert.Equal(JsonValueKind.String, el.ValueKind);
            Assert.Equal(nullableGuid.Value, el.GetGuid());
        }

        [Fact]
        public async Task NonNumber_AsBoxed_CollectionProperty_Element()
        {
            DateTime dateTime = DateTimeTestHelpers.FixedDateTimeValue;
            Guid? nullableGuid = Guid.NewGuid();

            string expected = @$"{{""MyDateTimes"":[{await Serializer.SerializeWrapper(dateTime)}],""MyNullableGuids"":[{await Serializer.SerializeWrapper(nullableGuid)}]}}";

            var obj = new Class_With_ListsOfBoxedNonNumbers
            {
                MyDateTimes = new List<object> { dateTime },
                MyNullableGuids = new object[] { nullableGuid }
            };

            string serialized = await Serializer.SerializeWrapper(obj);
            JsonTestHelper.AssertJsonEqual(expected, serialized);

            obj = await Serializer.DeserializeWrapper<Class_With_ListsOfBoxedNonNumbers>(serialized);

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
        public async Task Number_AsCollectionElement_RoundTrip()
        {
            await RunAsCollectionElementTest(JsonNumberTestData.Bytes);
            await RunAsCollectionElementTest(JsonNumberTestData.SBytes);
            await RunAsCollectionElementTest(JsonNumberTestData.Shorts);
            await RunAsCollectionElementTest(JsonNumberTestData.Ints);
            await RunAsCollectionElementTest(JsonNumberTestData.Longs);
            await RunAsCollectionElementTest(JsonNumberTestData.UShorts);
            await RunAsCollectionElementTest(JsonNumberTestData.UInts);
            await RunAsCollectionElementTest(JsonNumberTestData.ULongs);
            await RunAsCollectionElementTest(JsonNumberTestData.Floats);
            await RunAsCollectionElementTest(JsonNumberTestData.Doubles);
            await RunAsCollectionElementTest(JsonNumberTestData.Decimals);
#if NET
            await RunAsCollectionElementTest(JsonNumberTestData.Int128s);
            await RunAsCollectionElementTest(JsonNumberTestData.UInt128s);
            await RunAsCollectionElementTest(JsonNumberTestData.Halfs);
#endif

            // https://github.com/dotnet/runtime/issues/66220
            if (!PlatformDetection.IsAppleMobile)
            {
                await RunAsCollectionElementTest(JsonNumberTestData.NullableBytes);
                await RunAsCollectionElementTest(JsonNumberTestData.NullableSBytes);
                await RunAsCollectionElementTest(JsonNumberTestData.NullableShorts);
                await RunAsCollectionElementTest(JsonNumberTestData.NullableInts);
                await RunAsCollectionElementTest(JsonNumberTestData.NullableLongs);
                await RunAsCollectionElementTest(JsonNumberTestData.NullableUShorts);
                await RunAsCollectionElementTest(JsonNumberTestData.NullableUInts);
                await RunAsCollectionElementTest(JsonNumberTestData.NullableULongs);
                await RunAsCollectionElementTest(JsonNumberTestData.NullableFloats);
                await RunAsCollectionElementTest(JsonNumberTestData.NullableDoubles);
                await RunAsCollectionElementTest(JsonNumberTestData.NullableDecimals);
#if NET
                await RunAsCollectionElementTest(JsonNumberTestData.NullableInt128s);
                await RunAsCollectionElementTest(JsonNumberTestData.NullableUInt128s);
                await RunAsCollectionElementTest(JsonNumberTestData.NullableHalfs);
#endif
            }
        }

        private async Task RunAsCollectionElementTest<T>(List<T> numbers)
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

            await PerformAsCollectionElementSerialization(
                numbers,
                jsonBuilder_NumbersAsNumbers.ToString(),
                jsonNumbersAsStrings,
                jsonBuilder_NumbersAsNumbersAndStrings.ToString(),
                jsonBuilder_NumbersAsNumbersAndStrings_Alternate.ToString());

            // Reflection based tests for every collection type.
            await RunAllCollectionsRoundTripTest<T>(jsonNumbersAsStrings);
        }

        private async Task PerformAsCollectionElementSerialization<T>(
            List<T> numbers,
            string json_NumbersAsNumbers,
            string json_NumbersAsStrings,
            string json_NumbersAsNumbersAndStrings,
            string json_NumbersAsNumbersAndStrings_Alternate)
        {
            List<T> deserialized;

            // Option: read from string

            // Deserialize
            deserialized = await Serializer.DeserializeWrapper<List<T>>(json_NumbersAsNumbers, s_optionReadFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            deserialized = await Serializer.DeserializeWrapper<List<T>>(json_NumbersAsStrings, s_optionReadFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            deserialized = await Serializer.DeserializeWrapper<List<T>>(json_NumbersAsNumbersAndStrings, s_optionReadFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            deserialized = await Serializer.DeserializeWrapper<List<T>>(json_NumbersAsNumbersAndStrings_Alternate, s_optionReadFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            // Serialize
            Assert.Equal(json_NumbersAsNumbers, await Serializer.SerializeWrapper(numbers, s_optionReadFromStr));

            // Option: write as string

            // Deserialize
            deserialized = await Serializer.DeserializeWrapper<List<T>>(json_NumbersAsNumbers, s_optionWriteAsStr);
            AssertIEnumerableEqual(numbers, deserialized);

            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<List<T>>(json_NumbersAsStrings, s_optionWriteAsStr));

            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<List<T>>(json_NumbersAsNumbersAndStrings, s_optionWriteAsStr));

            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<List<T>>(json_NumbersAsNumbersAndStrings_Alternate, s_optionWriteAsStr));

            // Serialize
            Assert.Equal(json_NumbersAsStrings, await Serializer.SerializeWrapper(numbers, s_optionWriteAsStr));

            // Option: read and write from/to string

            // Deserialize
            deserialized = await Serializer.DeserializeWrapper<List<T>>(json_NumbersAsNumbers, s_optionReadAndWriteFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            deserialized = await Serializer.DeserializeWrapper<List<T>>(json_NumbersAsStrings, s_optionReadAndWriteFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            deserialized = await Serializer.DeserializeWrapper<List<T>>(json_NumbersAsNumbersAndStrings, s_optionReadAndWriteFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            deserialized = await Serializer.DeserializeWrapper<List<T>>(json_NumbersAsNumbersAndStrings_Alternate, s_optionReadAndWriteFromStr);
            AssertIEnumerableEqual(numbers, deserialized);

            // Serialize
            Assert.Equal(json_NumbersAsStrings, await Serializer.SerializeWrapper(numbers, s_optionReadAndWriteFromStr));
        }

        private void AssertIEnumerableEqual<T>(IEnumerable<T> list1, IEnumerable<T> list2)
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

        private async Task RunAllCollectionsRoundTripTest<T>(string json)
        {
            foreach (Type type in CollectionTestTypes.DeserializableGenericEnumerableTypes<T>())
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    HashSet<T> obj1 = (HashSet<T>)await Serializer.DeserializeWrapper(json, type, s_optionReadAndWriteFromStr);
                    string serialized = await Serializer.SerializeWrapper(obj1, s_optionReadAndWriteFromStr);

                    HashSet<T> obj2 = (HashSet<T>)await Serializer.DeserializeWrapper(serialized, type, s_optionReadAndWriteFromStr);

                    Assert.Equal(obj1.Count, obj2.Count);
                    foreach (T element in obj1)
                    {
                        Assert.True(obj2.Contains(element));
                    }
                }
                else if (type != typeof(byte[]))
                {
                    object obj = await Serializer.DeserializeWrapper(json, type, s_optionReadAndWriteFromStr);
                    string serialized = await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr);
                    Assert.Equal(json, serialized);
                }
            }

            foreach (Type type in CollectionTestTypes.DeserializableNonGenericEnumerableTypes())
            {
                // Deserialized as collection of JsonElements.
                object obj = await Serializer.DeserializeWrapper(json, type, s_optionReadAndWriteFromStr);
                // Serialized as strings with escaping.
                string serialized = await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr);

                // Ensure escaped values were serialized accurately
                List<T> list = await Serializer.DeserializeWrapper<List<T>>(serialized, s_optionReadAndWriteFromStr);
                serialized = await Serializer.SerializeWrapper(list, s_optionReadAndWriteFromStr);
                Assert.Equal(json, serialized);

                // Serialize instance which is a collection of numbers (not JsonElements).
                obj = Activator.CreateInstance(type, new[] { list });
                serialized = await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr);
                Assert.Equal(json, serialized);
            }
        }

        [Fact]
        public async Task Number_AsDictionaryElement_RoundTrip()
        {
            var dict = new Dictionary<int, float>();
            for (int i = 0; i < 10; i++)
            {
                dict[JsonNumberTestData.Ints[i]] = JsonNumberTestData.Floats[i];
            }

            // Serialize
            string serialized = await Serializer.SerializeWrapper(dict, s_optionReadAndWriteFromStr);
            AssertDictionaryElements_StringValues(serialized);

            // Deserialize
            dict = await Serializer.DeserializeWrapper<Dictionary<int, float>>(serialized, s_optionReadAndWriteFromStr);

            // Test roundtrip
            JsonTestHelper.AssertJsonEqual(serialized, await Serializer.SerializeWrapper(dict, s_optionReadAndWriteFromStr));
        }

        private void AssertDictionaryElements_StringValues(string serialized)
        {
            Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(serialized));
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
        public async Task DictionariesRoundTrip()
        {
            await RunAllDictionariessRoundTripTest(JsonNumberTestData.ULongs);
            await RunAllDictionariessRoundTripTest(JsonNumberTestData.Floats);
            await RunAllDictionariessRoundTripTest(JsonNumberTestData.Doubles);
        }

        private async Task RunAllDictionariessRoundTripTest<T>(List<T> numbers)
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
                object obj = await Serializer.DeserializeWrapper(jsonNumbersAsStrings, type, s_optionReadAndWriteFromStr);
                JsonTestHelper.AssertJsonEqual(jsonNumbersAsStrings, await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr));
            }

            foreach (Type type in CollectionTestTypes.DeserializableNonGenericDictionaryTypes())
            {
                Dictionary<T, T> dict = await Serializer.DeserializeWrapper<Dictionary<T, T>>(jsonNumbersAsStrings, s_optionReadAndWriteFromStr);

                // Serialize instance which is a dictionary of numbers (not JsonElements).
                object obj = Activator.CreateInstance(type, new[] { dict });
                string serialized = await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr);
                JsonTestHelper.AssertJsonEqual(jsonNumbersAsStrings, serialized);
            }
        }

        [Fact]
        public async Task Number_AsPropertyValue_RoundTrip()
        {
            var obj = new Class_With_NullableUInt64_And_Float()
            {
                NullableUInt64Number = JsonNumberTestData.NullableULongs.LastOrDefault(),
                FloatNumbers = JsonNumberTestData.Floats
            };

            // Serialize
            string serialized = await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr);

            // Deserialize
            obj = await Serializer.DeserializeWrapper<Class_With_NullableUInt64_And_Float>(serialized, s_optionReadAndWriteFromStr);

            // Test roundtrip
            JsonTestHelper.AssertJsonEqual(serialized, await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr));
        }

        public class Class_With_NullableUInt64_And_Float
        {
            public ulong? NullableUInt64Number { get; set; }
            [JsonInclude]
            public List<float> FloatNumbers;
        }

        [Fact]
        public async Task Number_AsKeyValuePairValue_RoundTrip()
        {
            var obj = new KeyValuePair<ulong?, List<float>>(JsonNumberTestData.NullableULongs.LastOrDefault(), JsonNumberTestData.Floats);

            // Serialize
            string serialized = await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr);

            // Deserialize
            obj = await Serializer.DeserializeWrapper<KeyValuePair<ulong?, List<float>>>(serialized, s_optionReadAndWriteFromStr);

            // Test roundtrip
            JsonTestHelper.AssertJsonEqual(serialized, await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr));
        }

        [Fact]
        public async Task Number_AsObjectWithParameterizedCtor_RoundTrip()
        {
            var obj = new MyClassWithNumbers(JsonNumberTestData.NullableULongs.LastOrDefault(), JsonNumberTestData.Floats);

            // Serialize
            string serialized = await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr);

            // Deserialize
            obj = await Serializer.DeserializeWrapper<MyClassWithNumbers>(serialized, s_optionReadAndWriteFromStr);

            // Test roundtrip
            JsonTestHelper.AssertJsonEqual(serialized, await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr));
        }

        public class MyClassWithNumbers
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
        public async Task Number_AsObjectWithParameterizedCtor_PropHasAttribute()
        {
            string json = @"{""ListOfFloats"":[""1""]}";
            // Strict handling on property overrides loose global policy.
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<MyClassWithNumbers_PropsHasAttribute>(json, s_optionReadFromStr));

            // Serialize
            json = @"{""ListOfFloats"":[1]}";
            MyClassWithNumbers_PropsHasAttribute obj = await Serializer.DeserializeWrapper<MyClassWithNumbers_PropsHasAttribute>(json);

            // Number serialized as JSON number due to strict handling on property which overrides loose global policy.
            Assert.Equal(json, await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr));
        }

        public class MyClassWithNumbers_PropsHasAttribute
        {
            [JsonNumberHandling(JsonNumberHandling.Strict)]
            public List<float> ListOfFloats { get; }

            public MyClassWithNumbers_PropsHasAttribute(List<float> listOfFloats)
            {
                ListOfFloats = listOfFloats;
            }
        }

        [Fact]
        public async Task FloatingPointConstants_Pass()
        {
            // Valid values
            await PerformFloatingPointSerialization("NaN");
            await PerformFloatingPointSerialization("Infinity");
            await PerformFloatingPointSerialization("-Infinity");

            await PerformFloatingPointSerialization("\u004EaN"); // NaN
            await PerformFloatingPointSerialization("Inf\u0069ni\u0074y"); // Infinity
            await PerformFloatingPointSerialization("\u002DInf\u0069nity"); // -Infinity

            async Task PerformFloatingPointSerialization(string testString)
            {
                string testStringAsJson = $@"""{testString}""";
#if NET
                string testJson = @$"{{""HalfNumber"":{testStringAsJson},""FloatNumber"":{testStringAsJson},""DoubleNumber"":{testStringAsJson}}}";
#else
                string testJson = @$"{{""FloatNumber"":{testStringAsJson},""DoubleNumber"":{testStringAsJson}}}";
#endif

                StructWithNumbers obj;
                switch (testString)
                {
                    case "NaN":
                        obj = await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionsAllowFloatConstants);
#if NET
                        Assert.Equal(Half.NaN, obj.HalfNumber);
#endif
                        Assert.Equal(float.NaN, obj.FloatNumber);
                        Assert.Equal(double.NaN, obj.DoubleNumber);

                        obj = await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionReadFromStr);
#if NET
                        Assert.Equal(Half.NaN, obj.HalfNumber);
#endif
                        Assert.Equal(float.NaN, obj.FloatNumber);
                        Assert.Equal(double.NaN, obj.DoubleNumber);
                        break;
                    case "Infinity":
                        obj = await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionsAllowFloatConstants);
#if NET
                        Assert.Equal(Half.PositiveInfinity, obj.HalfNumber);
#endif
                        Assert.Equal(float.PositiveInfinity, obj.FloatNumber);
                        Assert.Equal(double.PositiveInfinity, obj.DoubleNumber);

                        obj = await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionReadFromStr);
#if NET
                        Assert.Equal(Half.PositiveInfinity, obj.HalfNumber);
#endif
                        Assert.Equal(float.PositiveInfinity, obj.FloatNumber);
                        Assert.Equal(double.PositiveInfinity, obj.DoubleNumber);
                        break;
                    case "-Infinity":
                        obj = await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionsAllowFloatConstants);
#if NET
                        Assert.Equal(Half.NegativeInfinity, obj.HalfNumber);
#endif
                        Assert.Equal(float.NegativeInfinity, obj.FloatNumber);
                        Assert.Equal(double.NegativeInfinity, obj.DoubleNumber);

                        obj = await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionReadFromStr);
#if NET
                        Assert.Equal(Half.NegativeInfinity, obj.HalfNumber);
#endif
                        Assert.Equal(float.NegativeInfinity, obj.FloatNumber);
                        Assert.Equal(double.NegativeInfinity, obj.DoubleNumber);
                        break;
                    default:
                        await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionsAllowFloatConstants));
                        return;
                }

                JsonTestHelper.AssertJsonEqual(testJson, await Serializer.SerializeWrapper(obj, s_optionsAllowFloatConstants));
                JsonTestHelper.AssertJsonEqual(testJson, await Serializer.SerializeWrapper(obj, s_optionWriteAsStr));
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
        public async Task FloatingPointConstants_Fail(string testString)
        {
            string testStringAsJson = $@"""{testString}""";
            string testJson;
#if NET
            testJson = @$"{{""HalfNumber"":{testStringAsJson}}}";
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionReadFromStr));
#endif
            testJson = @$"{{""FloatNumber"":{testStringAsJson}}}";
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionReadFromStr));

            testJson = @$"{{""DoubleNumber"":{testStringAsJson}}}";
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<StructWithNumbers>(testJson, s_optionReadFromStr));
        }

        [Fact]
        public async Task AllowFloatingPointConstants_WriteAsNumber_IfNotConstant()
        {
#if NET
            Half half = (Half)1;
            // Not written as "1"
            Assert.Equal("1", await Serializer.SerializeWrapper(half, s_optionsAllowFloatConstants));
#endif
            float @float = 1;
            // Not written as "1"
            Assert.Equal("1", await Serializer.SerializeWrapper(@float, s_optionsAllowFloatConstants));

            double @double = 1;
            // Not written as "1"
            Assert.Equal("1", await Serializer.SerializeWrapper(@double, s_optionsAllowFloatConstants));
        }

        [Theory]
        [InlineData("NaN")]
        [InlineData("Infinity")]
        [InlineData("-Infinity")]
        public async Task Unquoted_FloatingPointConstants_Read_Fail(string testString)
        {
#if NET
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<Half>(testString, s_optionsAllowFloatConstants));
#endif
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<float>(testString, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<double?>(testString, s_optionReadFromStr));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<double>(testString, s_optionReadFromStrAllowFloatConstants));
        }

        public struct StructWithNumbers
        {
#if NET
            public Half HalfNumber { get; set; }
#endif
            public float FloatNumber { get; set; }
            public double DoubleNumber { get; set; }
        }

        [Fact]
        public async Task ReadFromString_AllowFloatingPoint()
        {
            string json = @"{""IntNumber"":""1"",""FloatNumber"":""NaN""}";
            ClassWithNumbers obj = await Serializer.DeserializeWrapper<ClassWithNumbers>(json, s_optionReadFromStrAllowFloatConstants);

            Assert.Equal(1, obj.IntNumber);
            Assert.Equal(float.NaN, obj.FloatNumber);

            JsonTestHelper.AssertJsonEqual(@"{""IntNumber"":1,""FloatNumber"":""NaN""}", await Serializer.SerializeWrapper(obj, s_optionReadFromStrAllowFloatConstants));
        }

        [Fact]
        public async Task WriteAsString_AllowFloatingPoint()
        {
            string json = @"{""IntNumber"":""1"",""FloatNumber"":""NaN""}";
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithNumbers>(json, s_optionWriteAsStrAllowFloatConstants));

            var obj = new ClassWithNumbers
            {
                IntNumber = 1,
                FloatNumber = float.NaN
            };

            JsonTestHelper.AssertJsonEqual(json, await Serializer.SerializeWrapper(obj, s_optionWriteAsStrAllowFloatConstants));
        }

        public class ClassWithNumbers
        {
            public int IntNumber { get; set; }
            public float FloatNumber { get; set; }
        }

        [Fact]
        public async Task FloatingPointConstants_IncompatibleNumber()
        {
            await AssertFloatingPointIncompatible_Fails<byte>();
            await AssertFloatingPointIncompatible_Fails<sbyte>();
            await AssertFloatingPointIncompatible_Fails<short>();
            await AssertFloatingPointIncompatible_Fails<int>();
            await AssertFloatingPointIncompatible_Fails<long>();
            await AssertFloatingPointIncompatible_Fails<ushort>();
            await AssertFloatingPointIncompatible_Fails<uint>();
            await AssertFloatingPointIncompatible_Fails<ulong>();
            await AssertFloatingPointIncompatible_Fails<decimal>();
            await AssertFloatingPointIncompatible_Fails<byte?>();
            await AssertFloatingPointIncompatible_Fails<sbyte?>();
            await AssertFloatingPointIncompatible_Fails<short?>();
            await AssertFloatingPointIncompatible_Fails<int?>();
            await AssertFloatingPointIncompatible_Fails<long?>();
            await AssertFloatingPointIncompatible_Fails<ushort?>();
            await AssertFloatingPointIncompatible_Fails<uint?>();
            await AssertFloatingPointIncompatible_Fails<ulong?>();
            await AssertFloatingPointIncompatible_Fails<decimal?>();
#if NET
            await AssertFloatingPointIncompatible_Fails<Int128>();
            await AssertFloatingPointIncompatible_Fails<UInt128>();
            await AssertFloatingPointIncompatible_Fails<Int128?>();
            await AssertFloatingPointIncompatible_Fails<UInt128?>();
#endif
        }

        private async Task AssertFloatingPointIncompatible_Fails<T>()
        {
            string[] testCases = new[]
            {
                @"""NaN""",
                @"""Infinity""",
                @"""-Infinity""",
            };

            foreach (string test in testCases)
            {
                await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<T>(test, s_optionReadFromStrAllowFloatConstants));
            }
        }

        [Fact]
        public async Task UnsupportedFormats()
        {
            await AssertUnsupportedFormatThrows<byte>();
            await AssertUnsupportedFormatThrows<sbyte>();
            await AssertUnsupportedFormatThrows<short>();
            await AssertUnsupportedFormatThrows<int>();
            await AssertUnsupportedFormatThrows<long>();
            await AssertUnsupportedFormatThrows<ushort>();
            await AssertUnsupportedFormatThrows<uint>();
            await AssertUnsupportedFormatThrows<ulong>();
            await AssertUnsupportedFormatThrows<float>();
            await AssertUnsupportedFormatThrows<decimal>();
            await AssertUnsupportedFormatThrows<byte?>();
            await AssertUnsupportedFormatThrows<sbyte?>();
            await AssertUnsupportedFormatThrows<short?>();
            await AssertUnsupportedFormatThrows<int?>();
            await AssertUnsupportedFormatThrows<long?>();
            await AssertUnsupportedFormatThrows<ushort?>();
            await AssertUnsupportedFormatThrows<uint?>();
            await AssertUnsupportedFormatThrows<ulong?>();
            await AssertUnsupportedFormatThrows<float?>();
            await AssertUnsupportedFormatThrows<decimal?>();
#if NET
            await AssertUnsupportedFormatThrows<Int128>();
            await AssertUnsupportedFormatThrows<UInt128>();
            await AssertUnsupportedFormatThrows<Half>();
            await AssertUnsupportedFormatThrows<Int128?>();
            await AssertUnsupportedFormatThrows<UInt128?>();
            await AssertUnsupportedFormatThrows<Half?>();
#endif
        }

        private async Task AssertUnsupportedFormatThrows<T>()
        {
            string[] testCases = new[]
            {
                "01", // leading zeroes
                "$123.46", // Currency
                "100.00 %", // Percent
                 "1234,57", // Fixed point
                 "00FF", // Hexadecimal
            };

            foreach (string test in testCases)
            {
                await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<T>(test, s_optionReadFromStr));
            }
        }

#if NET
        [Fact]
        public async Task InvalidNumberFormatThrows()
        {
            await AssertInvalidNumberFormatThrows<Int128>("170141183460469231731687303715884105728"); // MaxValue + 1
            await AssertInvalidNumberFormatThrows<Int128>("-170141183460469231731687303715884105729"); // MaxValue - 1
            await AssertInvalidNumberFormatThrows<Int128>("3.14");
            await AssertInvalidNumberFormatThrows<UInt128>("340282366920938463463374607431768211456"); // MaxValue + 1
            await AssertInvalidNumberFormatThrows<Int128>("3.14");
            await AssertInvalidNumberFormatThrows<UInt128>("-1");
            await AssertInvalidNumberFormatThrows<Half>("65520");
            await AssertInvalidNumberFormatThrows<Half>("-65520");
        }

        private async Task AssertInvalidNumberFormatThrows<T>(string testString)
        {
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<T>(testString));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<T>($@"""{testString}""", s_optionReadFromStr));
        }
#endif

        [Fact]
        public async Task EscapingTest()
        {
            // Cause all characters to be escaped.
            var encoderSettings = new TextEncoderSettings();
            encoderSettings.ForbidCharacters('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', '+', '-', 'e', 'E');

            JavaScriptEncoder encoder = JavaScriptEncoder.Create(encoderSettings);
            var options = new JsonSerializerOptions(s_optionReadAndWriteFromStr)
            {
                Encoder = encoder
            };

            await PerformEscapingTest(JsonNumberTestData.Bytes, options);
            await PerformEscapingTest(JsonNumberTestData.SBytes, options);
            await PerformEscapingTest(JsonNumberTestData.Shorts, options);
            await PerformEscapingTest(JsonNumberTestData.Ints, options);
            await PerformEscapingTest(JsonNumberTestData.Longs, options);
            await PerformEscapingTest(JsonNumberTestData.UShorts, options);
            await PerformEscapingTest(JsonNumberTestData.UInts, options);
            await PerformEscapingTest(JsonNumberTestData.ULongs, options);
            await PerformEscapingTest(JsonNumberTestData.Floats, options);
            await PerformEscapingTest(JsonNumberTestData.Doubles, options);
            await PerformEscapingTest(JsonNumberTestData.Decimals, options);
#if NET
            await PerformEscapingTest(JsonNumberTestData.Int128s, options);
            await PerformEscapingTest(JsonNumberTestData.UInt128s, options);
            await PerformEscapingTest(JsonNumberTestData.Halfs, options);
#endif

        }

        private async Task PerformEscapingTest<T>(List<T> numbers, JsonSerializerOptions options)
        {
            // All input characters are escaped
            IEnumerable<string> numbersAsStrings = numbers.Select(num => GetNumberAsString(num));
            string input = await Serializer.SerializeWrapper(numbersAsStrings, options);
            AssertListNumbersEscaped(input);

            // Unescaping works
            List<T> deserialized = await Serializer.DeserializeWrapper<List<T>>(input, options);
            Assert.Equal(numbers.Count, deserialized.Count);
            for (int i = 0; i < numbers.Count; i++)
            {
                Assert.Equal(numbers[i], deserialized[i]);
            }

            // Every number is written as a string, and custom escaping is not honored.
            string serialized = await Serializer.SerializeWrapper(deserialized, options);
            AssertListNumbersUnescaped(serialized);
        }

        private void AssertListNumbersEscaped(string json)
        {
            Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
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

        private void AssertListNumbersUnescaped(string json)
        {
            Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
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
        public async Task Number_RoundtripNull()
        {
            await Perform_Number_RoundTripNull_Test<byte>();
            await Perform_Number_RoundTripNull_Test<sbyte>();
            await Perform_Number_RoundTripNull_Test<short>();
            await Perform_Number_RoundTripNull_Test<int>();
            await Perform_Number_RoundTripNull_Test<long>();
            await Perform_Number_RoundTripNull_Test<ushort>();
            await Perform_Number_RoundTripNull_Test<uint>();
            await Perform_Number_RoundTripNull_Test<ulong>();
            await Perform_Number_RoundTripNull_Test<float>();
            await Perform_Number_RoundTripNull_Test<decimal>();
#if NET
            await Perform_Number_RoundTripNull_Test<Int128>();
            await Perform_Number_RoundTripNull_Test<UInt128>();
            await Perform_Number_RoundTripNull_Test<Half>();
#endif
        }

        private async Task Perform_Number_RoundTripNull_Test<T>()
        {
            string nullAsJson = "null";
            string nullAsQuotedJson = $@"""{nullAsJson}""";

            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<T>(nullAsJson, s_optionReadAndWriteFromStr));
            Assert.Equal("0", await Serializer.SerializeWrapper(default(T)));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<T>(nullAsQuotedJson, s_optionReadAndWriteFromStr));
        }

        [Fact]
        public async Task NullableNumber_RoundtripNull()
        {
            await Perform_NullableNumber_RoundTripNull_Test<byte?>();
            await Perform_NullableNumber_RoundTripNull_Test<sbyte?>();
            await Perform_NullableNumber_RoundTripNull_Test<short?>();
            await Perform_NullableNumber_RoundTripNull_Test<int?>();
            await Perform_NullableNumber_RoundTripNull_Test<long?>();
            await Perform_NullableNumber_RoundTripNull_Test<ushort?>();
            await Perform_NullableNumber_RoundTripNull_Test<uint?>();
            await Perform_NullableNumber_RoundTripNull_Test<ulong?>();
            await Perform_NullableNumber_RoundTripNull_Test<float?>();
            await Perform_NullableNumber_RoundTripNull_Test<decimal?>();
#if NET
            await Perform_NullableNumber_RoundTripNull_Test<Int128?>();
            await Perform_NullableNumber_RoundTripNull_Test<UInt128?>();
            await Perform_NullableNumber_RoundTripNull_Test<Half?>();
#endif
        }

        private async Task Perform_NullableNumber_RoundTripNull_Test<T>()
        {
            string nullAsJson = "null";
            string nullAsQuotedJson = $@"""{nullAsJson}""";

            Assert.Null(await Serializer.DeserializeWrapper<T>(nullAsJson, s_optionReadAndWriteFromStr));
            Assert.Equal(nullAsJson, await Serializer.SerializeWrapper(default(T)));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<T>(nullAsQuotedJson, s_optionReadAndWriteFromStr));
        }

        [Fact]
        public async Task Disallow_ArbritaryStrings_On_AllowFloatingPointConstants()
        {
            string json = @"""12345""";

            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<byte>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<sbyte>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<short>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<int>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<long>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ushort>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<uint>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ulong>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<float>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<double>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<decimal>(json, s_optionsAllowFloatConstants));
#if NET
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<Int128>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<UInt128>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<Half>(json, s_optionsAllowFloatConstants));
#endif
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<byte?>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<sbyte?>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<short?>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<int?>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<long?>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ushort?>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<uint?>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ulong?>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<float?>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<double?>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<decimal?>(json, s_optionsAllowFloatConstants));
#if NET
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<Int128?>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<UInt128?>(json, s_optionsAllowFloatConstants));
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<Half?>(json, s_optionsAllowFloatConstants));
#endif
        }

        [Fact]
        public async Task Attributes_OnMembers_Work()
        {
            // Bad JSON because Int should not be string.
            string intIsString = @"{""Float"":""1234.5"",""Int"":""12345""}";

            // Good JSON because Float can be string.
            string floatIsString = @"{""Float"":""1234.5"",""Int"":12345}";

            // Good JSON because Float can be number.
            string floatIsNumber = @"{""Float"":1234.5,""Int"":12345}";

            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWith_Attribute_OnNumber>(intIsString));

            ClassWith_Attribute_OnNumber obj = await Serializer.DeserializeWrapper<ClassWith_Attribute_OnNumber>(floatIsString);
            Assert.Equal(1234.5, obj.Float);
            Assert.Equal(12345, obj.Int);

            obj = await Serializer.DeserializeWrapper<ClassWith_Attribute_OnNumber>(floatIsNumber);
            Assert.Equal(1234.5, obj.Float);
            Assert.Equal(12345, obj.Int);

            // Per options, float should be written as string.
            JsonTestHelper.AssertJsonEqual(floatIsString, await Serializer.SerializeWrapper(obj));
        }

        public class ClassWith_Attribute_OnNumber
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public float Float { get; set; }

            public int Int { get; set; }
        }

        [Fact]
        public async Task Attribute_OnRootType_Works()
        {
            // Not allowed
            string floatIsString = @"{""Float"":""1234"",""Int"":123}";

            // Allowed
            string floatIsNan = @"{""Float"":""NaN"",""Int"":123}";

            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<Type_AllowFloatConstants>(floatIsString));

            Type_AllowFloatConstants obj = await Serializer.DeserializeWrapper<Type_AllowFloatConstants>(floatIsNan);
            Assert.Equal(float.NaN, obj.Float);
            Assert.Equal(123, obj.Int);

            JsonTestHelper.AssertJsonEqual(floatIsNan, await Serializer.SerializeWrapper(obj));
        }

        [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
        public class Type_AllowFloatConstants
        {
            public float Float { get; set; }

            public int Int { get; set; }
        }

        [Fact]
        public async Task AttributeOnType_WinsOver_GlobalOption()
        {
            // Global options strict, type options loose
            string json = @"{""Float"":""12345""}";
            var obj1 = await Serializer.DeserializeWrapper<ClassWith_LooseAttribute>(json);

            Assert.Equal(@"{""Float"":""12345""}", await Serializer.SerializeWrapper(obj1));

            // Global options loose, type options strict
            json = @"{""Float"":""12345""}";
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWith_StrictAttribute>(json, s_optionReadAndWriteFromStr));

            var obj2 = new ClassWith_StrictAttribute() { Float = 12345 };
            Assert.Equal(@"{""Float"":12345}", await Serializer.SerializeWrapper(obj2, s_optionReadAndWriteFromStr));
        }

        [JsonNumberHandling(JsonNumberHandling.Strict)]
        public class ClassWith_StrictAttribute
        {
            public float Float { get; set; }
        }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public class ClassWith_LooseAttribute
        {
            public float Float { get; set; }
        }

        [Fact]
        public async Task AttributeOnMember_WinsOver_AttributeOnType()
        {
            string json = @"{""Double"":""NaN""}";
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWith_Attribute_On_TypeAndMember>(json));

            var obj = new ClassWith_Attribute_On_TypeAndMember { Double = float.NaN };
            await Assert.ThrowsAsync<ArgumentException>(async () => await Serializer.SerializeWrapper(obj));
        }

        [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
        public class ClassWith_Attribute_On_TypeAndMember
        {
            [JsonNumberHandling(JsonNumberHandling.Strict)]
            public double Double { get; set; }
        }

        [Fact]
        public async Task Attribute_OnNestedType_Works()
        {
            string jsonWithShortProperty = @"{""Short"":""1""}";
            ClassWith_ReadAsStringAttribute obj = await Serializer.DeserializeWrapper<ClassWith_ReadAsStringAttribute>(jsonWithShortProperty);
            Assert.Equal(1, obj.Short);

            string jsonWithMyObjectProperty = @"{""MyObject"":{""Float"":""1""}}";
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWith_ReadAsStringAttribute>(jsonWithMyObjectProperty));
        }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public class ClassWith_ReadAsStringAttribute
        {
            public short Short { get; set; }

            public ClassWith_StrictAttribute MyObject { get; set; }
        }

        [Fact]
        public async Task MemberAttributeAppliesToCollection_SimpleElements()
        {
            await RunTest<int[]>();
            await RunTest<ConcurrentQueue<int>>();
            await RunTest<GenericICollectionWrapper<int>>();
            await RunTest<IEnumerable<int>>();
            await RunTest<Collection<int>>();
            await RunTest<ImmutableList<int>>();
            await RunTest<HashSet<int>>();
            await RunTest<List<int>>();
            await RunTest<IList<int>>();
            await RunTest<IList>();
            await RunTest<Queue<int>>();

            async Task RunTest<T>()
            {
                string json = @"{""MyList"":[""1"",""2""]}";
                ClassWithSimpleCollectionProperty<T> obj = await Serializer.DeserializeWrapper<ClassWithSimpleCollectionProperty<T>>(json);
                Assert.Equal(json, await Serializer.SerializeWrapper(obj));
            }
        }

        public class ClassWithSimpleCollectionProperty<T>
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public T MyList { get; set; }
        }

        [Fact]
        public async Task NestedCollectionElementTypeHandling_Overrides_GlobalOption()
        {
            // Strict policy on the collection element type overrides read-as-string on the collection property
            string json = @"{""MyList"":[{""Float"":""1""}]}";
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithComplexListProperty>(json, s_optionReadAndWriteFromStr));

            // Strict policy on the collection element type overrides write-as-string on the collection property
            var obj = new ClassWithComplexListProperty
            {
                MyList = new List<ClassWith_StrictAttribute> { new ClassWith_StrictAttribute { Float = 1 } }
            };
            Assert.Equal(@"{""MyList"":[{""Float"":1}]}", await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr));
        }

        public class ClassWithComplexListProperty
        {
            public List<ClassWith_StrictAttribute> MyList { get; set; }
        }

        [Fact]
        public async Task NumberHandlingAttribute_NotAllowedOn_CollectionOfNonNumbers()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ClassWith_AttributeOnComplexListProperty>(""));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(new ClassWith_AttributeOnComplexListProperty()));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ClassWith_AttributeOnComplexDictionaryProperty>(""));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(new ClassWith_AttributeOnComplexDictionaryProperty()));
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
        public async Task MemberAttributeAppliesToDictionary_SimpleElements()
        {
            string json = @"{""First"":""1"",""Second"":""2""}";
            ClassWithSimpleDictionaryProperty obj = await Serializer.DeserializeWrapper<ClassWithSimpleDictionaryProperty>(json);
        }

        public class ClassWithSimpleDictionaryProperty
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public Dictionary<string, int> MyDictionary { get; set; }
        }

        [Fact]
        public async Task NestedDictionaryElementTypeHandling_Overrides_GlobalOption()
        {
            // Strict policy on the dictionary element type overrides read-as-string on the collection property.
            string json = @"{""MyDictionary"":{""Key"":{""Float"":""1""}}}";
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithComplexDictionaryProperty>(json, s_optionReadFromStr));

            // Strict policy on the collection element type overrides write-as-string on the collection property
            var obj = new ClassWithComplexDictionaryProperty
            {
                MyDictionary = new Dictionary<string, ClassWith_StrictAttribute> { ["Key"] = new ClassWith_StrictAttribute { Float = 1 } }
            };
            Assert.Equal(@"{""MyDictionary"":{""Key"":{""Float"":1}}}", await Serializer.SerializeWrapper(obj, s_optionReadFromStr));
        }

        public class ClassWithComplexDictionaryProperty
        {
            public Dictionary<string, ClassWith_StrictAttribute> MyDictionary { get; set; }
        }

        [Fact]
        public async Task TypeAttributeAppliesTo_CustomCollectionElements()
        {
            string json = @"[""1""]";
            MyCustomList obj = await Serializer.DeserializeWrapper<MyCustomList>(json);
            Assert.Equal(json, await Serializer.SerializeWrapper(obj));
        }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public class MyCustomList : List<int> { }

        [Fact]
        public async Task TypeAttributeAppliesTo_CustomCollectionElements_HonoredWhenProperty()
        {
            string json = @"{""List"":[""1""]}";
            ClassWithCustomList obj = await Serializer.DeserializeWrapper<ClassWithCustomList>(json);
            Assert.Equal(json, await Serializer.SerializeWrapper(obj));
        }

        public class ClassWithCustomList
        {
            public MyCustomList List { get; set; }
        }

        [Fact]
        public async Task TypeAttributeAppliesTo_CustomDictionaryElements()
        {
            string json = @"{""Key"":""1""}";
            MyCustomDictionary obj = await Serializer.DeserializeWrapper<MyCustomDictionary>(json);
            Assert.Equal(json, await Serializer.SerializeWrapper(obj));
        }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public class MyCustomDictionary : Dictionary<string, int> { }

        [Fact]
        public async Task TypeAttributeAppliesTo_CustomDictionaryElements_HonoredWhenProperty()
        {
            string json = @"{""Dictionary"":{""Key"":""1""}}";
            ClassWithCustomDictionary obj = await Serializer.DeserializeWrapper<ClassWithCustomDictionary>(json);
            Assert.Equal(json, await Serializer.SerializeWrapper(obj));
        }

        public class ClassWithCustomDictionary
        {
            public MyCustomDictionary Dictionary { get; set; }
        }

        [Fact]
        public async Task Attribute_OnType_NotRecursive()
        {
            // Recursive behavior, where number handling setting on a property is applied to subsequent
            // properties in its type closure, would allow a string number. This is not supported.
            string json = @"{""NestedClass"":{""MyInt"":""1""}}";
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<AttributeAppliedToFirstLevelProp>(json));

            var obj = new AttributeAppliedToFirstLevelProp
            {
                NestedClass = new NonNumberType { MyInt = 1 }
            };
            Assert.Equal(@"{""NestedClass"":{""MyInt"":1}}", await Serializer.SerializeWrapper(obj));
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
        public async Task HandlingOnMemberOverridesHandlingOnType_Enumerable()
        {
            string json = @"{""List"":[""1""]}";
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<MyCustomListWrapper>(json));

            var obj = new MyCustomListWrapper
            {
                List = new MyCustomList { 1 }
            };
            Assert.Equal(@"{""List"":[1]}", await Serializer.SerializeWrapper(obj));
        }

        public class MyCustomListWrapper
        {
            [JsonNumberHandling(JsonNumberHandling.Strict)]
            public MyCustomList List { get; set; }
        }

        [Fact]
        public async Task HandlingOnMemberOverridesHandlingOnType_Dictionary()
        {
            string json = @"{""Dictionary"":{""Key"":""1""}}";
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<MyCustomDictionaryWrapper>(json));

            var obj1 = new MyCustomDictionaryWrapper
            {
                Dictionary = new MyCustomDictionary { ["Key"] = 1 }
            };
            Assert.Equal(@"{""Dictionary"":{""Key"":1}}", await Serializer.SerializeWrapper(obj1));
        }

        public class MyCustomDictionaryWrapper
        {
            [JsonNumberHandling(JsonNumberHandling.Strict)]
            public MyCustomDictionary Dictionary { get; set; }
        }

        [Fact]
        public async Task Attribute_Allowed_On_NonNumber_NonCollection_Property()
        {
            const string Json = @"{""MyProp"":{""MyInt"":1}}";

            ClassWith_NumberHandlingOn_ObjectProperty obj = await Serializer.DeserializeWrapper<ClassWith_NumberHandlingOn_ObjectProperty>(Json);
            Assert.Equal(1, obj.MyProp.MyInt);

            string json = await Serializer.SerializeWrapper(obj);
            Assert.Equal(Json, json);
        }

        public class ClassWith_NumberHandlingOn_ObjectProperty
        {
            [JsonNumberHandling(JsonNumberHandling.Strict)]
            public NonNumberType MyProp { get; set; }
        }

        [Fact]
        public async Task Attribute_Allowed_On_Property_WithCustomConverter()
        {
            string json = @"{""Prop"":1}";

            // Converter returns 25 regardless of input.
            var obj = await Serializer.DeserializeWrapper<ClassWith_NumberHandlingOn_Property_WithCustomConverter>(json);
            Assert.Equal(25, obj.Prop);

            // Converter throws this exception regardless of input.
            NotImplementedException ex = await Assert.ThrowsAsync<NotImplementedException>(async () => await Serializer.SerializeWrapper(obj));
            Assert.Equal("Converter was called", ex.Message);
        }

        public class ClassWith_NumberHandlingOn_Property_WithCustomConverter
        {
            [JsonNumberHandling(JsonNumberHandling.Strict)]
            [JsonConverter(typeof(ConverterForInt32))]
            public int Prop { get; set; }
        }

        [Fact]
        public async Task Attribute_Allowed_On_Type_WithCustomConverter()
        {
            string json = @"{}";
            NotImplementedException ex;

            // Assert regular Read/Write methods on custom converter are called.
            ex = await Assert.ThrowsAsync<NotImplementedException>(async () => await Serializer.DeserializeWrapper<ClassWith_NumberHandlingOn_Type_WithCustomConverter>(json));
            Assert.Equal("Converter was called", ex.Message);

            ex = await Assert.ThrowsAsync<NotImplementedException>(async () => await Serializer.SerializeWrapper(new ClassWith_NumberHandlingOn_Type_WithCustomConverter()));
            Assert.Equal("Converter was called", ex.Message);
        }

        [JsonNumberHandling(JsonNumberHandling.Strict)]
        [JsonConverter(typeof(ConverterForMyType))]
        public class ClassWith_NumberHandlingOn_Type_WithCustomConverter
        {
        }

        public class ConverterForMyType : JsonConverter<ClassWith_NumberHandlingOn_Type_WithCustomConverter>
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
        public async Task CustomConverterOverridesBuiltInLogic()
        {
            var options = new JsonSerializerOptions(s_optionReadAndWriteFromStr)
            {
                Converters = { new ConverterForInt32(), new ConverterForFloat() }
            };

            string json = @"""32""";

            // Converter returns 25 regardless of input.
            Assert.Equal(25, await Serializer.DeserializeWrapper<int>(json, options));

            // Converter throws this exception regardless of input.
            NotImplementedException ex = await Assert.ThrowsAsync<NotImplementedException>(async () => await Serializer.SerializeWrapper(4, options));
            Assert.Equal("Converter was called", ex.Message);

            json = @"""NaN""";

            // Converter returns 25 if NaN.
            Assert.Equal(25, await Serializer.DeserializeWrapper<float?>(json, options));

            // Converter writes 25 if NaN.
            Assert.Equal("25", await Serializer.SerializeWrapper((float?)float.NaN, options));
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
            Assert.Contains("value", ex.Message);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new JsonSerializerOptions { NumberHandling = (JsonNumberHandling)(8) });

            ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new JsonNumberHandlingAttribute((JsonNumberHandling)(-1)));
            Assert.Contains("handling", ex.Message);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new JsonNumberHandlingAttribute((JsonNumberHandling)(8)));
        }

        [Fact]
        public async Task InternalCollectionConverter_CustomNumberConverter_GlobalOption()
        {
            NotImplementedException ex;

            var list = new List<int> { 1 };
            var options = new JsonSerializerOptions(s_optionReadAndWriteFromStr)
            {
                Converters = { new ConverterForInt32() }
            };

            // Assert converter methods are called and not Read/WriteWithNumberHandling (which would throw InvalidOperationException).
            // Converter returns 25 regardless of input.
            Assert.Equal(25, (await Serializer.DeserializeWrapper<List<int>>(@"[""1""]", options))[0]);
            // Converter throws this exception regardless of input.
            ex = await Assert.ThrowsAsync<NotImplementedException>(async () => await Serializer.SerializeWrapper(list, options));
            Assert.Equal("Converter was called", ex.Message);

            var list2 = new List<int?> { 1 };
            Assert.Equal(25, (await Serializer.DeserializeWrapper<List<int?>>(@"[""1""]", options))[0]);
            ex = await Assert.ThrowsAsync<NotImplementedException>(async () => await Serializer.SerializeWrapper(list2, options));
            Assert.Equal("Converter was called", ex.Message);

            // Okay to set number handling for number collection property when number is handled with custom converter;
            // converter Read/Write methods called.
            ClassWithListPropAndAttribute obj1 = await Serializer.DeserializeWrapper<ClassWithListPropAndAttribute>(@"{""Prop"":[""1""]}", options);
            Assert.Equal(25, obj1.Prop[0]);
            ex = await Assert.ThrowsAsync<NotImplementedException>(async () => await Serializer.SerializeWrapper(obj1, options));
            Assert.Equal("Converter was called", ex.Message);

            ClassWithDictPropAndAttribute obj2 = await Serializer.DeserializeWrapper<ClassWithDictPropAndAttribute>(@"{""Prop"":{""1"":""1""}}", options);
            Assert.Equal(25, obj2.Prop[1]);
            ex = await Assert.ThrowsAsync<NotImplementedException>(async () => await Serializer.SerializeWrapper(obj2, options));
            Assert.Equal("Converter was called", ex.Message);
        }

        public class ClassWithListPropAndAttribute
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public List<int> Prop { get; set; }
        }

        public class ClassWithDictPropAndAttribute
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            public Dictionary<int, int?> Prop { get; set; }
        }

        [Fact]
        public async Task InternalCollectionConverter_CustomNumberConverter_OnProperty()
        {
            // Invalid to set number handling for number collection property when number is handled with custom converter.
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ClassWithListPropAndAttribute_ConverterOnProp>(""));
            Assert.Contains(nameof(ClassWithListPropAndAttribute_ConverterOnProp), ex.Message);
            Assert.Contains("IntProp", ex.Message);

            ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(new ClassWithListPropAndAttribute_ConverterOnProp()));
            Assert.Contains(nameof(ClassWithListPropAndAttribute_ConverterOnProp), ex.Message);
            Assert.Contains("IntProp", ex.Message);

#if !BUILDING_SOURCE_GENERATOR_TESTS
            // Source-gen isn't currently validating that the converter on the test prop
            // is invalid so JsonException is being thrown instead due to invalid JSON.
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/73714"]
            ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<ClassWithDictPropAndAttribute_ConverterOnProp>(""));
            Assert.Contains(nameof(ClassWithDictPropAndAttribute_ConverterOnProp), ex.Message);
            Assert.Contains("IntProp", ex.Message);

            ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(new ClassWithDictPropAndAttribute_ConverterOnProp()));
            Assert.Contains(nameof(ClassWithDictPropAndAttribute_ConverterOnProp), ex.Message);
            Assert.Contains("IntProp", ex.Message);
#endif
        }

        public class ClassWithListPropAndAttribute_ConverterOnProp
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
            [JsonConverter(typeof(ListOfIntConverter))]
            public List<int> IntProp { get; set; }
        }

        public class ClassWithDictPropAndAttribute_ConverterOnProp
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
        public async Task InternalCollectionConverter_CustomNullableNumberConverter()
        {
            NotImplementedException ex;

            var dict = new Dictionary<int, int?> { [1] = 1 };
            var options = new JsonSerializerOptions(s_optionReadAndWriteFromStr)
            {
                Converters = { new ConverterForNullableInt32() }
            };

            // Assert converter methods are called and not Read/WriteWithNumberHandling (which would throw InvalidOperationException).
            // Converter returns 25 regardless of input.
            Assert.Equal(25, (await Serializer.DeserializeWrapper<Dictionary<int, int?>>(@"{""1"":""1""}", options))[1]);
            ex = await Assert.ThrowsAsync<NotImplementedException>(async () => await Serializer.SerializeWrapper(dict, options));
            Assert.Equal("Converter was called", ex.Message);

            var obj = await Serializer.DeserializeWrapper<ClassWithDictPropAndAttribute>(@"{""Prop"":{""1"":""1""}}", options);
            Assert.Equal(25, obj.Prop[1]);
            ex = await Assert.ThrowsAsync<NotImplementedException>(async () => await Serializer.SerializeWrapper(obj, options));
            await Assert.ThrowsAsync<NotImplementedException>(async () => await Serializer.SerializeWrapper(dict, options));
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
        public async Task AdaptableCustomConverter()
        {
            // Baseline without custom converter
            PlainClassWithList obj = new() { Prop = new List<int>() { 1 } };
            string json = await Serializer.SerializeWrapper(obj, s_optionReadAndWriteFromStr);
            Assert.Equal("{\"Prop\":[\"1\"]}", json);

            obj = await Serializer.DeserializeWrapper<PlainClassWithList>(json, s_optionReadAndWriteFromStr);
            Assert.Equal(1, obj.Prop[0]);

            // First with numbers
            JsonSerializerOptions options = new()
            {
                Converters = { new AdaptableInt32Converter() }
            };

            obj = new PlainClassWithList() { Prop = new List<int>() { 1 } };
            json = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("{\"Prop\":[101]}", json);

            obj = await Serializer.DeserializeWrapper<PlainClassWithList>(json, options);
            Assert.Equal(1, obj.Prop[0]);

            // Then with strings
            options = new JsonSerializerOptions()
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
                Converters = { new AdaptableInt32Converter() }
            };

            obj = new PlainClassWithList() { Prop = new List<int>() { 1 } };
            json = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("{\"Prop\":[\"101\"]}", json);

            obj = await Serializer.DeserializeWrapper<PlainClassWithList>(json, options);
            Assert.Equal(1, obj.Prop[0]);
        }

        public class PlainClassWithList
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
}
