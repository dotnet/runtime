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
#if !BUILDING_SOURCE_GENERATOR_TESTS
    public partial class CollectionTests
    {
        [Theory]
        [MemberData(nameof(GetTestDictionaries))]
        public async Task TestDictionaryKey<TKey, TValue>(Dictionary<TKey, TValue> dictionary, string expectedJson)
        {
            string json = await Serializer.SerializeWrapper(dictionary);
            Assert.Equal(expectedJson, json);

            Dictionary<TKey, TValue> deserializedDictionary = await Serializer.DeserializeWrapper<Dictionary<TKey, TValue>>(json);
            Assert.Equal(dictionary, deserializedDictionary);
        }

        [Theory]
        [MemberData(nameof(GetTestDictionaries))]
        public async Task TestDictionaryKey_CustomConverter_ComposingWithDefaultConverter<TKey, TValue>(Dictionary<TKey, TValue> dictionary, string expectedJson)
        {
            var options = new JsonSerializerOptions { Converters = { new CustomPropertyNameConverter<TKey>() } };
            string json = await Serializer.SerializeWrapper(dictionary, options);
            Assert.Equal(expectedJson, json);

            Dictionary<TKey, TValue> deserializedDictionary = await Serializer.DeserializeWrapper<Dictionary<TKey, TValue>>(json, options);
            Assert.Equal(dictionary, deserializedDictionary);
        }

        public static IEnumerable<object[]> GetTestDictionaries()
        {
            yield return WrapArgs(true, 1);
            yield return WrapArgs(byte.MaxValue, 1);
            yield return WrapArgs(char.MaxValue, char.MaxValue, expectedJson: @"{""\uFFFF"":""\uFFFF""}");
            yield return WrapArgs(DateTime.MaxValue, 1, expectedJson: $@"{{""{DateTime.MaxValue:O}"":1}}");
            yield return WrapArgs(DateTimeOffset.MaxValue, 1, expectedJson: $@"{{""{DateTimeOffset.MaxValue:O}"":1}}");
            yield return WrapArgs(TimeSpan.MaxValue, 1, expectedJson: $@"{{""{TimeSpan.MaxValue}"":1}}");
#if NET6_0_OR_GREATER
            yield return WrapArgs(DateOnly.MaxValue, 1, expectedJson: $@"{{""{DateOnly.MaxValue:O}"":1}}");
            yield return WrapArgs(TimeOnly.MaxValue, 1, expectedJson: $@"{{""{TimeOnly.MaxValue:O}"":1}}");
#endif
            yield return WrapArgs(decimal.MaxValue, 1, expectedJson: $@"{{""{JsonSerializer.Serialize(decimal.MaxValue)}"":1}}");
            yield return WrapArgs(double.MaxValue, 1, expectedJson: $@"{{""{JsonSerializer.Serialize(double.MaxValue)}"":1}}");
            yield return WrapArgs(MyEnum.Foo, 1);
            yield return WrapArgs(MyEnumFlags.Foo | MyEnumFlags.Bar, 1);
            yield return WrapArgs(Guid.NewGuid(), 1);
            yield return WrapArgs(new Version(8, 0, 0), 1);
            yield return WrapArgs(new Uri("http://dot.net/"), 1);
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

        public class CustomPropertyNameConverter<T> : JsonConverter<T>
        {
            private readonly JsonConverter<T> _defaultConverter;

            public CustomPropertyNameConverter()
            {
                _defaultConverter = (JsonConverter<T>)JsonSerializerOptions.Default.GetConverter(typeof(T));
            }

            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => _defaultConverter.Read(ref reader, typeToConvert, options);

            public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => _defaultConverter.ReadAsPropertyName(ref reader, typeToConvert, options);

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
                => _defaultConverter.Write(writer, value, options);

            public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
                => _defaultConverter.WriteAsPropertyName(writer, value, options);
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedDictionaries))]
        public Task ThrowUnsupported_Serialize<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
            => Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(dictionary));

        [Theory]
        [MemberData(nameof(GetUnsupportedDictionaries))]
        public Task DoesNotThrowIfEmpty_Serialize<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            _ = dictionary; // argument only needed to infer generic parameters
            return Serializer.SerializeWrapper(new Dictionary<TKey, TValue>());
        }

        [Theory]
        [MemberData(nameof(GetUnsupportedDictionaries))]
        public Task DoesNotThrowIfEmpty_Deserialize<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            _ = dictionary; // argument only needed to infer generic parameters
            return Serializer.DeserializeWrapper<Dictionary<TKey, TValue>>("{}");
        }

        public static IEnumerable<object[]> GetUnsupportedDictionaries()
        {
            yield return WrapArgs(new MyPublicClass(), 0);
            yield return WrapArgs(new MyPublicStruct(), 0);
            yield return WrapArgs(new object(), 0);
            yield return WrapArgs((object)new MyPublicStruct(), 0);

            static object[] WrapArgs<TKey, TValue>(TKey key, TValue value) => new object[] { new Dictionary<TKey, TValue>() { [key] = value } };
        }

        [Fact]
        public async Task TestGenericDictionaryKeyObject()
        {
            var dictionary = new Dictionary<object, object>();
            // Add multiple supported types.
            dictionary.Add(1, 1);
            dictionary.Add(new Guid("08314FA2-B1FE-4792-BCD1-6E62338AC7F3"), 2);
            dictionary.Add("KeyString", 3);
            dictionary.Add(MyEnum.Foo, 4);
            dictionary.Add(MyEnumFlags.Foo | MyEnumFlags.Bar, 5);

            const string expected = @"{""1"":1,""08314fa2-b1fe-4792-bcd1-6e62338ac7f3"":2,""KeyString"":3,""Foo"":4,""Foo, Bar"":5}";

            string json = await Serializer.SerializeWrapper(dictionary);
            Assert.Equal(expected, json);
            // object type is not supported on deserialization.
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<Dictionary<object, object>>(json));

            var @object = new ClassWithDictionary { Dictionary = dictionary };
            json = await Serializer.SerializeWrapper(@object);
            Assert.Equal($@"{{""Dictionary"":{expected}}}", json);
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<ClassWithDictionary>(json));
        }

        [Fact]
        public async Task TestNonGenericDictionaryKeyObject()
        {
            IDictionary dictionary = new OrderedDictionary();
            // Add multiple supported types.
            dictionary.Add(1, 1);
            dictionary.Add(new Guid("08314FA2-B1FE-4792-BCD1-6E62338AC7F3"), 2);
            dictionary.Add("KeyString", 3);
            dictionary.Add(MyEnum.Foo, 4);
            dictionary.Add(MyEnumFlags.Foo | MyEnumFlags.Bar, 5);

            const string expected = @"{""1"":1,""08314fa2-b1fe-4792-bcd1-6e62338ac7f3"":2,""KeyString"":3,""Foo"":4,""Foo, Bar"":5}";
            string json = await Serializer.SerializeWrapper(dictionary);
            Assert.Equal(expected, json);

            dictionary = await Serializer.DeserializeWrapper<IDictionary>(json);
            Assert.IsType<Dictionary<string, object>>(dictionary);

            dictionary = await Serializer.DeserializeWrapper<OrderedDictionary>(json);
            foreach (object key in dictionary.Keys)
            {
                Assert.IsType<string>(key);
            }

            var @object = new ClassWithIDictionary { Dictionary = dictionary };
            json = await Serializer.SerializeWrapper(@object);
            Assert.Equal($@"{{""Dictionary"":{expected}}}", json);

            @object = await Serializer.DeserializeWrapper<ClassWithIDictionary>(json);
            Assert.IsType<Dictionary<string, object>>(@object.Dictionary);
        }

        [Theory]
        [InlineData("1.1", typeof(int))]
        [InlineData("42", typeof(bool))]
        [InlineData("false", typeof(double))]
        [InlineData("{00000000-0000-0000-0000-000000000000}", typeof(Guid))]
        public async Task ThrowOnInvalidFormat(string keyValue, Type keyType)
        {
            Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, typeof(int));
            string json = $@"{{ ""{keyValue}"" : 1 }}";
            string expectedJsonPath = keyValue.Contains(".") ? $"$['{keyValue}']" : $"$.{keyValue}";

            JsonException ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper(json, dictionaryType));
            Assert.Contains(keyType.ToString(), ex.Message);
            Assert.Contains(expectedJsonPath, ex.Message);
        }

        [Fact]
        public async Task TestNotSuportedExceptionIsThrown()
        {
            // Dictionary<int[], int>>
            Assert.Null(await Serializer.DeserializeWrapper<Dictionary<int[], int>>("null"));
            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<Dictionary<int[], int>>("\"\""));
            Assert.NotNull(await Serializer.DeserializeWrapper<Dictionary<int[], int>>("{}"));

            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<Dictionary<int[], int>>(@"{""Foo"":1}"));

            // UnsupportedDictionaryWrapper
            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<UnsupportedDictionaryWrapper>("\"\""));
            Assert.NotNull(await Serializer.DeserializeWrapper<UnsupportedDictionaryWrapper>("{}"));
            Assert.Null(await Serializer.DeserializeWrapper<UnsupportedDictionaryWrapper>("null"));
            Assert.NotNull(await Serializer.DeserializeWrapper<UnsupportedDictionaryWrapper>(@"{""Dictionary"":null}"));
            Assert.NotNull(await Serializer.DeserializeWrapper<UnsupportedDictionaryWrapper>(@"{""Dictionary"":{}}"));

            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<UnsupportedDictionaryWrapper>(@"{""Dictionary"":{""Foo"":1}}"));
        }

        [Fact]
        public async Task TestPolicyOnlyAppliesToString()
        {
            var opts = new JsonSerializerOptions
            {
                DictionaryKeyPolicy = new FixedNamingPolicy()
            };

            var stringIntDictionary = new Dictionary<string, int> { { "1", 1 } };
            string json = await Serializer.SerializeWrapper(stringIntDictionary, opts);
            Assert.Equal($@"{{""{FixedNamingPolicy.FixedName}"":1}}", json);

            var intIntDictionary = new Dictionary<int, int> { { 1, 1 } };
            json = await Serializer.SerializeWrapper(intIntDictionary, opts);
            Assert.Equal(@"{""1"":1}", json);

            var objectIntDictionary = new Dictionary<object, int> { { "1", 1 } };
            json = await Serializer.SerializeWrapper(objectIntDictionary, opts);
            Assert.Equal($@"{{""{FixedNamingPolicy.FixedName}"":1}}", json);

            objectIntDictionary = new Dictionary<object, int> { { 1, 1 } };
            json = await Serializer.SerializeWrapper(objectIntDictionary, opts);
            Assert.Equal(@"{""1"":1}", json);
        }

        [Fact]
        public async Task TestEnumKeyWithNotValidIdentifier()
        {
            var myEnumIntDictionary = new Dictionary<MyEnum, int>();
            myEnumIntDictionary.Add((MyEnum)(-1), 1);

            string json = await Serializer.SerializeWrapper(myEnumIntDictionary);
            Assert.Equal(@"{""-1"":1}", json);

            myEnumIntDictionary = await Serializer.DeserializeWrapper<Dictionary<MyEnum, int>>(json);
            Assert.Equal(1, myEnumIntDictionary[(MyEnum)(-1)]);

            var myEnumFlagsIntDictionary = new Dictionary<MyEnumFlags, int>();
            myEnumFlagsIntDictionary.Add((MyEnumFlags)(-1), 1);

            json = await Serializer.SerializeWrapper(myEnumFlagsIntDictionary);
            Assert.Equal(@"{""-1"":1}", json);

            myEnumFlagsIntDictionary = await Serializer.DeserializeWrapper<Dictionary<MyEnumFlags, int>>(json);
            Assert.Equal(1, myEnumFlagsIntDictionary[(MyEnumFlags)(-1)]);
        }

        [Theory]
        [MemberData(nameof(DictionaryKeysWithSpecialCharacters))]
        public async Task EnsureNonStringKeysDontGetEscapedOnSerialize(object key, string expectedKeySerialized)
        {
            Dictionary<object, int> root = new Dictionary<object, int>();
            root.Add(key, 1);

            string json = await Serializer.SerializeWrapper(root);
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
        public async Task TestEscapedValuesOnDeserialize(string escapedPropertyName, object expectedDictionaryKey, Type dictionaryType)
        {
            string json = $@"{{""{escapedPropertyName}"":1}}";
            IDictionary root = (IDictionary) await Serializer.DeserializeWrapper(json, dictionaryType);

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
        public async Task RoundtripAllDictionaryConverters()
        {
            const string Expected = @"{""1"":1}";

            foreach (Type type in CollectionTestTypes.DeserializableDictionaryTypes<int, int>())
            {
                object dict = await Serializer.DeserializeWrapper(Expected, type);
                Assert.Equal(Expected, await Serializer.SerializeWrapper(dict, type));
            }
        }

        [Theory]
        [InlineData(typeof(IDictionary))]
        [InlineData(typeof(Hashtable))]
        public async Task IDictionary_Keys_ShouldBe_String_WhenDeserializing(Type type)
        {
            const string Expected = @"{""1998-02-14"":1}";

            IDictionary dict = (IDictionary) await Serializer.DeserializeWrapper(Expected, type);
            Assert.Equal(1, dict.Count);
            JsonElement element = Assert.IsType<JsonElement>(dict["1998-02-14"]);
            Assert.Equal(1, element.GetInt32());

            Assert.Equal(Expected, await Serializer.SerializeWrapper(dict, type));
        }

        [Fact]
        public async Task GenericDictionary_WithObjectKeys_Throw_WhenDeserializing()
        {
            const string Expected = @"{""1998-02-14"":1}";

            var dict = new Dictionary<object, int> { ["1998-02-14"] = 1 };
            await RunTest<IDictionary<object, int>>(dict);
            await RunTest<Dictionary<object, int>>(dict);
            await RunTest<ImmutableDictionary<object, int>>(ImmutableDictionary.CreateRange(dict));

            async Task RunTest<T>(T dictionary)
            {
                await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<T>(Expected));
                Assert.Equal(Expected, await Serializer.SerializeWrapper(dictionary));
            }
        }

        [Fact]
        public async Task KeyWithCustomPrimitiveConverter_FallbackToDefaultConverter()
        {
            // Validates .NET 5 primitive custom key converter behavior.

            JsonSerializerOptions options = new()
            {
                Converters = { new ConverterForInt32() }
            };

            var dictionary = new Dictionary<int, string> { [1] = "1" };

            string expectedJson = @"{""1"":""1""}";
            string actualJson = await Serializer.SerializeWrapper(dictionary, options);
            Assert.Equal(expectedJson, actualJson);

            dictionary = await Serializer.DeserializeWrapper<Dictionary<int, string>>(expectedJson);
            Assert.True(dictionary.ContainsKey(1));
        }

        [Fact]
        public async Task KeyWithCustomPrimitiveConverter_JsonTypeInfo_ThrowsNotSupportedException()
        {
            JsonSerializer.Serialize(42); // Ensure default converters are rooted in current process

            CustomInt32ConverterSerializerContext ctx = new();

            var dictionary = new Dictionary<int, string> { [1] = "1" };
            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(dictionary, ctx.DictionaryInt32String));
            ValidateException(ex);

            string json = @"{""1"":""1""}";
            ex = await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<Dictionary<int, string>>(json, ctx.DictionaryInt32String));
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
        public async Task KeyWithCustomClassConverter_ThrowsNotSupportedException()
        {
            // TODO: update after https://github.com/dotnet/runtime/issues/46520 is implemented.

            JsonSerializerOptions options = new()
            {
                Converters = { new ComplexKeyConverter() }
            };

            var dictionary = new Dictionary<ClassWithIDictionary, string> { [new ClassWithIDictionary()] = "1" };

            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(dictionary, options));
            ValidateException(ex);

            string json = @"{""SomeStringRepresentation"":""1""}";
            ex = await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<Dictionary<ClassWithIDictionary, string>>(json, options));
            ValidateException(ex);

            static void ValidateException(NotSupportedException ex)
            {
                Assert.Contains(nameof(ClassWithIDictionary), ex.Message);
                Assert.Contains(nameof(ComplexKeyConverter), ex.Message);
            }
        }

        [Fact]
        public void NullKeyReturnedFromDictionary_ThrowsArgumentNullException()
        {
            // Via JsonSerializer.Serialize
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(new NullKeyDictionary<object>()));
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(new NullKeyDictionary<string>()));
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(new NullKeyDictionary<Uri>()));
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(new NullKeyDictionary<Version>()));

            // Via converter directly
            var writer = new Utf8JsonWriter(Stream.Null);
            Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.ObjectConverter.WriteAsPropertyName(writer, null, JsonSerializerOptions.Default));
            Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.StringConverter.WriteAsPropertyName(writer, null, JsonSerializerOptions.Default));
            Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.UriConverter.WriteAsPropertyName(writer, null, JsonSerializerOptions.Default));
            Assert.Throws<ArgumentNullException>(() => JsonMetadataServices.VersionConverter.WriteAsPropertyName(writer, null, JsonSerializerOptions.Default));
        }

        private sealed class NullKeyDictionary<TKey> : IReadOnlyDictionary<TKey, int> where TKey : class?
        {
            public int Count => 1;

            public IEnumerator<KeyValuePair<TKey, int>> GetEnumerator()
            {
                yield return new KeyValuePair<TKey, int>(null!, 0);
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public IEnumerable<TKey> Keys => throw new NotImplementedException();
            public IEnumerable<int> Values => throw new NotImplementedException();
            public int this[TKey key] => throw new NotImplementedException();
            public bool ContainsKey(TKey key) => throw new NotImplementedException();
            public bool TryGetValue(TKey key, out int value) => throw new NotImplementedException();
        }

        private class ComplexKeyConverter : JsonConverter<ClassWithIDictionary>
        {
            public override ClassWithIDictionary? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => throw new NotImplementedException();
            public override void Write(Utf8JsonWriter writer, ClassWithIDictionary value, JsonSerializerOptions options)
                => throw new NotImplementedException();
        }
    }
#endif
}
