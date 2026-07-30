// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Tests for the built-in converters of the IEEE 754 types that were added in .NET 11:
    /// <see cref="BFloat16"/>, <see cref="Decimal32"/>, <see cref="Decimal64"/> and <see cref="Decimal128"/>.
    /// </summary>
    public abstract class Ieee754NumericConverterTests : SerializerTests
    {
        protected Ieee754NumericConverterTests(JsonSerializerWrapper serializer) : base(serializer)
        {
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("-1")]
        [InlineData("0.5")]
        [InlineData("-0.75")]
        [InlineData("1.5")]
        [InlineData("256")]
        [InlineData("-1024")]
        public Task BFloat16_Roundtrip(string json) => AssertRoundtrip<BFloat16>(json);

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("-1")]
        [InlineData("1.5")]
        [InlineData("-2.25")]
        [InlineData("1000000")]
        public Task Decimal32_Roundtrip(string json) => AssertRoundtrip<Decimal32>(json);

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("-1")]
        [InlineData("1.5")]
        [InlineData("-2.25")]
        [InlineData("1234567890123456")]
        public Task Decimal64_Roundtrip(string json) => AssertRoundtrip<Decimal64>(json);

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("-1")]
        [InlineData("1.5")]
        [InlineData("-2.25")]
        [InlineData("1234567890123456789012345678901234")]
        public Task Decimal128_Roundtrip(string json) => AssertRoundtrip<Decimal128>(json);

        [Fact]
        public async Task Decimal128_ExtremeValues_Roundtrip()
        {
            // MaxValue and Epsilon are the values most likely to exercise the converter's pooled-buffer
            // growth path, since their general format is the longest the type can produce.
            foreach (Decimal128 value in new[] { Decimal128.MaxValue, Decimal128.MinValue, Decimal128.Epsilon })
            {
                string json = await Serializer.SerializeWrapper(value);
                Assert.Equal(value.ToString(null, CultureInfo.InvariantCulture), json);
                Assert.Equal(value, await Serializer.DeserializeWrapper<Decimal128>(json));
            }
        }

        [Theory]
        [MemberData(nameof(SupportedTypes))]
        public void MetadataServicesExposesConverterForType(Type type, JsonConverter converter)
        {
            Assert.Equal(type, converter.Type);

            // Both the reflection-based resolver and the source generator must map these types to a
            // value converter rather than treating them as object or unsupported types.
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type);
            Assert.Equal(JsonTypeInfoKind.None, typeInfo.Kind);
        }

        public static IEnumerable<object[]> SupportedTypes()
        {
            yield return new object[] { typeof(BFloat16), JsonMetadataServices.BFloat16Converter };
            yield return new object[] { typeof(Decimal32), JsonMetadataServices.Decimal32Converter };
            yield return new object[] { typeof(Decimal64), JsonMetadataServices.Decimal64Converter };
            yield return new object[] { typeof(Decimal128), JsonMetadataServices.Decimal128Converter };
        }

        [Fact]
        public async Task NamedFloatingPointLiterals_Roundtrip()
        {
            await AssertNamedLiterals(BFloat16.NaN, BFloat16.PositiveInfinity, BFloat16.NegativeInfinity);
            await AssertNamedLiterals(Decimal32.NaN, Decimal32.PositiveInfinity, Decimal32.NegativeInfinity);
            await AssertNamedLiterals(Decimal64.NaN, Decimal64.PositiveInfinity, Decimal64.NegativeInfinity);
            await AssertNamedLiterals(Decimal128.NaN, Decimal128.PositiveInfinity, Decimal128.NegativeInfinity);
        }

        [Fact]
        public async Task AllowReadingFromString()
        {
            var options = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString };

            Assert.Equal((BFloat16)1.5f, await Serializer.DeserializeWrapper<BFloat16>("\"1.5\"", options));
            Assert.Equal(Parse<Decimal32>("1.5"), await Serializer.DeserializeWrapper<Decimal32>("\"1.5\"", options));
            Assert.Equal(Parse<Decimal64>("1.5"), await Serializer.DeserializeWrapper<Decimal64>("\"1.5\"", options));
            Assert.Equal(Parse<Decimal128>("1.5"), await Serializer.DeserializeWrapper<Decimal128>("\"1.5\"", options));
        }

        [Fact]
        public async Task WriteAsString()
        {
            var options = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.WriteAsString };

            Assert.Equal("\"1.5\"", await Serializer.SerializeWrapper((BFloat16)1.5f, options));
            Assert.Equal("\"1.5\"", await Serializer.SerializeWrapper(Parse<Decimal32>("1.5"), options));
            Assert.Equal("\"1.5\"", await Serializer.SerializeWrapper(Parse<Decimal64>("1.5"), options));
            Assert.Equal("\"1.5\"", await Serializer.SerializeWrapper(Parse<Decimal128>("1.5"), options));
        }

        [Fact]
        public async Task DictionaryKeys_Roundtrip()
        {
            await AssertDictionaryKeyRoundtrip((BFloat16)1.5f, "1.5");
            await AssertDictionaryKeyRoundtrip(Parse<Decimal32>("1.5"), "1.5");
            await AssertDictionaryKeyRoundtrip(Parse<Decimal64>("1.5"), "1.5");
            await AssertDictionaryKeyRoundtrip(Parse<Decimal128>("1.5"), "1.5");
        }

        [Fact]
        public async Task FullPrecisionValues_RoundtripAsKeysAndStrings()
        {
            // Reading a string or property name buffers the token, so exercise the widest significand
            // each type can represent: 7, 16 and 34 significant digits for the decimal types.
            await AssertFullPrecision(Parse<Decimal32>("-1.234567E-95"));
            await AssertFullPrecision(Parse<Decimal64>("-1.234567890123456E-383"));
            await AssertFullPrecision(Parse<Decimal128>("-1.234567890123456789012345678901234E-6143"));
            await AssertFullPrecision(BFloat16.Epsilon);

            async Task AssertFullPrecision<T>(T value) where T : struct, IFloatingPointIeee754<T>
            {
                string text = value.ToString(null, CultureInfo.InvariantCulture);

                await AssertDictionaryKeyRoundtrip(value, text);

                var options = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString };
                Assert.Equal(value, await Serializer.DeserializeWrapper<T>($"\"{text}\"", options));
            }
        }

        [Fact]
        public async Task NumberOverflowingToInfinity_IsReadAsInfinity()
        {
            // IEEE 754 rounds a magnitude beyond the largest finite value to infinity, and the JSON
            // number grammar cannot express infinity, so overflow is only detectable by inspecting the
            // parsed result. Matches the behavior of float and double.
            const string Json = "1e999999";

            Assert.Equal(BFloat16.PositiveInfinity, await Serializer.DeserializeWrapper<BFloat16>(Json));
            Assert.Equal(Decimal32.PositiveInfinity, await Serializer.DeserializeWrapper<Decimal32>(Json));
            Assert.Equal(Decimal64.PositiveInfinity, await Serializer.DeserializeWrapper<Decimal64>(Json));
            Assert.Equal(Decimal128.PositiveInfinity, await Serializer.DeserializeWrapper<Decimal128>(Json));

            Assert.Equal(BFloat16.NegativeInfinity, await Serializer.DeserializeWrapper<BFloat16>("-" + Json));
            Assert.Equal(Decimal128.NegativeInfinity, await Serializer.DeserializeWrapper<Decimal128>("-" + Json));
        }

        [Fact]
        public async Task StringOverflowingToInfinity_Throws()
        {
            // Unlike number tokens, strings are rejected when they do not parse to a finite value.
            // Matches the behavior of float and double.
            var options = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString };
            const string Json = "\"1e999999\"";

            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<BFloat16>(Json, options));
            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<Decimal32>(Json, options));
            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<Decimal64>(Json, options));
            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<Decimal128>(Json, options));
        }

        [Fact]
        public async Task NonFiniteValues_WithoutNamedLiterals_Throw()
        {
            // NaN and infinity have no JSON number representation, so serializing them without
            // AllowNamedFloatingPointLiterals reports the same error float and double do.
            await AssertNotSupported(BFloat16.NaN, BFloat16.PositiveInfinity);
            await AssertNotSupported(Decimal32.NaN, Decimal32.PositiveInfinity);
            await AssertNotSupported(Decimal64.NaN, Decimal64.PositiveInfinity);
            await AssertNotSupported(Decimal128.NaN, Decimal128.NegativeInfinity);

            async Task AssertNotSupported<T>(T nan, T infinity) where T : struct, IFloatingPointIeee754<T>
            {
                await Assert.ThrowsAsync<ArgumentException>(() => Serializer.SerializeWrapper(nan));
                await Assert.ThrowsAsync<ArgumentException>(() => Serializer.SerializeWrapper(infinity));

                var dictionary = new Dictionary<T, int> { [infinity] = 42 };
                await Assert.ThrowsAsync<ArgumentException>(() => Serializer.SerializeWrapper(dictionary));
            }
        }

        [Fact]
        public async Task PocoWithIeee754Properties_Roundtrips()
        {
            var poco = new Ieee754Poco
            {
                BFloat16Value = (BFloat16)1.5f,
                Decimal32Value = Parse<Decimal32>("2.5"),
                Decimal64Value = Parse<Decimal64>("3.5"),
                Decimal128Value = Parse<Decimal128>("4.5"),
            };

            string json = await Serializer.SerializeWrapper(poco);
            JsonTestHelper.AssertJsonEqual(
                """{"BFloat16Value":1.5,"Decimal32Value":2.5,"Decimal64Value":3.5,"Decimal128Value":4.5}""",
                json);

            Ieee754Poco deserialized = await Serializer.DeserializeWrapper<Ieee754Poco>(json);
            Assert.Equal(poco.BFloat16Value, deserialized.BFloat16Value);
            Assert.Equal(poco.Decimal32Value, deserialized.Decimal32Value);
            Assert.Equal(poco.Decimal64Value, deserialized.Decimal64Value);
            Assert.Equal(poco.Decimal128Value, deserialized.Decimal128Value);
        }

        [Theory]
        [InlineData("\"abc\"")]
        [InlineData("\"naN\"")]
        [InlineData("\"1.5abc\"")]
        public async Task InvalidValues_ThrowJsonException(string json)
        {
            var options = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString };

            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<BFloat16>(json, options));
            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<Decimal32>(json, options));
            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<Decimal64>(json, options));
            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<Decimal128>(json, options));
        }

        [Fact]
        public void JsonValue_ReportsNumberValueKind()
        {
            AssertNumberValueKind((BFloat16)1.5f);
            AssertNumberValueKind(Parse<Decimal32>("1.5"));
            AssertNumberValueKind(Parse<Decimal64>("1.5"));
            AssertNumberValueKind(Parse<Decimal128>("1.5"));

            void AssertNumberValueKind<T>(T value) where T : struct, IFloatingPointIeee754<T>
                => Assert.Equal(JsonValueKind.Number, JsonValue.Create(value, Serializer.GetTypeInfo<T>()).GetValueKind());
        }

        private static T Parse<T>(string value) where T : struct, IFloatingPointIeee754<T>
            => T.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

        private async Task AssertRoundtrip<T>(string json) where T : struct, IFloatingPointIeee754<T>
        {
            T value = Parse<T>(json);
            Assert.Equal(json, value.ToString(null, CultureInfo.InvariantCulture));

            // Exercises root values, object properties, collection elements, dictionary values,
            // JsonNode values and boxed values.
            await TestMultiContextSerialization(value, json);
            await TestMultiContextDeserialization(json, value);
        }

        private async Task AssertNamedLiterals<T>(T nan, T positiveInfinity, T negativeInfinity)
            where T : struct, IFloatingPointIeee754<T>
        {
            var options = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };

            Assert.Equal("\"NaN\"", await Serializer.SerializeWrapper(nan, options));
            Assert.Equal("\"Infinity\"", await Serializer.SerializeWrapper(positiveInfinity, options));
            Assert.Equal("\"-Infinity\"", await Serializer.SerializeWrapper(negativeInfinity, options));

            Assert.True(T.IsNaN(await Serializer.DeserializeWrapper<T>("\"NaN\"", options)));
            Assert.Equal(positiveInfinity, await Serializer.DeserializeWrapper<T>("\"Infinity\"", options));
            Assert.Equal(negativeInfinity, await Serializer.DeserializeWrapper<T>("\"-Infinity\"", options));

            // Named literals are rejected unless the corresponding flag is enabled.
            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<T>("\"NaN\""));
        }

        private async Task AssertDictionaryKeyRoundtrip<T>(T value, string expectedKey)
            where T : struct, IFloatingPointIeee754<T>
        {
            var dictionary = new Dictionary<T, int> { [value] = 42 };

            string json = await Serializer.SerializeWrapper(dictionary);
            Assert.Equal($"{{\"{expectedKey}\":42}}", json);

            Dictionary<T, int> deserialized = await Serializer.DeserializeWrapper<Dictionary<T, int>>(json);
            Assert.Equal(42, deserialized[value]);
        }

        public class Ieee754Poco
        {
            public BFloat16 BFloat16Value { get; set; }
            public Decimal32 Decimal32Value { get; set; }
            public Decimal64 Decimal64Value { get; set; }
            public Decimal128 Decimal128Value { get; set; }
        }
    }
}
