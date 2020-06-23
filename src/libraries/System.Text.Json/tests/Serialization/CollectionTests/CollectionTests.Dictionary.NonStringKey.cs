// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
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
            public void TestDictinaryKey()
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
            protected override string _expectedJson => $@"{{""{DateTime.MaxValue.ToString("O")}"":1}}";
            protected override DateTime Key => DateTime.MaxValue;
            protected override int Value => 1;
        }

        public class DictionaryDateTimeOffsetKey : DictionaryKeyTestsBase<DateTimeOffset, DateTimeOffset>
        {
            //TODO: The plus sign is escaped for the key but not for the value. Is this correct?
            protected override string _expectedJson => $@"{{""9999-12-31T23:59:59.9999999\u002B00:00"":""{DateTimeOffset.MaxValue.ToString("O")}""}}";
            protected override DateTimeOffset Key => DateTimeOffset.MaxValue;
            protected override DateTimeOffset Value => DateTimeOffset.MaxValue;
        }

        public class DictionaryDecimalKey : DictionaryKeyTestsBase<decimal, int>
        {
            protected override decimal Key => decimal.MaxValue;
            protected override int Value => 1;
        }

        //public class DictionaryDoubleKey : DictionaryKeyTestsBase<double, double>
        //{
        //    //TODO: The plus sign is escaped for the key but not for the value. Is this correct?
        //    protected override string _expectedJson => $@"{{""1.7976931348623157E\u002B308"":{double.MaxValue}}}";
        //    protected override double Key => double.MaxValue;
        //    protected override double Value => double.MaxValue;
        //}

        public class DictionaryDoubleKey : DictionaryKeyTestsBase<double, int>
        {
            protected override double Key => 1;
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

        //public class DictionarySingleKey : DictionaryKeyTestsBase<float, float>
        //{
        //    //TODO: The plus sign is escaped for the key but not for the value. Is this correct?
        //    protected override float Key => float.MaxValue;
        //    protected override float Value => float.MaxValue;
        //}

        public class DictionarySingleKey : DictionaryKeyTestsBase<float, int>
        {
            protected override float Key => 1;
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

            [Theory]
            [InlineData(@"{""\u0041"":1}", typeof(Dictionary<string, int>), "A")]
            [InlineData(@"{""\u0066\u006f\u006f"":1}", typeof(Dictionary<MyEnum, int>), MyEnum.Foo)]
            [InlineData(@"{""\u0066\u006f\u006f\u002c\u0020\u0062\u0061\u0072"":1}", typeof(Dictionary<MyEnumFlags, int>), MyEnumFlags.Foo | MyEnumFlags.Bar)]
            public static void TestUnescapedKeys(string json, Type typeToConvert, object keyAsType)
            {
                object result = JsonSerializer.Deserialize(json, typeToConvert);

                IDictionary dictionary = (IDictionary)result;
                Assert.True(dictionary.Contains(keyAsType));
            }

            [Fact]
            public static void TestMoreUnescapedKeys()
            {
                // Test types that cannot be passed as InlineData in above method.

                Dictionary<Guid, int> result = JsonSerializer.Deserialize<Dictionary<Guid, int>>(@"{""\u0036bb67e4e-9780-4895-851b-75f72ac34c5a"":1}");
                Guid guid = new Guid("6bb67e4e-9780-4895-851b-75f72ac34c5a");
                Assert.Equal(1, result[guid]);

                Dictionary<DateTime, int> result2 = JsonSerializer.Deserialize<Dictionary<DateTime, int>>(@"{""\u0032\u0030\u0030\u0039\u002d\u0030\u0036\u002d\u0031\u0035\u0054\u0031\u0033\u003a\u0034\u0035\u003a\u0033\u0030\u002e\u0030\u0030\u0030\u0030\u0030\u0030\u0030"":1}");
                // 2009-06-15T13:45:30.0000000
                DateTime dateTime = new DateTime(2009, 6, 15, 13, 45, 30, DateTimeKind.Unspecified);
                Assert.Equal(1, result2[dateTime]);

                Dictionary<DateTime, int> result3 = JsonSerializer.Deserialize<Dictionary<DateTime, int>>(@"{""\u0032\u0030\u0030\u0039\u002d\u0030\u0036\u002d\u0031\u0035\u0054\u0031\u0033\u003a\u0034\u0035\u003a\u0033\u0030\u002e\u0030\u0030\u0030\u0030\u0030\u0030\u0030\u005a"":1}");
                // 2009-06-15T13:45:30.0000000Z
                dateTime = new DateTime(2009, 6, 15, 13, 45, 30, DateTimeKind.Utc);
                Assert.Equal(1, result3[dateTime]);

                Dictionary<DateTimeOffset, int> result4 = JsonSerializer.Deserialize<Dictionary<DateTimeOffset, int>>(@"{""\u0032\u0030\u0030\u0039\u002d\u0030\u0036\u002d\u0031\u0035\u0054\u0031\u0033\u003a\u0034\u0035\u003a\u0033\u0030\u002e\u0030\u0030\u0030\u0030\u0030\u0030\u0030\u002d\u0030\u0037\u003a\u0030\u0030"":1}");
                // 2009-06-15T13:45:30.0000000-07:00
                DateTimeOffset dateTimeOffset = new DateTimeOffset(2009, 6, 15, 13, 45, 30, new TimeSpan(-7, 0, 0));
                Assert.Equal(1, result4[dateTimeOffset]);
            }

            [Theory]
            [InlineData(@"{""\u0041"":1}", typeof(Dictionary<string, int>), "A")]
            [InlineData(@"{""\u0066\u006f\u006f"":1}", typeof(Dictionary<MyEnum, int>), MyEnum.Foo)]
            [InlineData(@"{""\u0066\u006f\u006f\u002c\u0020\u0062\u0061\u0072"":1}", typeof(Dictionary<MyEnumFlags, int>), MyEnumFlags.Foo | MyEnumFlags.Bar)]
            public static async Task TestUnescapedKeysAsync(string json, Type typeToConvert, object keyAsType)
            {
                byte[] utf8Json = Encoding.UTF8.GetBytes(json);
                MemoryStream stream = new MemoryStream(utf8Json);

                object result = await JsonSerializer.DeserializeAsync(stream, typeToConvert);

                IDictionary dictionary = (IDictionary)result;
                Assert.True(dictionary.Contains(keyAsType));
            }

            [Fact]
            public static async Task TestMoreUnescapedKeyAsync()
            {
                // Test types that cannot be passed as InlineData in above method.

                byte[] utf8Json = Encoding.UTF8.GetBytes(@"{""\u0036bb67e4e-9780-4895-851b-75f72ac34c5a"":1}");
                MemoryStream stream = new MemoryStream(utf8Json);
                Dictionary<Guid, int> result = await JsonSerializer.DeserializeAsync<Dictionary<Guid, int>>(stream);
                Guid myGuid = new Guid("6bb67e4e-9780-4895-851b-75f72ac34c5a");
                Assert.Equal(1, result[myGuid]);

                utf8Json = Encoding.UTF8.GetBytes(@"{""\u0032\u0030\u0030\u0039\u002d\u0030\u0036\u002d\u0031\u0035\u0054\u0031\u0033\u003a\u0034\u0035\u003a\u0033\u0030\u002e\u0030\u0030\u0030\u0030\u0030\u0030\u0030"":1}");
                stream = new MemoryStream(utf8Json);
                Dictionary<DateTime, int> result2 = await JsonSerializer.DeserializeAsync<Dictionary<DateTime, int>>(stream);
                // 2009-06-15T13:45:30.0000000
                DateTime myDate = new DateTime(2009, 6, 15, 13, 45, 30, DateTimeKind.Unspecified);
                Assert.Equal(1, result2[myDate]);

                utf8Json = Encoding.UTF8.GetBytes(@"{""\u0032\u0030\u0030\u0039\u002d\u0030\u0036\u002d\u0031\u0035\u0054\u0031\u0033\u003a\u0034\u0035\u003a\u0033\u0030\u002e\u0030\u0030\u0030\u0030\u0030\u0030\u0030\u005a"":1}");
                stream = new MemoryStream(utf8Json);
                Dictionary<DateTime, int> result3 = await JsonSerializer.DeserializeAsync<Dictionary<DateTime, int>>(stream);
                // 2009-06-15T13:45:30.0000000Z
                myDate = new DateTime(2009, 6, 15, 13, 45, 30, DateTimeKind.Utc);
                Assert.Equal(1, result3[myDate]);

                utf8Json = Encoding.UTF8.GetBytes(@"{""\u0032\u0030\u0030\u0039\u002d\u0030\u0036\u002d\u0031\u0035\u0054\u0031\u0033\u003a\u0034\u0035\u003a\u0033\u0030\u002e\u0030\u0030\u0030\u0030\u0030\u0030\u0030\u002d\u0030\u0037\u003a\u0030\u0030"":1}");
                stream = new MemoryStream(utf8Json);
                Dictionary<DateTimeOffset, int> result4 = await JsonSerializer.DeserializeAsync<Dictionary<DateTimeOffset, int>>(stream);
                // 2009-06-15T13:45:30.0000000-07:00
                DateTimeOffset dateTimeOffset = new DateTimeOffset(2009, 6, 15, 13, 45, 30, new TimeSpan(-7, 0, 0));
                Assert.Equal(1, result4[dateTimeOffset]);
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
    }
}
