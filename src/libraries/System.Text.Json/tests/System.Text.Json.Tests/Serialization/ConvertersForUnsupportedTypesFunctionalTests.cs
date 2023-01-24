// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class ConvertersForUnsupportedTypesFunctionalTests
    {
        [Theory]
        [MemberData(nameof(GetTestData))]
        public static void RoundtripValues<T>(string expectedValueAsString, T value)
        {
            JsonSerializerOptions options = new()
            {
                Converters = { new Int128Converter(), new UInt128Converter(), new HalfConverter(), new BigIntegerConverter() }
            };

            ClassWithProperty<T> wrappedValue = new()
            {
                Property = value
            };

            string json = JsonSerializer.Serialize(value, options);
            Assert.Equal(expectedValueAsString, json);

            T deserializedValue = JsonSerializer.Deserialize<T>(json, options);
            Assert.Equal(value, deserializedValue);

            json = JsonSerializer.Serialize(wrappedValue, options);
            Assert.Equal($"{{\"Property\":{expectedValueAsString}}}", json);
            ClassWithProperty<T> deserializedWrappedValue = JsonSerializer.Deserialize<ClassWithProperty<T>>(json, options);
            Assert.Equal(wrappedValue.Property, deserializedWrappedValue.Property);
        }

        public static IEnumerable<object[]> GetTestData()
        {
            NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;

            yield return GetTestCase("0", Int128.Zero);
            yield return GetTestCase("-1", new Int128(ulong.MaxValue, ulong.MaxValue));
            yield return GetTestCase(Int128.MaxValue.ToString(nfi), Int128.MaxValue);
            yield return GetTestCase(ulong.MaxValue.ToString(nfi), new Int128(0UL, ulong.MaxValue));
            yield return GetTestCase(ulong.MaxValue.ToString(nfi) + "173", new Int128(0UL, ulong.MaxValue) * 1000 + 173);

            yield return GetTestCase("0", UInt128.Zero);
            yield return GetTestCase(UInt128.MaxValue.ToString(nfi), UInt128.MaxValue);
            yield return GetTestCase(ulong.MaxValue.ToString(nfi), new UInt128(0UL, ulong.MaxValue));
            yield return GetTestCase(ulong.MaxValue.ToString(nfi) + "173", new UInt128(0UL, ulong.MaxValue) * 1000 + 173);

            yield return GetTestCase("0", Half.Zero);
            yield return GetTestCase(Half.MaxValue.ToString(nfi), Half.MaxValue);
            yield return GetTestCase(Half.MinValue.ToString(nfi), Half.MinValue);
            yield return GetTestCase(((Half)1.45f).ToString(nfi), (Half)1.45f);

            yield return GetTestCase("0", BigInteger.Zero);
            yield return GetTestCase("1", BigInteger.One);
            yield return GetTestCase(ulong.MaxValue.ToString(nfi), (BigInteger)ulong.MaxValue);
            yield return GetTestCase("-123", (BigInteger)(-123));

            static object[] GetTestCase(string expectedValue, object value) => new object[] { expectedValue, value };
        }

        internal class ClassWithProperty<T>
        {
            public T Property { get; set; }
        }

        internal class HalfConverter : SimpleConverter<Half>
        {
            public override Half Parse(string value) => Half.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
            public override string ToString(Half value) => value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        internal class UInt128Converter : SimpleConverter<UInt128>
        {
            public override UInt128 Parse(string value) => UInt128.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
            public override string ToString(UInt128 value) => value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        internal class Int128Converter : SimpleConverter<Int128>
        {
            public override Int128 Parse(string value) => Int128.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
            public override string ToString(Int128 value) => value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        internal class BigIntegerConverter : SimpleConverter<BigInteger>
        {
            public override BigInteger Parse(string value) => BigInteger.Parse(value, CultureInfo.InvariantCulture.NumberFormat);
            public override string ToString(BigInteger value) => value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        internal abstract class SimpleConverter<T> : JsonConverter<T>
        {
            public abstract T Parse(string value);
            public abstract string ToString(T value);

            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.HasValueSequence)
                {
                    return Parse(Encoding.UTF8.GetString(reader.ValueSequence));
                }
                else
                {
                    return Parse(Encoding.UTF8.GetString(reader.ValueSpan));
                }
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                writer.WriteRawValue(ToString(value));
            }
        }
    }
}
