// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public partial class DictionaryTests
    {
        public abstract class DictionaryKeyTestsBase<TKey, TValue>
        {
            protected abstract TKey Key { get; }
            protected abstract TValue Value { get; }
            protected virtual string _expectedJson => $"{{\"{Key}\":{Value}}}";

            protected virtual void Validate(Dictionary<TKey, TValue> dictionary)
            {
                bool success = dictionary.TryGetValue(Key, out TValue value);
                Assert.True(success);
                Assert.Equal(Value, value);
            }

            private Dictionary<TKey, TValue> BuildDictionary()
            {
                var dictionary = new Dictionary<TKey, TValue>();
                dictionary.Add(Key, Value);

                return dictionary;
            }

            [Fact]
            public void TestDictionaryKey()
            {
                Dictionary<TKey, TValue> dictionary = BuildDictionary();

                string json = JsonSerializer.Serialize(dictionary);
                Assert.Equal(_expectedJson, json);

                Dictionary<TKey, TValue> dictionaryCopy = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(json);
                Validate(dictionaryCopy);
            }

            [Fact]
            public async Task TestDictionaryKeyAsync()
            {
                Dictionary<TKey, TValue> dictionary = BuildDictionary();

                MemoryStream serializeStream = new MemoryStream();
                await JsonSerializer.SerializeAsync(serializeStream, dictionary);
                string json = Encoding.UTF8.GetString(serializeStream.ToArray());
                Assert.Equal(_expectedJson, json);

                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                Stream deserializeStream = new MemoryStream(jsonBytes);
                Dictionary<TKey, TValue> dictionaryCopy = await JsonSerializer.DeserializeAsync<Dictionary<TKey, TValue>>(deserializeStream);
                Validate(dictionaryCopy);
            }
        }

        public class DictionaryBoolKey : DictionaryKeyTestsBase<bool, int>
        {
            protected override bool Key => true;
            protected override int Value => 1;
        }

        public class DictionaryByteKey : DictionaryKeyTestsBase<byte, int>
        {
            protected override byte Key => byte.MaxValue;
            protected override int Value => 1;
        }

        public class DictionaryCharKey : DictionaryKeyTestsBase<char, char>
        {
            protected override string _expectedJson => @"{""\uFFFF"":""\uFFFF""}";
            protected override char Key => char.MaxValue;
            protected override char Value => char.MaxValue;
        }

        public class DictionaryDateTimeKey : DictionaryKeyTestsBase<DateTime, int>
        {
            protected override string _expectedJson => $@"{{""{DateTime.MaxValue:O}"":1}}";
            protected override DateTime Key => DateTime.MaxValue;
            protected override int Value => 1;
        }

        public class DictionaryDateTimeOffsetKey : DictionaryKeyTestsBase<DateTimeOffset, int>
        {
            protected override string _expectedJson => $@"{{""{DateTimeOffset.MaxValue:O}"":1}}";
            protected override DateTimeOffset Key => DateTimeOffset.MaxValue;
            protected override int Value => 1;
        }

        public class DictionaryDecimalKey : DictionaryKeyTestsBase<decimal, int>
        {
            protected override string _expectedJson => $@"{{""{JsonSerializer.Serialize(decimal.MaxValue)}"":1}}";
            protected override decimal Key => decimal.MaxValue;
            protected override int Value => 1;
        }

        public class DictionaryDoubleKey : DictionaryKeyTestsBase<double, int>
        {
            protected override string _expectedJson => $@"{{""{JsonSerializer.Serialize(double.MaxValue)}"":1}}";
            protected override double Key => double.MaxValue;
            protected override int Value => 1;
        }

        public class DictionaryEnumKey : DictionaryKeyTestsBase<MyEnum, int>
        {
            protected override MyEnum Key => MyEnum.Foo;
            protected override int Value => 1;
        }

        public class DictionaryEnumFlagsKey : DictionaryKeyTestsBase<MyEnumFlags, int>
        {
            protected override MyEnumFlags Key => MyEnumFlags.Foo | MyEnumFlags.Bar;
            protected override int Value => 1;
        }

        public class DictionaryGuidKey : DictionaryKeyTestsBase<Guid, int>
        {
            // Use singleton pattern here so the Guid key does not change everytime this is called.
            protected override Guid Key { get; } = Guid.NewGuid();
            protected override int Value => 1;
        }

        public class DictionaryInt16Key : DictionaryKeyTestsBase<short, int>
        {
            protected override short Key => short.MaxValue;
            protected override int Value => 1;
        }

        public class DictionaryInt32Key : DictionaryKeyTestsBase<int, int>
        {
            protected override int Key => int.MaxValue;
            protected override int Value => 1;
        }

        public class DictionaryInt64Key : DictionaryKeyTestsBase<long, int>
        {
            protected override long Key => long.MaxValue;
            protected override int Value => 1;
        }

        public class DictionarySByteKey : DictionaryKeyTestsBase<sbyte, int>
        {
            protected override sbyte Key => sbyte.MaxValue;
            protected override int Value => 1;
        }

        public class DictionarySingleKey : DictionaryKeyTestsBase<float, int>
        {
            protected override string _expectedJson => $@"{{""{JsonSerializer.Serialize(float.MaxValue)}"":1}}";
            protected override float Key => float.MaxValue;
            protected override int Value => 1;
        }

        public class DictionaryStringKey : DictionaryKeyTestsBase<string, int>
        {
            protected override string Key => "KeyString";
            protected override int Value => 1;
        }

        public class DictionaryUInt16Key : DictionaryKeyTestsBase<ushort, int>
        {
            protected override ushort Key => ushort.MaxValue;
            protected override int Value => 1;
        }

        public class DictionaryUInt32Key : DictionaryKeyTestsBase<uint, int>
        {
            protected override uint Key => uint.MaxValue;
            protected override int Value => 1;
        }

        public class DictionaryUInt64Key : DictionaryKeyTestsBase<ulong, int>
        {
            protected override ulong Key => ulong.MaxValue;
            protected override int Value => 1;
        }

        public abstract class DictionaryUnsupportedKeyTestsBase<TKey, TValue>
        {
            private Dictionary<TKey, TValue> _dictionary => BuildDictionary();
            protected abstract TKey Key { get; }
            private Dictionary<TKey, TValue> BuildDictionary()
            {
                return new Dictionary<TKey, TValue>() { { Key, default } };
            }

            [Fact]
            public void ThrowUnsupported_Serialize()
                => Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(_dictionary));

            [Fact]
            public Task ThrowUnsupported_SerializeAsync()
                => Assert.ThrowsAsync<NotSupportedException>(() => JsonSerializer.SerializeAsync(new MemoryStream(), _dictionary));

            [Fact]
            public void ThrowUnsupported_Deserialize() => Assert.Throws<NotSupportedException>(()
                => JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(@"{""foo"":1}"));

            [Fact]
            public Task ThrowUnsupported_DeserializeAsync() => Assert.ThrowsAsync<NotSupportedException>(()
                => JsonSerializer.DeserializeAsync<Dictionary<TKey, TValue>>(new MemoryStream(Encoding.UTF8.GetBytes(@"{""foo"":1}"))).AsTask());

            [Fact]
            public void DoesNotThrowIfEmpty_Serialize()
                => JsonSerializer.Serialize(new Dictionary<TKey, TValue>());

            [Fact]
            public Task DoesNotThrowIfEmpty_SerializeAsync()
                => JsonSerializer.SerializeAsync(new MemoryStream(), new Dictionary<TKey, TValue>());

            [Fact]
            public void DoesNotThrowIfEmpty_Deserialize()
                => JsonSerializer.Deserialize<Dictionary<TKey, TValue>>("{}");

            [Fact]
            public Task DoesNotThrowIfEmpty_DeserializeAsync()
                => JsonSerializer.DeserializeAsync<Dictionary<TKey, TValue>>(new MemoryStream(Encoding.UTF8.GetBytes("{}"))).AsTask();
        }

        public class DictionaryMyPublicClassKeyUnsupported : DictionaryUnsupportedKeyTestsBase<MyPublicClass, int>
        {
            protected override MyPublicClass Key => new MyPublicClass();
        }

        public class DictionaryMyPublicStructKeyUnsupported : DictionaryUnsupportedKeyTestsBase<MyPublicStruct, int>
        {
            protected override MyPublicStruct Key => new MyPublicStruct();
        }

        public class DictionaryUriKeyUnsupported : DictionaryUnsupportedKeyTestsBase<Uri, int>
        {
            protected override Uri Key => new Uri("http://foo");
        }

        public class DictionaryObjectKeyUnsupported : DictionaryUnsupportedKeyTestsBase<object, int>
        {
            protected override object Key => new object();
        }

        public class DictionaryPolymorphicKeyUnsupported : DictionaryUnsupportedKeyTestsBase<object, int>
        {
            protected override object Key => new Uri("http://foo");
        }

        public class DictionaryNonStringKeyTests
        {
            [Fact]
            public void TestGenericDictionaryKeyObject()
            {
                var dictionary = new Dictionary<object, object>();
                // Add multiple supported types.
                dictionary.Add(1, 1);
                dictionary.Add(new Guid("08314FA2-B1FE-4792-BCD1-6E62338AC7F3"), 2);
                dictionary.Add("KeyString", 3);
                dictionary.Add(MyEnum.Foo, 4);
                dictionary.Add(MyEnumFlags.Foo | MyEnumFlags.Bar, 5);

                const string expected = @"{""1"":1,""08314fa2-b1fe-4792-bcd1-6e62338ac7f3"":2,""KeyString"":3,""Foo"":4,""Foo, Bar"":5}";

                string json = JsonSerializer.Serialize(dictionary);
                Assert.Equal(expected, json);
                // object type is not supported on deserialization.
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Dictionary<object, object>>(json));

                var @object = new ClassWithDictionary { Dictionary = dictionary };
                json = JsonSerializer.Serialize(@object);
                Assert.Equal($@"{{""Dictionary"":{expected}}}", json);
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithDictionary>(json));
            }

            [Fact]
            public void TestNonGenericDictionaryKeyObject()
            {
                IDictionary dictionary = new OrderedDictionary();
                // Add multiple supported types.
                dictionary.Add(1, 1);
                dictionary.Add(new Guid("08314FA2-B1FE-4792-BCD1-6E62338AC7F3"), 2);
                dictionary.Add("KeyString", 3);
                dictionary.Add(MyEnum.Foo, 4);
                dictionary.Add(MyEnumFlags.Foo | MyEnumFlags.Bar, 5);

                const string expected = @"{""1"":1,""08314fa2-b1fe-4792-bcd1-6e62338ac7f3"":2,""KeyString"":3,""Foo"":4,""Foo, Bar"":5}";
                string json = JsonSerializer.Serialize(dictionary);
                Assert.Equal(expected, json);

                dictionary = JsonSerializer.Deserialize<IDictionary>(json);
                Assert.IsType<Dictionary<string, object>>(dictionary);

                dictionary = JsonSerializer.Deserialize<OrderedDictionary>(json);
                foreach (object key in dictionary.Keys)
                {
                    Assert.IsType<string>(key);
                }

                var @object = new ClassWithIDictionary { Dictionary = dictionary };
                json = JsonSerializer.Serialize(@object);
                Assert.Equal($@"{{""Dictionary"":{expected}}}", json);

                @object = JsonSerializer.Deserialize<ClassWithIDictionary>(json);
                Assert.IsType<Dictionary<string, object>>(@object.Dictionary);
            }

            [Theory] // Extend this test when support for more types is added.
            [InlineData(@"{""1.1"":1}", typeof(Dictionary<int, int>))]
            [InlineData(@"{""{00000000-0000-0000-0000-000000000000}"":1}", typeof(Dictionary<Guid, int>))]
            public void ThrowOnInvalidFormat(string json, Type typeToConvert)
            {
                JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, typeToConvert));
                Assert.Contains(typeToConvert.ToString(), ex.Message);
            }

            [Theory] // Extend this test when support for more types is added.
            [InlineData(@"{""1.1"":1}", typeof(Dictionary<int, int>))]
            [InlineData(@"{""{00000000-0000-0000-0000-000000000000}"":1}", typeof(Dictionary<Guid, int>))]
            public async Task ThrowOnInvalidFormatAsync(string json, Type typeToConvert)
            {
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                Stream stream = new MemoryStream(jsonBytes);

                JsonException ex = await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializer.DeserializeAsync(stream, typeToConvert));
                Assert.Contains(typeToConvert.ToString(), ex.Message);
            }

            [Fact]
            public static void TestNotSuportedExceptionIsThrown()
            {
                // Dictionary<int[], int>>
                Assert.Null(JsonSerializer.Deserialize<Dictionary<int[], int>>("null"));
                Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<int[], int>>("\"\""));
                Assert.NotNull(JsonSerializer.Deserialize<Dictionary<int[], int>>("{}"));

                Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Dictionary<int[], int>>(@"{""Foo"":1}"));

                // UnsupportedDictionaryWrapper
                Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<UnsupportedDictionaryWrapper>("\"\""));
                Assert.NotNull(JsonSerializer.Deserialize<UnsupportedDictionaryWrapper>("{}"));
                Assert.Null(JsonSerializer.Deserialize<UnsupportedDictionaryWrapper>("null"));
                Assert.NotNull(JsonSerializer.Deserialize<UnsupportedDictionaryWrapper>(@"{""Dictionary"":null}"));
                Assert.NotNull(JsonSerializer.Deserialize<UnsupportedDictionaryWrapper>(@"{""Dictionary"":{}}"));

                Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<UnsupportedDictionaryWrapper>(@"{""Dictionary"":{""Foo"":1}}"));
            }

            [Fact]
            public void TestPolicyOnlyAppliesToString()
            {
                var opts = new JsonSerializerOptions
                {
                    DictionaryKeyPolicy = new FixedNamingPolicy()
                };

                var stringIntDictionary = new Dictionary<string, int> { { "1", 1 } };
                string json = JsonSerializer.Serialize(stringIntDictionary, opts);
                Assert.Equal($@"{{""{FixedNamingPolicy.FixedName}"":1}}", json);

                var intIntDictionary = new Dictionary<int, int> { { 1, 1 } };
                json = JsonSerializer.Serialize(intIntDictionary, opts);
                Assert.Equal(@"{""1"":1}", json);

                var objectIntDictionary = new Dictionary<object, int> { { "1", 1 } };
                json = JsonSerializer.Serialize(objectIntDictionary, opts);
                Assert.Equal($@"{{""{FixedNamingPolicy.FixedName}"":1}}", json);

                objectIntDictionary = new Dictionary<object, int> { { 1, 1 } };
                json = JsonSerializer.Serialize(objectIntDictionary, opts);
                Assert.Equal(@"{""1"":1}", json);
            }

            [Fact]
            public async Task TestPolicyOnlyAppliesToStringAsync()
            {
                var opts = new JsonSerializerOptions
                {
                    DictionaryKeyPolicy = new FixedNamingPolicy()
                };

                MemoryStream stream = new MemoryStream();

                var stringIntDictionary = new Dictionary<string, int> { { "1", 1 } };
                await JsonSerializer.SerializeAsync(stream, stringIntDictionary, opts);

                string json = Encoding.UTF8.GetString(stream.ToArray());
                Assert.Equal($@"{{""{FixedNamingPolicy.FixedName}"":1}}", json);

                stream.Position = 0;
                stream.SetLength(0);

                var intIntDictionary = new Dictionary<int, int> { { 1, 1 } };
                await JsonSerializer.SerializeAsync(stream, intIntDictionary, opts);

                json = Encoding.UTF8.GetString(stream.ToArray());
                Assert.Equal(@"{""1"":1}", json);

                stream.Position = 0;
                stream.SetLength(0);

                var objectIntDictionary = new Dictionary<object, int> { { "1", 1 } };
                await JsonSerializer.SerializeAsync(stream, objectIntDictionary, opts);

                json = Encoding.UTF8.GetString(stream.ToArray());
                Assert.Equal($@"{{""{FixedNamingPolicy.FixedName}"":1}}", json);

                stream.Position = 0;
                stream.SetLength(0);

                objectIntDictionary = new Dictionary<object, int> { { 1, 1 } };
                await JsonSerializer.SerializeAsync(stream, objectIntDictionary, opts);

                json = Encoding.UTF8.GetString(stream.ToArray());
                Assert.Equal(@"{""1"":1}", json);
            }

            [Fact]
            public void TestEnumKeyWithNotValidIdentifier()
            {
                var myEnumIntDictionary = new Dictionary<MyEnum, int>();
                myEnumIntDictionary.Add((MyEnum)(-1), 1);

                string json = JsonSerializer.Serialize(myEnumIntDictionary);
                Assert.Equal(@"{""-1"":1}", json);

                myEnumIntDictionary = JsonSerializer.Deserialize<Dictionary<MyEnum, int>>(json);
                Assert.Equal(1, myEnumIntDictionary[(MyEnum)(-1)]);

                var myEnumFlagsIntDictionary = new Dictionary<MyEnumFlags, int>();
                myEnumFlagsIntDictionary.Add((MyEnumFlags)(-1), 1);

                json = JsonSerializer.Serialize(myEnumFlagsIntDictionary);
                Assert.Equal(@"{""-1"":1}", json);

                myEnumFlagsIntDictionary = JsonSerializer.Deserialize<Dictionary<MyEnumFlags, int>>(json);
                Assert.Equal(1, myEnumFlagsIntDictionary[(MyEnumFlags)(-1)]);
            }

            [Theory]
            [MemberData(nameof(DictionaryKeysWithSpecialCharacters))]
            public void EnsureNonStringKeysDontGetEscapedOnSerialize(object key, string expectedKeySerialized)
            {
                Dictionary<object, int> root = new Dictionary<object, int>();
                root.Add(key, 1);

                string json = JsonSerializer.Serialize(root);
                Assert.Contains(expectedKeySerialized, json);
            }

            public static IEnumerable<object[]> DictionaryKeysWithSpecialCharacters =>
                new List<object[]>
                {
                    new object[] { float.MaxValue, JsonSerializer.Serialize(float.MaxValue)  },
                    new object[] { double.MaxValue, JsonSerializer.Serialize(double.MaxValue) },
                    new object[] { DateTimeOffset.MaxValue, JsonSerializer.Serialize(DateTimeOffset.MaxValue) }
                };

            [Theory]
            [MemberData(nameof(EscapedMemberData))]
            public void TestEscapedValuesOnDeserialize(string escapedPropertyName, object expectedDictionaryKey, Type dictionaryType)
            {
                string json = $@"{{""{escapedPropertyName}"":1}}";
                IDictionary root = (IDictionary)JsonSerializer.Deserialize(json, dictionaryType);

                bool containsKey = root.Contains(expectedDictionaryKey);
                Assert.True(containsKey);
                Assert.Equal(1, root[expectedDictionaryKey]);
            }

            [Theory]
            [MemberData(nameof(EscapedMemberData))]
            public async Task TestEscapedValuesOnDeserializeAsync(string escapedPropertyName, object expectedDictionaryKey, Type dictionaryType)
            {
                string json = $@"{{""{escapedPropertyName}"":1}}";
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                IDictionary root = (IDictionary)await JsonSerializer.DeserializeAsync(stream, dictionaryType);

                bool containsKey = root.Contains(expectedDictionaryKey);
                Assert.True(containsKey);
                Assert.Equal(1, root[expectedDictionaryKey]);
            }

            public static IEnumerable<object[]> EscapedMemberData =>
                new List<object[]>
                {
                    new object[] { @"\u0031\u0032\u0037",
                        sbyte.MaxValue, typeof(Dictionary<sbyte, int>) },
                    new object[] { @"\u0032\u0035\u0035",
                        byte.MaxValue, typeof(Dictionary<byte, int>) },
                    new object[] { @"\u0033\u0032\u0037\u0036\u0037",
                        short.MaxValue, typeof(Dictionary<short, int>) },
                    new object[] { @"\u0036\u0035\u0035\u0033\u0035",
                        ushort.MaxValue, typeof(Dictionary<ushort, int>) },
                    new object[] { @"\u0032\u0031\u0034\u0037\u0034\u0038\u0033\u0036\u0034\u0037",
                        int.MaxValue, typeof(Dictionary<int, int>) },
                    new object[] { @"\u0034\u0032\u0039\u0034\u0039\u0036\u0037\u0032\u0039\u0035",
                        uint.MaxValue, typeof(Dictionary<uint, int>) },
                    new object[] { @"\u0039\u0032\u0032\u0033\u0033\u0037\u0032\u0030\u0033\u0036\u0038\u0035\u0034\u0037\u0037\u0035\u0038\u0030\u0037",
                        long.MaxValue, typeof(Dictionary<long, int>) },
                    new object[] { @"\u0031\u0038\u0034\u0034\u0036\u0037\u0034\u0034\u0030\u0037\u0033\u0037\u0030\u0039\u0035\u0035\u0031\u0036\u0031\u0035",
                        ulong.MaxValue, typeof(Dictionary<ulong, int>) },
                    // Do not use max values on floating point types since it may have different string representations depending on the tfm.
                    new object[] { @"\u0033\u002e\u0031\u0032\u0035\u0065\u0037",
                        3.125e7f, typeof(Dictionary<float, int>) },
                    new object[] { @"\u0033\u002e\u0031\u0032\u0035\u0065\u0037",
                        3.125e7d, typeof(Dictionary<double, int>) },
                    new object[] { @"\u0033\u002e\u0031\u0032\u0035\u0065\u0037",
                        3.125e7m, typeof(Dictionary<decimal, int>) },
                    new object[] { @"\u0039\u0039\u0039\u0039\u002d\u0031\u0032\u002d\u0033\u0031\u0054\u0032\u0033\u003a\u0035\u0039\u003a\u0035\u0039\u002e\u0039\u0039\u0039\u0039\u0039\u0039\u0039",
                        DateTime.MaxValue, typeof(Dictionary<DateTime, int>) },
                    new object[] { @"\u0039\u0039\u0039\u0039\u002d\u0031\u0032\u002d\u0033\u0031\u0054\u0032\u0033\u003a\u0035\u0039\u003a\u0035\u0039\u002e\u0039\u0039\u0039\u0039\u0039\u0039\u0039\u002b\u0030\u0030\u003a\u0030\u0030",
                        DateTimeOffset.MaxValue, typeof(Dictionary<DateTimeOffset, int>) },
                    new object[] { @"\u0030\u0030\u0030\u0030\u0030\u0030\u0030\u0030\u002d\u0030\u0030\u0030\u0030\u002d\u0030\u0030\u0030\u0030\u002d\u0030\u0030\u0030\u0030\u002d\u0030\u0030\u0030\u0030\u0030\u0030\u0030\u0030\u0030\u0030\u0030\u0030",
                        Guid.Empty, typeof(Dictionary<Guid, int>) },
                    new object[] { @"\u0042\u0061\u0072",
                        MyEnum.Bar, typeof(Dictionary<MyEnum, int>) },
                    new object[] { @"\u0042\u0061\u0072\u002c\u0042\u0061\u007a",
                        MyEnumFlags.Bar | MyEnumFlags.Baz, typeof(Dictionary<MyEnumFlags, int>) },
                    new object[] { @"\u002b", '+', typeof(Dictionary<char, int>) }
                };
        }

        public class MyPublicClass { }

        public struct MyPublicStruct { }

        public enum MyEnum
        {
            Foo,
            Bar
        }

        [Flags]
        public enum MyEnumFlags
        {
            Foo = 1,
            Bar = 2,
            Baz = 4
        }

        private class ClassWithIDictionary
        {
            public IDictionary Dictionary { get; set; }
        }

        private class ClassWithDictionary
        {
            public Dictionary<object, object> Dictionary { get; set; }
        }

        private class ClassWithExtensionData
        {
            [JsonExtensionData]
            public Dictionary<int, object> Overflow { get; set; }
        }

        private class UnsupportedDictionaryWrapper
        {
            public Dictionary<int[], int> Dictionary { get; set; }
        }

        public class FixedNamingPolicy : JsonNamingPolicy
        {
            public const string FixedName = nameof(FixedName);
            public override string ConvertName(string name) => FixedName;
        }

        public class SuffixNamingPolicy : JsonNamingPolicy
        {
            public const string Suffix = "_Suffix";
            public override string ConvertName(string name) => name + Suffix;
        }

        [Fact]
        public static void RoundtripAllDictionaryConverters()
        {
            const string Expected = @"{""1"":1}";

            foreach (Type type in CollectionTestTypes.DeserializableDictionaryTypes<int, int>())
            {
                object dict = JsonSerializer.Deserialize(Expected, type);
                Assert.Equal(Expected, JsonSerializer.Serialize(dict, type));
            }
        }

        [Theory]
        [InlineData(typeof(IDictionary))]
        [InlineData(typeof(Hashtable))]
        public static void IDictionary_Keys_ShouldBe_String_WhenDeserializing(Type type)
        {
            const string Expected = @"{""1998-02-14"":1}";

            IDictionary dict = (IDictionary)JsonSerializer.Deserialize(Expected, type);
            Assert.Equal(1, dict.Count);
            JsonElement element = Assert.IsType<JsonElement>(dict["1998-02-14"]);
            Assert.Equal(1, element.GetInt32());

            Assert.Equal(Expected, JsonSerializer.Serialize(dict, type));
        }

        [Fact]
        public static void GenericDictionary_WithObjectKeys_Throw_WhenDeserializing()
        {
            const string Expected = @"{""1998-02-14"":1}";

            var dict = new Dictionary<object, int> { ["1998-02-14"] = 1 };
            RunTest<IDictionary<object, int>>(dict);
            RunTest<Dictionary<object, int>>(dict);
            RunTest<ImmutableDictionary<object, int>>(ImmutableDictionary.CreateRange(dict));

            void RunTest<T>(T dictionary)
            {
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<T>(Expected));
                Assert.Equal(Expected, JsonSerializer.Serialize(dictionary));
            }
        }

        [Fact]
        public static void KeyWithCustomConverter()
        {
            // TODO: update these tests after https://github.com/dotnet/runtime/issues/50071 is implemented.

            JsonSerializerOptions options = new()
            {
                Converters = { new ConverterForInt32(), new ComplexKeyConverter() }
            };

            // Primitive key
            string json = @"{
    ""PrimitiveKey"":{
        ""1"":""1""
    }
}
";
            ClassWithNonStringDictKeys obj = new()
            {
                PrimitiveKey = new Dictionary<int, string> { [1] = "1" },
            };
            RunTest(obj, json, typeof(int).ToString(), typeof(ConverterForInt32).ToString());

            // Complex key
            json = @"{
    ""ComplexKey"":{
        ""SomeStringRepresentation"":""1""
    }
}
";
            obj = new()
            {
                ComplexKey = new Dictionary<ClassWithIDictionary, string> { [new ClassWithIDictionary()] = "1" },
            };
            RunTest(obj, json, typeof(ClassWithIDictionary).ToString(), typeof(ComplexKeyConverter).ToString());

            void RunTest(ClassWithNonStringDictKeys obj, string payload, string keyTypeAsStr, string converterTypeAsStr)
            {
                NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(obj, options));
                string exAsStr = ex.ToString();
                Assert.Contains(keyTypeAsStr, exAsStr);
                Assert.Contains(converterTypeAsStr, exAsStr);

                ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithNonStringDictKeys>(payload, options));
                exAsStr = ex.ToString();
                Assert.Contains(keyTypeAsStr, exAsStr);
                Assert.Contains(converterTypeAsStr, exAsStr);
            }
        }

        private class ClassWithNonStringDictKeys
        {
            public Dictionary<int, string> PrimitiveKey { get; set; }
            public Dictionary<ClassWithIDictionary, string> ComplexKey { get; set; }
        }

        private class ComplexKeyConverter : JsonConverter<ClassWithIDictionary>
        {
            public override ClassWithIDictionary? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => throw new NotImplementedException();
            public override void Write(Utf8JsonWriter writer, ClassWithIDictionary value, JsonSerializerOptions options)
                => throw new NotImplementedException();
        }
    }
}
