// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class DictionaryTests
    {
        [Theory]
        [MemberData(nameof(GetTestDictionaries))]
        public void TestDictionaryKey<TKey, TValue>(Dictionary<TKey, TValue> dictionary, string expectedJson)
        {
            string json = JsonSerializer.Serialize(dictionary);
            Assert.Equal(expectedJson, json);

            Dictionary<TKey, TValue> deserializedDictionary = JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(json);
            Assert.Equal(dictionary, deserializedDictionary);
        }

        [Theory]
        [MemberData(nameof(GetTestDictionaries))]
        public async Task TestDictionaryKeyAsync<TKey, TValue>(Dictionary<TKey, TValue> dictionary, string expectedJson)
        {
            MemoryStream serializeStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(serializeStream, dictionary);
            string json = Encoding.UTF8.GetString(serializeStream.ToArray());
            Assert.Equal(expectedJson, json);

            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            Stream deserializeStream = new MemoryStream(jsonBytes);
            Dictionary<TKey, TValue> deserializedDictionary = await JsonSerializer.DeserializeAsync<Dictionary<TKey, TValue>>(deserializeStream);
            Assert.Equal(dictionary, deserializedDictionary);
        }

        public static IEnumerable<object[]> GetTestDictionaries()
        {
            yield return WrapArgs(true, 1);
            yield return WrapArgs(byte.MaxValue, 1);
            yield return WrapArgs(char.MaxValue, char.MaxValue, expectedJson: @"{""\uFFFF"":""\uFFFF""}");
            yield return WrapArgs(DateTime.MaxValue, 1, expectedJson: $@"{{""{DateTime.MaxValue:O}"":1}}");
            yield return WrapArgs(DateTimeOffset.MaxValue, 1, expectedJson: $@"{{""{DateTimeOffset.MaxValue:O}"":1}}");
            yield return WrapArgs(decimal.MaxValue, 1, expectedJson: $@"{{""{JsonSerializer.Serialize(decimal.MaxValue)}"":1}}");
            yield return WrapArgs(double.MaxValue, 1, expectedJson: $@"{{""{JsonSerializer.Serialize(double.MaxValue)}"":1}}");
            yield return WrapArgs(MyEnum.Foo, 1);
            yield return WrapArgs(MyEnumFlags.Foo | MyEnumFlags.Bar, 1);
            yield return WrapArgs(Guid.NewGuid(), 1);
            yield return WrapArgs(short.MaxValue, 1);
            yield return WrapArgs(int.MaxValue, 1);
            yield return WrapArgs(long.MaxValue, 1);
            yield return WrapArgs(sbyte.MaxValue, 1);
            yield return WrapArgs(float.MaxValue, 1, $@"{{""{JsonSerializer.Serialize(float.MaxValue)}"":1}}");
            yield return WrapArgs("KeyString", 1);
            yield return WrapArgs(ushort.MaxValue, 1);
            yield return WrapArgs(uint.MaxValue, 1);
            yield return WrapArgs(ulong.MaxValue, 1);

            static object[] WrapArgs<TKey, TValue>(TKey key, TValue value, string? expectedJson = null)
            {
                Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue> { [key] = value };
                expectedJson ??= $"{{\"{key}\":{value}}}";
                return new object[] { dictionary, expectedJson };
            }
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedDictionaries))]
        public void ThrowUnsupported_Serialize<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
            => Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(dictionary));


        [Theory]
        [MemberData(nameof(GetUnsupportedDictionaries))]
        public Task ThrowUnsupported_SerializeAsync<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
            => Assert.ThrowsAsync<NotSupportedException>(() => JsonSerializer.SerializeAsync(new MemoryStream(), dictionary));

        [Theory]
        [MemberData(nameof(GetUnsupportedDictionaries))]
        public void DoesNotThrowIfEmpty_Serialize<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            _ = dictionary; // argument only needed to infer generic parameters
            JsonSerializer.Serialize(new Dictionary<TKey, TValue>());
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedDictionaries))]
        public Task DoesNotThrowIfEmpty_SerializeAsync<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            _ = dictionary; // argument only needed to infer generic parameters
            return JsonSerializer.SerializeAsync(new MemoryStream(), new Dictionary<TKey, TValue>());
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedDictionaries))]
        public void DoesNotThrowIfEmpty_Deserialize<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            _ = dictionary; // argument only needed to infer generic parameters
            JsonSerializer.Deserialize<Dictionary<TKey, TValue>>("{}");
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedDictionaries))]
        public Task DoesNotThrowIfEmpty_DeserializeAsync<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            _ = dictionary; // argument only needed to infer generic parameters
            return JsonSerializer.DeserializeAsync<Dictionary<TKey, TValue>>(new MemoryStream(Encoding.UTF8.GetBytes("{}"))).AsTask();
        }

        public static IEnumerable<object[]> GetUnsupportedDictionaries()
        {
            yield return WrapArgs(new MyPublicClass(), 0);
            yield return WrapArgs(new MyPublicStruct(), 0);
            yield return WrapArgs(new Uri("http://foo"), 0);
            yield return WrapArgs(new object(), 0);
            yield return WrapArgs((object)new Uri("http://foo"), 0);

            static object[] WrapArgs<TKey, TValue>(TKey key, TValue value) => new object[] { new Dictionary<TKey, TValue>() { [key] = value } };
        }

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

        [Theory]
        [InlineData("1.1", typeof(int))]
        [InlineData("42", typeof(bool))]
        [InlineData("false", typeof(double))]
        [InlineData("{00000000-0000-0000-0000-000000000000}", typeof(Guid))]
        public void ThrowOnInvalidFormat(string keyValue, Type keyType)
        {
            Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, typeof(int));
            string json = $@"{{ ""{keyValue}"" : 1 }}";
            string expectedJsonPath = keyValue.Contains(".") ? $"$['{keyValue}']" : $"$.{keyValue}";

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, dictionaryType));
            Assert.Contains(keyType.ToString(), ex.Message);
            Assert.Contains(expectedJsonPath, ex.Message);
        }

        [Theory]
        [InlineData("1.1", typeof(int))]
        [InlineData("42", typeof(bool))]
        [InlineData("false", typeof(double))]
        [InlineData("{00000000-0000-0000-0000-000000000000}", typeof(Guid))]
        public async Task ThrowOnInvalidFormatAsync(string keyValue, Type keyType)
        {
            Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, typeof(int));
            string json = $@"{{ ""{keyValue}"" : 1 }}";
            string expectedJsonPath = keyValue.Contains(".") ? $"$['{keyValue}']" : $"$.{keyValue}";

            using var stream = new Utf8MemoryStream(json);
            JsonException ex = await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializer.DeserializeAsync(stream, dictionaryType));
            Assert.Contains(keyType.ToString(), ex.Message);
            Assert.Contains(expectedJsonPath, ex.Message);
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
        public static void KeyWithCustomPrimitiveConverter_FallbackToDefaultConverter()
        {
            // Validates .NET 5 primitive custom key converter behavior.

            JsonSerializerOptions options = new()
            {
                Converters = { new ConverterForInt32() }
            };

            var dictionary = new Dictionary<int, string> { [1] = "1" };

            string expectedJson = @"{""1"":""1""}";
            string actualJson = JsonSerializer.Serialize(dictionary, options);
            Assert.Equal(expectedJson, actualJson);

            dictionary = JsonSerializer.Deserialize<Dictionary<int, string>>(expectedJson);
            Assert.True(dictionary.ContainsKey(1));
        }

        [Fact]
        public static void KeyWithCustomPrimitiveConverter_JsonTypeInfo_ThrowsNotSupportedException()
        {
            JsonSerializer.Serialize(42); // Ensure default converters are rooted in current process

            CustomInt32ConverterSerializerContext ctx = new();

            var dictionary = new Dictionary<int, string> { [1] = "1" };
            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(dictionary, ctx.DictionaryInt32String));
            ValidateException(ex);

            string json = @"{""1"":""1""}";
            ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Dictionary<int, string>>(json, ctx.DictionaryInt32String));
            ValidateException(ex);

            static void ValidateException(NotSupportedException ex)
            {
                Assert.Contains(nameof(Int32), ex.Message);
                Assert.Contains(nameof(ConverterForInt32), ex.Message);
            }
        }

        public class CustomInt32ConverterSerializerContext : JsonSerializerContext
        {
            public CustomInt32ConverterSerializerContext() : base(null) { }
            public override JsonTypeInfo? GetTypeInfo(Type _) => throw new NotImplementedException();

            public JsonTypeInfo<Dictionary<int, string>> DictionaryInt32String => _dictionaryInt32String ??= CreateDictionaryConverter();
            private JsonTypeInfo<Dictionary<int, string>>? _dictionaryInt32String;

            protected override JsonSerializerOptions? GeneratedSerializerOptions => null;

            private JsonTypeInfo<Dictionary<int, string>> CreateDictionaryConverter()
            {
                JsonTypeInfo<int> keyInfo = JsonMetadataServices.CreateValueInfo<int>(Options, new ConverterForInt32());
                JsonTypeInfo<string> valueInfo = JsonMetadataServices.CreateValueInfo<string>(Options, JsonMetadataServices.StringConverter);
                JsonCollectionInfoValues<Dictionary<int, string>> info = new()
                {
                    ObjectCreator = () => new Dictionary<int, string>(),
                    KeyInfo = keyInfo,
                    ElementInfo = valueInfo,
                };

                return JsonMetadataServices.CreateDictionaryInfo<Dictionary<int, string>, int, string>(Options, info);
            }
        }

        [Fact]
        public static void KeyWithCustomClassConverter_ThrowsNotSupportedException()
        {
            // TODO: update after https://github.com/dotnet/runtime/issues/46520 is implemented.

            JsonSerializerOptions options = new()
            {
                Converters = { new ComplexKeyConverter() }
            };

            var dictionary = new Dictionary<ClassWithIDictionary, string> { [new ClassWithIDictionary()] = "1" };

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(dictionary, options));
            ValidateException(ex);

            string json = @"{""SomeStringRepresentation"":""1""}";
            ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Dictionary<ClassWithIDictionary, string>>(json, options));
            ValidateException(ex);

            static void ValidateException(NotSupportedException ex)
            {
                Assert.Contains(nameof(ClassWithIDictionary), ex.Message);
                Assert.Contains(nameof(ComplexKeyConverter), ex.Message);
            }
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
