// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class NullableTests
    {
        [Fact]
        public static void DictionaryWithNullableValue()
        {
            Dictionary<string, float?> dictWithFloatValue = new Dictionary<string, float?> { { "key", 42.0f } };
            Dictionary<string, float?> dictWithFloatNull = new Dictionary<string, float?> { { "key", null } };
            TestDictionaryWithNullableValue<Dictionary<string, float?>, Dictionary<string, Dictionary<string, float?>>, float?>(
                dictWithFloatValue,
                dictWithFloatNull,
                dictOfDictWithValue: new Dictionary<string, Dictionary<string, float?>> { { "key", dictWithFloatValue } },
                dictOfDictWithNull: new Dictionary<string, Dictionary<string, float?>> { { "key", dictWithFloatNull } },
                42.0f);

            DateTime now = DateTimeTestHelpers.FixedDateTimeValue;
            Dictionary<string, DateTime?> dictWithDateTimeValue = new Dictionary<string, DateTime?> { { "key", now } };
            Dictionary<string, DateTime?> dictWithDateTimeNull = new Dictionary<string, DateTime?> { { "key", null } };
            TestDictionaryWithNullableValue<Dictionary<string, DateTime?>, Dictionary<string, Dictionary<string, DateTime?>>, DateTime?>(
                dictWithDateTimeValue,
                dictWithDateTimeNull,
                dictOfDictWithValue: new Dictionary<string, Dictionary<string, DateTime?>> { { "key", dictWithDateTimeValue } },
                dictOfDictWithNull: new Dictionary<string, Dictionary<string, DateTime?>> { { "key", dictWithDateTimeNull } },
                now);

            MyDictionaryWrapper<float?> dictWrapperWithFloatValue = new MyDictionaryWrapper<float?>() { { "key", 42.0f } };
            MyDictionaryWrapper<float?> dictWrapperWithFloatNull = new MyDictionaryWrapper<float?>() { { "key", null } };
            TestDictionaryWithNullableValue<MyDictionaryWrapper<float?>, MyDictionaryWrapper<MyDictionaryWrapper<float?>>, float?>(
                dictWrapperWithFloatValue,
                dictWrapperWithFloatNull,
                dictOfDictWithValue: new MyDictionaryWrapper<MyDictionaryWrapper<float?>> { { "key", dictWrapperWithFloatValue } },
                dictOfDictWithNull: new MyDictionaryWrapper<MyDictionaryWrapper<float?>> { { "key", dictWrapperWithFloatNull } },
                42.0f);

            MyIDictionaryWrapper<float?> idictWrapperWithFloatValue = new MyIDictionaryWrapper<float?>() { { "key", 42.0f } };
            MyIDictionaryWrapper<float?> idictWrapperWithFloatNull = new MyIDictionaryWrapper<float?>() { { "key", null } };
            TestDictionaryWithNullableValue<MyIDictionaryWrapper<float?>, MyIDictionaryWrapper<MyIDictionaryWrapper<float?>>, float?>(
                idictWrapperWithFloatValue,
                idictWrapperWithFloatNull,
                dictOfDictWithValue: new MyIDictionaryWrapper<MyIDictionaryWrapper<float?>> { { "key", idictWrapperWithFloatValue } },
                dictOfDictWithNull: new MyIDictionaryWrapper<MyIDictionaryWrapper<float?>> { { "key", idictWrapperWithFloatNull } },
                42.0f);

            IDictionary<string, DateTime?> idictWithDateTimeValue = new Dictionary<string, DateTime?> { { "key", now } };
            IDictionary<string, DateTime?> idictWithDateTimeNull = new Dictionary<string, DateTime?> { { "key", null } };
            TestDictionaryWithNullableValue<IDictionary<string, DateTime?>, IDictionary<string, IDictionary<string, DateTime?>>, DateTime?>(
                idictWithDateTimeValue,
                idictWithDateTimeNull,
                dictOfDictWithValue: new Dictionary<string, IDictionary<string, DateTime?>> { { "key", idictWithDateTimeValue } },
                dictOfDictWithNull: new Dictionary<string, IDictionary<string, DateTime?>> { { "key", idictWithDateTimeNull } },
                now);

            ImmutableDictionary<string, DateTime?> immutableDictWithDateTimeValue = ImmutableDictionary.CreateRange(new Dictionary<string, DateTime?> { { "key", now } });
            ImmutableDictionary<string, DateTime?> immutableDictWithDateTimeNull = ImmutableDictionary.CreateRange(new Dictionary<string, DateTime?> { { "key", null } });
            TestDictionaryWithNullableValue<ImmutableDictionary<string, DateTime?>, ImmutableDictionary<string, ImmutableDictionary<string, DateTime?>>, DateTime?>(
                immutableDictWithDateTimeValue,
                immutableDictWithDateTimeNull,
                dictOfDictWithValue: ImmutableDictionary.CreateRange(new Dictionary<string, ImmutableDictionary<string, DateTime?>> { { "key", immutableDictWithDateTimeValue } }),
                dictOfDictWithNull: ImmutableDictionary.CreateRange(new Dictionary<string, ImmutableDictionary<string, DateTime?>> { { "key", immutableDictWithDateTimeNull } }),
                now);
        }

        public class MyOverflowWrapper
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement?> MyOverflow { get; set; }
        }

        public class MyMultipleOverflowWrapper
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> MyValidOverflow { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement?> MyInvalidOverflow { get; set; }
        }

        public class AnotherMultipleOverflowWrapper
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement?> MyInvalidOverflow { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> MyValidOverflow { get; set; }
        }

        public class AnotherOverflowWrapper
        {
            public MyOverflowWrapper Wrapper { get; set; }
        }

        [Fact]
        public static void ExtensionDataWithNullableJsonElement_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<MyOverflowWrapper>(@"{""key"":""value""}"));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<AnotherOverflowWrapper>(@"{""Wrapper"": {""key"":""value""}}"));

            // Having multiple extension properties is not allowed.
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<MyMultipleOverflowWrapper>(@"{""key"":""value""}"));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<AnotherMultipleOverflowWrapper>(@"{""key"":""value""}"));
        }

        private static void TestDictionaryWithNullableValue<TDict, TDictOfDict, TValue>(
            TDict dictWithValue,
            TDict dictWithNull,
            TDictOfDict dictOfDictWithValue,
            TDictOfDict dictOfDictWithNull,
            TValue value)
        {
            string valueSerialized = JsonSerializer.Serialize(value);

            static void ValidateDict(TDict dict, TValue expectedValue)
            {
                IDictionary<string, TValue> genericIDict = (IDictionary<string, TValue>)dict;
                Assert.Equal(1, genericIDict.Count);
                Assert.Equal(expectedValue, genericIDict["key"]);
            }

            static void ValidateDictOfDict(TDictOfDict dictOfDict, TValue expectedValue)
            {
                IDictionary<string, TDict> genericIDict = (IDictionary<string, TDict>)dictOfDict;
                Assert.Equal(1, genericIDict.Count);

                IDictionary<string, TValue> nestedDict = (IDictionary<string, TValue>)genericIDict["key"];
                Assert.Equal(1, nestedDict.Count);
                Assert.Equal(expectedValue, nestedDict["key"]);
            }

            string json = JsonSerializer.Serialize(dictWithValue);
            Assert.Equal(@"{""key"":" + valueSerialized + "}", json);

            TDict parsedDictWithValue = JsonSerializer.Deserialize<TDict>(json);
            ValidateDict(parsedDictWithValue, value);

            json = JsonSerializer.Serialize(dictWithNull);
            Assert.Equal(@"{""key"":null}", json);

            TDict parsedDictWithNull = JsonSerializer.Deserialize<TDict>(json);
            ValidateDict(parsedDictWithNull, default);

            // Test nested dicts with nullable values.
            json = JsonSerializer.Serialize(dictOfDictWithValue);
            Assert.Equal(@"{""key"":{""key"":" + valueSerialized + "}}", json);

            TDictOfDict parsedDictOfDictWithValue = JsonSerializer.Deserialize<TDictOfDict>(json);
            ValidateDictOfDict(parsedDictOfDictWithValue, value);

            json = JsonSerializer.Serialize(dictOfDictWithNull);
            Assert.Equal(@"{""key"":{""key"":null}}", json);

            TDictOfDict parsedDictOfDictWithNull = JsonSerializer.Deserialize<TDictOfDict>(json);
            ValidateDictOfDict(parsedDictOfDictWithNull, default);
        }

        public class SimpleClassWithDictionariesWithNullableValues
        {
            public Dictionary<string, DateTime?> Dict { get; set; }
            public IDictionary<string, DateTime?> IDict { get; set; }
            public ImmutableDictionary<string, DateTime?> ImmutableDict { get; set; }
            public ImmutableSortedDictionary<string, DateTime?> ImmutableSortedDict { get; set; }
        }

        [Fact]
        public static void ClassWithDictionariesWithNullableValues()
        {
            string json =
                @"{
                    ""Dict"": {""key"": ""1995-04-16""},
                    ""IDict"": {""key"": null},
                    ""ImmutableDict"": {""key"": ""1997-03-22""},
                    ""ImmutableSortedDict"": { ""key"": null}
                }";

            SimpleClassWithDictionariesWithNullableValues obj = JsonSerializer.Deserialize<SimpleClassWithDictionariesWithNullableValues>(json);
            Assert.Equal(new DateTime(1995, 4, 16), obj.Dict["key"]);
            Assert.Null(obj.IDict["key"]);
            Assert.Equal(new DateTime(1997, 3, 22), obj.ImmutableDict["key"]);
            Assert.Null(obj.ImmutableSortedDict["key"]);

            string serialized = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""Dict"":{""key"":""1995-04-16T00:00:00""}", serialized);
            Assert.Contains(@"""IDict"":{""key"":null}", serialized);
            Assert.Contains(@"""ImmutableDict"":{""key"":""1997-03-22T00:00:00""}", serialized);
            Assert.Contains(@"""ImmutableSortedDict"":{""key"":null}", serialized);
        }

        [Fact]
        public static void EnumerableWithNullableValue()
        {
            IEnumerable<float?> ieWithFloatValue = new List<float?> { 42.0f };
            IEnumerable<float?> ieWithFloatNull = new List<float?> { null };
            TestEnumerableWithNullableValue<IEnumerable<float?>, IEnumerable<IEnumerable<float?>>, float?>(
                ieWithFloatValue,
                ieWithFloatNull,
                enumerableOfEnumerableWithValue: new List<IEnumerable<float?>> { ieWithFloatValue },
                enumerableOfEnumerableWithNull: new List<IEnumerable<float?>> { ieWithFloatNull },
                42.0f);

            DateTime now = DateTimeTestHelpers.FixedDateTimeValue;
            IEnumerable<DateTime?> ieWithDateTimeValue = new List<DateTime?> { now };
            IEnumerable<DateTime?> ieWithDateTimeNull = new List<DateTime?> { null };
            TestEnumerableWithNullableValue<IEnumerable<DateTime?>, IEnumerable<IEnumerable<DateTime?>>, DateTime?>(
                ieWithDateTimeValue,
                ieWithDateTimeNull,
                enumerableOfEnumerableWithValue: new List<IEnumerable<DateTime?>> { ieWithDateTimeValue },
                enumerableOfEnumerableWithNull: new List<IEnumerable<DateTime?>> { ieWithDateTimeNull },
                now);

            IReadOnlyList<DateTime?> irlWithDateTimeValue = new List<DateTime?> { now };
            IReadOnlyList<DateTime?> irlWithDateTimeNull = new List<DateTime?> { null };
            TestEnumerableWithNullableValue<IReadOnlyList<DateTime?>, IReadOnlyList<IReadOnlyList<DateTime?>>, DateTime?>(
                irlWithDateTimeValue,
                irlWithDateTimeNull,
                enumerableOfEnumerableWithValue: new List<IReadOnlyList<DateTime?>> { irlWithDateTimeValue },
                enumerableOfEnumerableWithNull: new List<IReadOnlyList<DateTime?>> { irlWithDateTimeNull },
                now);

            Stack<DateTime?> stWithDateTimeValue = new Stack<DateTime?>();
            stWithDateTimeValue.Push(now);

            Stack<DateTime?> stWithDateTimeNull = new Stack<DateTime?>();
            stWithDateTimeNull.Push(null);

            Stack<Stack<DateTime?>> enumerableOfEnumerableWithValue = new Stack<Stack<DateTime?>>();
            enumerableOfEnumerableWithValue.Push(stWithDateTimeValue);

            Stack<Stack<DateTime?>> enumerableOfEnumerableWithNull = new Stack<Stack<DateTime?>>();
            enumerableOfEnumerableWithNull.Push(stWithDateTimeNull);

            TestEnumerableWithNullableValue<Stack<DateTime?>, Stack<Stack<DateTime?>>, DateTime?>(
                stWithDateTimeValue,
                stWithDateTimeNull,
                enumerableOfEnumerableWithValue,
                enumerableOfEnumerableWithNull,
                now);

            IImmutableList<DateTime?> imlWithDateTimeValue = ImmutableList.CreateRange(new List<DateTime?> { now });
            IImmutableList<DateTime?> imlWithDateTimeNull = ImmutableList.CreateRange(new List<DateTime?> { null });
            TestEnumerableWithNullableValue<IImmutableList<DateTime?>, IImmutableList<IImmutableList<DateTime?>>, DateTime?>(
                imlWithDateTimeValue,
                imlWithDateTimeNull,
                enumerableOfEnumerableWithValue: ImmutableList.CreateRange(new List<IImmutableList<DateTime?>> { imlWithDateTimeValue }),
                enumerableOfEnumerableWithNull: ImmutableList.CreateRange(new List<IImmutableList<DateTime?>> { imlWithDateTimeNull }),
                now);
        }

        private static void TestEnumerableWithNullableValue<TEnumerable, TEnumerableOfEnumerable, TValue>(
            TEnumerable enumerableWithValue,
            TEnumerable enumerableWithNull,
            TEnumerableOfEnumerable enumerableOfEnumerableWithValue,
            TEnumerableOfEnumerable enumerableOfEnumerableWithNull,
            TValue value)
        {
            string valueSerialized = JsonSerializer.Serialize(value);

            static void ValidateEnumerable(TEnumerable enumerable, TValue expectedValue)
            {
                IEnumerable<TValue> ienumerable = (IEnumerable<TValue>)enumerable;
                int count = 0;
                foreach (TValue val in ienumerable)
                {
                    Assert.Equal(expectedValue, val);
                    count += 1;
                }
                Assert.Equal(1, count);
            }

            static void ValidateEnumerableOfEnumerable(TEnumerableOfEnumerable dictOfDict, TValue expectedValue)
            {
                IEnumerable<TEnumerable> ienumerable = (IEnumerable<TEnumerable>)dictOfDict;
                int ienumerableCount = 0;
                int nestedIEnumerableCount = 0;

                foreach (IEnumerable<TValue> nestedIEnumerable in ienumerable)
                {
                    foreach (TValue val in nestedIEnumerable)
                    {
                        Assert.Equal(expectedValue, val);
                        nestedIEnumerableCount += 1;
                    }
                    ienumerableCount += 1;
                }
                Assert.Equal(1, ienumerableCount);
                Assert.Equal(1, nestedIEnumerableCount);
            }

            string json = JsonSerializer.Serialize(enumerableWithValue);
            Assert.Equal($"[{valueSerialized}]", json);

            TEnumerable parsedEnumerableWithValue = JsonSerializer.Deserialize<TEnumerable>(json);
            ValidateEnumerable(parsedEnumerableWithValue, value);

            json = JsonSerializer.Serialize(enumerableWithNull);
            Assert.Equal("[null]", json);

            TEnumerable parsedEnumerableWithNull = JsonSerializer.Deserialize<TEnumerable>(json);
            ValidateEnumerable(parsedEnumerableWithNull, default);

            // Test nested dicts with nullable values.
            json = JsonSerializer.Serialize(enumerableOfEnumerableWithValue);
            Assert.Equal($"[[{valueSerialized}]]", json);

            TEnumerableOfEnumerable parsedEnumerableOfEnumerableWithValue = JsonSerializer.Deserialize<TEnumerableOfEnumerable>(json);
            ValidateEnumerableOfEnumerable(parsedEnumerableOfEnumerableWithValue, value);

            json = JsonSerializer.Serialize(enumerableOfEnumerableWithNull);
            Assert.Equal("[[null]]", json);

            TEnumerableOfEnumerable parsedEnumerableOfEnumerableWithNull = JsonSerializer.Deserialize<TEnumerableOfEnumerable>(json);
            ValidateEnumerableOfEnumerable(parsedEnumerableOfEnumerableWithNull, default);
        }

        public class MyDictionaryWrapper<TValue> : Dictionary<string, TValue> { }

        public class MyIDictionaryWrapper<TValue> : IDictionary<string, TValue>
        {
            Dictionary<string, TValue> dict = new Dictionary<string, TValue>();

            // Derived types need default constructors to be supported.
            public MyIDictionaryWrapper() { }

            public TValue this[string key] { get => ((IDictionary<string, TValue>)dict)[key]; set => ((IDictionary<string, TValue>)dict)[key] = value; }

            public ICollection<string> Keys => ((IDictionary<string, TValue>)dict).Keys;

            public ICollection<TValue> Values => ((IDictionary<string, TValue>)dict).Values;

            public int Count => ((IDictionary<string, TValue>)dict).Count;

            public bool IsReadOnly => ((IDictionary<string, TValue>)dict).IsReadOnly;

            public void Add(string key, TValue value)
            {
                ((IDictionary<string, TValue>)dict).Add(key, value);
            }

            public void Add(KeyValuePair<string, TValue> item)
            {
                ((IDictionary<string, TValue>)dict).Add(item);
            }

            public void Clear()
            {
                ((IDictionary<string, TValue>)dict).Clear();
            }

            public bool Contains(KeyValuePair<string, TValue> item)
            {
                return ((IDictionary<string, TValue>)dict).Contains(item);
            }

            public bool ContainsKey(string key)
            {
                return ((IDictionary<string, TValue>)dict).ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
            {
                ((IDictionary<string, TValue>)dict).CopyTo(array, arrayIndex);
            }

            public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
            {
                return ((IDictionary<string, TValue>)dict).GetEnumerator();
            }

            public bool Remove(string key)
            {
                return ((IDictionary<string, TValue>)dict).Remove(key);
            }

            public bool Remove(KeyValuePair<string, TValue> item)
            {
                return ((IDictionary<string, TValue>)dict).Remove(item);
            }

            public bool TryGetValue(string key, out TValue value)
            {
                return ((IDictionary<string, TValue>)dict).TryGetValue(key, out value);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IDictionary<string, TValue>)dict).GetEnumerator();
            }
        }

        [Fact]
        public static void NullableCustomStructRoundtrip()
        {
            string serialized = JsonSerializer.Serialize(new ClassWithNullablePerson
            {
                Person = new Person
                {
                    FirstName = "John",
                    Age = 24
                }
            });
            Assert.Contains(@"{""Person"":{", serialized);
            Assert.Contains(@"""FirstName"":""John""", serialized);
            Assert.Contains(@"""Age"":24", serialized);
            Assert.Contains(@"""Birthday"":null", serialized);
            Assert.DoesNotContain(@"""Value"":{""", serialized);
            Assert.DoesNotContain(@"""HasValue"":""", serialized);

            var obj = JsonSerializer.Deserialize<ClassWithNullablePerson>(serialized);
            Assert.Equal("John", obj.Person?.FirstName);
            Assert.Equal(24, obj.Person?.Age);
            Assert.Null(obj.Person?.Birthday);

            serialized = JsonSerializer.Serialize(new ClassWithNullablePerson());
            Assert.Equal(@"{""Person"":null}", serialized);

            obj = JsonSerializer.Deserialize<ClassWithNullablePerson>(serialized);
            Assert.Null(obj.Person);
        }

        public class ClassWithNullablePerson
        {
            public Person? Person { get; set; }
        }

        public struct Person
        {
            public string FirstName { get; set; }
            public int? Age { get; set; }
            public DateTime? Birthday { get; set; }
        }
    }
}
