// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ValueTests
    {
        public static bool IsX64 { get; } = Environment.Is64BitProcess;

        [Fact]
        public static void ReadPrimitives()
        {
            int i = JsonSerializer.Deserialize<int>(Encoding.UTF8.GetBytes(@"1"));
            Assert.Equal(1, i);

            int i2 = JsonSerializer.Deserialize<int>("2");
            Assert.Equal(2, i2);

            int? i3 = JsonSerializer.Deserialize<int?>("null");
            Assert.Null(i3);

            long l = JsonSerializer.Deserialize<long>(Encoding.UTF8.GetBytes(long.MaxValue.ToString()));
            Assert.Equal(long.MaxValue, l);

            long l2 = JsonSerializer.Deserialize<long>(long.MaxValue.ToString());
            Assert.Equal(long.MaxValue, l2);

            string s = JsonSerializer.Deserialize<string>(Encoding.UTF8.GetBytes(@"""Hello"""));
            Assert.Equal("Hello", s);

            string s2 = JsonSerializer.Deserialize<string>(@"""Hello""");
            Assert.Equal("Hello", s2);

            Uri u = JsonSerializer.Deserialize<Uri>(@"""""");
            Assert.Equal("", u.OriginalString);
        }

        [Fact]
        public static void ReadPrimitivesWithWhitespace()
        {
            int i = JsonSerializer.Deserialize<int>(Encoding.UTF8.GetBytes(@" 1 "));
            Assert.Equal(1, i);

            int i2 = JsonSerializer.Deserialize<int>("2\t");
            Assert.Equal(2, i2);

            int? i3 = JsonSerializer.Deserialize<int?>("\r\nnull");
            Assert.Null(i3);

            long l = JsonSerializer.Deserialize<long>(Encoding.UTF8.GetBytes("\t" + long.MaxValue.ToString()));
            Assert.Equal(long.MaxValue, l);

            long l2 = JsonSerializer.Deserialize<long>(long.MaxValue.ToString() + " \r\n");
            Assert.Equal(long.MaxValue, l2);

            string s = JsonSerializer.Deserialize<string>(Encoding.UTF8.GetBytes(@"""Hello"" "));
            Assert.Equal("Hello", s);

            string s2 = JsonSerializer.Deserialize<string>(@"  ""Hello"" ");
            Assert.Equal("Hello", s2);

            bool b = JsonSerializer.Deserialize<bool>(" \ttrue ");
            Assert.True(b);

            bool b2 = JsonSerializer.Deserialize<bool>(" false\n");
            Assert.False(b2);
        }

        [Fact]
        public static void ReadPrimitivesFail()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(Encoding.UTF8.GetBytes(@"a")));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int[]>(Encoding.UTF8.GetBytes(@"[1,a]")));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(@"null"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(@""""""));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTime>("\"abc\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTimeOffset>("\"abc\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Guid>("\"abc\""));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte>("\"abc\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte>("1.1"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<sbyte>("\"abc\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<sbyte>("1.1"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<short>("\"abc\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<short>("1.1"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ushort>("\"abc\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ushort>("1.1"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>("\"abc\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>("1.1"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<uint>("\"abc\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<uint>("1.1"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>("\"abc\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>("1.1"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ulong>("\"abc\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ulong>("1.1"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<float>("\"abc\""));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double>("\"abc\""));
        }

        [Theory]
        [InlineData(typeof(bool))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(char))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(double))]
        [InlineData(typeof(JsonTokenType))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(short))]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(sbyte))]
        [InlineData(typeof(float))]
        [InlineData(typeof(string))]
        [InlineData(typeof(ushort))]
        [InlineData(typeof(uint))]
        [InlineData(typeof(ulong))]
        [InlineData(typeof(Uri))]
        [InlineData(typeof(Version))]
        public static void PrimitivesShouldFailWithArrayOrObjectAssignment(Type primitiveType)
        {
            // This test lines up with the built in JsonConverters
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(@"[]", primitiveType));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(@"{}", primitiveType));
        }

        [Fact]
        public static void EmptyStringInput()
        {
            string obj = JsonSerializer.Deserialize<string>(@"""""");
            Assert.Equal(string.Empty, obj);
        }

        [Fact]
        public static void ReadPrimitiveExtraBytesFail()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int[]>("[2] {3}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int[]>(Encoding.UTF8.GetBytes(@"[2] {3}")));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<string>(@"""Hello"" 42"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<string>(Encoding.UTF8.GetBytes(@"""Hello"" 42")));
        }

        [Fact]
        public static void RangeFail()
        {
            // These have custom code because the reader doesn't natively support:
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte>((byte.MinValue - 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte>((byte.MaxValue + 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte?>((byte.MinValue - 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte?>((byte.MaxValue + 1).ToString()));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<sbyte>((sbyte.MinValue - 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<sbyte>((sbyte.MaxValue + 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<sbyte?>((sbyte.MinValue - 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<sbyte?>((sbyte.MaxValue + 1).ToString()));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<short>((short.MinValue - 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<short>((short.MaxValue + 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<short?>((short.MinValue - 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<short?>((short.MaxValue + 1).ToString()));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ushort>((ushort.MinValue - 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ushort>((ushort.MaxValue + 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ushort?>((ushort.MinValue - 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ushort?>((ushort.MaxValue + 1).ToString()));

            // These are natively supported by the reader:
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(((long)int.MinValue - 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(((long)int.MaxValue + 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int?>(((long)int.MinValue - 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int?>(((long)int.MaxValue + 1).ToString()));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<uint>(((long)uint.MinValue - 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<uint>(((long)uint.MaxValue + 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<uint?>(((long)uint.MinValue - 1).ToString()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<uint?>(((long)uint.MaxValue + 1).ToString()));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(long.MinValue.ToString() + "0"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(long.MaxValue.ToString() + "0"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long?>(long.MinValue.ToString() + "0"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long?>(long.MaxValue.ToString() + "0"));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ulong>(ulong.MinValue.ToString() + "0"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ulong>(ulong.MaxValue.ToString() + "0"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ulong?>(ulong.MinValue.ToString() + "0"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ulong?>(ulong.MaxValue.ToString() + "0"));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<decimal>(decimal.MinValue.ToString() + "0"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<decimal>(decimal.MaxValue.ToString() + "0"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<decimal?>(decimal.MinValue.ToString() + "0"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<decimal?>(decimal.MaxValue.ToString() + "0"));
        }

        [Fact]
        public static void RangePass()
        {
            Assert.Equal(byte.MaxValue, JsonSerializer.Deserialize<byte>(byte.MaxValue.ToString()));
            Assert.Equal(byte.MaxValue, JsonSerializer.Deserialize<byte?>(byte.MaxValue.ToString()));

            Assert.Equal(sbyte.MaxValue, JsonSerializer.Deserialize<sbyte>(sbyte.MaxValue.ToString()));
            Assert.Equal(sbyte.MaxValue, JsonSerializer.Deserialize<sbyte?>(sbyte.MaxValue.ToString()));

            Assert.Equal(short.MaxValue, JsonSerializer.Deserialize<short>(short.MaxValue.ToString()));
            Assert.Equal(short.MaxValue, JsonSerializer.Deserialize<short?>(short.MaxValue.ToString()));

            Assert.Equal(ushort.MaxValue, JsonSerializer.Deserialize<ushort>(ushort.MaxValue.ToString()));
            Assert.Equal(ushort.MaxValue, JsonSerializer.Deserialize<ushort?>(ushort.MaxValue.ToString()));

            Assert.Equal(int.MaxValue, JsonSerializer.Deserialize<int>(int.MaxValue.ToString()));
            Assert.Equal(int.MaxValue, JsonSerializer.Deserialize<int?>(int.MaxValue.ToString()));

            Assert.Equal(uint.MaxValue, JsonSerializer.Deserialize<uint>(uint.MaxValue.ToString()));
            Assert.Equal(uint.MaxValue, JsonSerializer.Deserialize<uint?>(uint.MaxValue.ToString()));

            Assert.Equal(long.MaxValue, JsonSerializer.Deserialize<long>(long.MaxValue.ToString()));
            Assert.Equal(long.MaxValue, JsonSerializer.Deserialize<long?>(long.MaxValue.ToString()));

            Assert.Equal(ulong.MaxValue, JsonSerializer.Deserialize<ulong>(ulong.MaxValue.ToString()));
            Assert.Equal(ulong.MaxValue, JsonSerializer.Deserialize<ulong?>(ulong.MaxValue.ToString()));

            Assert.Equal(decimal.MaxValue, JsonSerializer.Deserialize<decimal>(decimal.MaxValue.ToString(CultureInfo.InvariantCulture)));
            Assert.Equal(decimal.MaxValue, JsonSerializer.Deserialize<decimal?>(decimal.MaxValue.ToString(CultureInfo.InvariantCulture)));
        }

        [Fact]
        public static void RangePassFloatingPoint()
        {
            // Verify overflow\underflow.
            AssertFloatingPointBehavior(netcoreExpectedValue: float.NegativeInfinity, () => JsonSerializer.Deserialize<float>(float.MinValue.ToString(CultureInfo.InvariantCulture) + "0"));
            AssertFloatingPointBehavior(netcoreExpectedValue: float.PositiveInfinity, () => JsonSerializer.Deserialize<float>(float.MaxValue.ToString(CultureInfo.InvariantCulture) + "0"));
            AssertFloatingPointBehavior(netcoreExpectedValue: float.NegativeInfinity, () => JsonSerializer.Deserialize<float?>(float.MinValue.ToString(CultureInfo.InvariantCulture) + "0").Value);
            AssertFloatingPointBehavior(netcoreExpectedValue: float.PositiveInfinity, () => JsonSerializer.Deserialize<float?>(float.MaxValue.ToString(CultureInfo.InvariantCulture) + "0").Value);

            AssertFloatingPointBehavior(netcoreExpectedValue: double.NegativeInfinity, () => JsonSerializer.Deserialize<double>(double.MinValue.ToString(CultureInfo.InvariantCulture) + "0"));
            AssertFloatingPointBehavior(netcoreExpectedValue: double.PositiveInfinity, () => JsonSerializer.Deserialize<double>(double.MaxValue.ToString(CultureInfo.InvariantCulture) + "0"));
            AssertFloatingPointBehavior(netcoreExpectedValue: double.NegativeInfinity, () => JsonSerializer.Deserialize<double?>(double.MinValue.ToString(CultureInfo.InvariantCulture) + "0").Value);
            AssertFloatingPointBehavior(netcoreExpectedValue: double.PositiveInfinity, () => JsonSerializer.Deserialize<double?>(double.MaxValue.ToString(CultureInfo.InvariantCulture) + "0").Value);

            // Verify sign is correct.
            AssertFloatingPointBehavior(netfxExpectedValue: 0x00000000u, netcoreExpectedValue: 0x00000000u, () => (uint)SingleToInt32Bits(JsonSerializer.Deserialize<float>("0")));
            AssertFloatingPointBehavior(netfxExpectedValue: 0x00000000u, netcoreExpectedValue: 0x80000000u, () => (uint)SingleToInt32Bits(JsonSerializer.Deserialize<float>("-0")));
            AssertFloatingPointBehavior(netfxExpectedValue: 0x00000000u, netcoreExpectedValue: 0x80000000u, () => (uint)SingleToInt32Bits(JsonSerializer.Deserialize<float>("-0.0")));

            AssertFloatingPointBehavior(netfxExpectedValue: 0x0000000000000000ul, netcoreExpectedValue: 0x0000000000000000ul, () => (ulong)BitConverter.DoubleToInt64Bits(JsonSerializer.Deserialize<double>("0")));
            AssertFloatingPointBehavior(netfxExpectedValue: 0x0000000000000000ul, netcoreExpectedValue: 0x8000000000000000ul, () => (ulong)BitConverter.DoubleToInt64Bits(JsonSerializer.Deserialize<double>("-0")));
            AssertFloatingPointBehavior(netfxExpectedValue: 0x0000000000000000ul, netcoreExpectedValue: 0x8000000000000000ul, () => (ulong)BitConverter.DoubleToInt64Bits(JsonSerializer.Deserialize<double>("-0.0")));

            // Verify Round-tripping.
            Assert.Equal(float.MaxValue, JsonSerializer.Deserialize<float>(float.MaxValue.ToString(JsonTestHelper.SingleFormatString, CultureInfo.InvariantCulture)));
            Assert.Equal(float.MaxValue, JsonSerializer.Deserialize<float?>(float.MaxValue.ToString(JsonTestHelper.SingleFormatString, CultureInfo.InvariantCulture)));

            Assert.Equal(double.MaxValue, JsonSerializer.Deserialize<double>(double.MaxValue.ToString(JsonTestHelper.DoubleFormatString, CultureInfo.InvariantCulture)));
            Assert.Equal(double.MaxValue, JsonSerializer.Deserialize<double?>(double.MaxValue.ToString(JsonTestHelper.DoubleFormatString, CultureInfo.InvariantCulture)));
        }

        [Fact]
        public static void ValueFail()
        {
            string unexpectedString = @"""unexpected string""";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<sbyte>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<sbyte?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<short>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<short?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ushort>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ushort?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<float>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<float?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<uint>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<uint?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<long?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ulong>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ulong?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<decimal>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<decimal?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<double?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTime>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTime?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTimeOffset>(unexpectedString));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DateTimeOffset?>(unexpectedString));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<string>("1"));

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<char>("1"));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<char?>("1"));

            // This throws because Enum is an abstract type.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Enum>(unexpectedString));
        }

        [Fact]
        public static void ReadVersion()
        {
            Version version;

            version = JsonSerializer.Deserialize<Version>(@"""1.2""");
            Assert.Equal(new Version(1, 2), version);

            version = JsonSerializer.Deserialize<Version>(@"""1.2.3""");
            Assert.Equal(new Version(1, 2, 3), version);

            version = JsonSerializer.Deserialize<Version>(@"""1.2.3.4""");
            Assert.Equal(new Version(1, 2, 3, 4), version);

            version = JsonSerializer.Deserialize<Version>("null");
            Assert.Null(version);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Version>(@""""""));
        }

        [Fact]
        public static void ReadPrimitiveUri()
        {
            Uri uri = JsonSerializer.Deserialize<Uri>(@"""https://domain/path""");
            Assert.Equal(@"https://domain/path", uri.ToString());
            Assert.Equal("https://domain/path", uri.OriginalString);

            uri = JsonSerializer.Deserialize<Uri>(@"""https:\/\/domain\/path""");
            Assert.Equal(@"https://domain/path", uri.ToString());
            Assert.Equal("https://domain/path", uri.OriginalString);

            uri = JsonSerializer.Deserialize<Uri>(@"""https:\u002f\u002fdomain\u002fpath""");
            Assert.Equal(@"https://domain/path", uri.ToString());
            Assert.Equal("https://domain/path", uri.OriginalString);

            uri = JsonSerializer.Deserialize<Uri>(@"""~/path""");
            Assert.Equal("~/path", uri.ToString());
            Assert.Equal("~/path", uri.OriginalString);
        }

        private static int SingleToInt32Bits(float value)
        {
#if BUILDING_INBOX_LIBRARY
            return BitConverter.SingleToInt32Bits(value);
#else
            return Unsafe.As<float, int>(ref value);
#endif
        }

        private static void AssertFloatingPointBehavior<T>(T netcoreExpectedValue, Func<T> testCode)
        {
            if (PlatformDetection.IsNetFramework)
            {
                Assert.Throws<JsonException>(() => testCode());
            }
            else
            {
                Assert.Equal(netcoreExpectedValue, testCode());
            }
        }

        private static void AssertFloatingPointBehavior<T>(T netfxExpectedValue, T netcoreExpectedValue, Func<T> testCode)
        {
            if (PlatformDetection.IsNetFramework)
            {
                Assert.Equal(netfxExpectedValue, testCode());
            }
            else
            {
                Assert.Equal(netcoreExpectedValue, testCode());
            }
        }

        private const long ArrayPoolMaxSizeBeforeUsingNormalAlloc = 1024 * 1024;
        private const int MaxExpansionFactorWhileTranscoding = 3;
        private const long Threshold = ArrayPoolMaxSizeBeforeUsingNormalAlloc / MaxExpansionFactorWhileTranscoding;

        [Theory]
        [InlineData(Threshold - 3)]
        [InlineData(Threshold - 2)]
        [InlineData(Threshold - 1)]
        [InlineData(Threshold)]
        [InlineData(Threshold + 1)]
        [InlineData(Threshold + 2)]
        [InlineData(Threshold + 3)]
        public static void LongInputString(int length)
        {
            // Verify boundary conditions in Deserialize() that inspect the size to determine allocation strategy.
            DeserializeLongJsonString(length);
        }

        private const int MaxInt = int.MaxValue / MaxExpansionFactorWhileTranscoding;
        private const int MaximumPossibleStringLength = int.MaxValue / 2 - 32;

        // NOTE: VeryLongInputString test is constrained to run on Windows and MacOSX because it causes
        //       problems on Linux due to the way deferred memory allocation works. On Linux, the allocation can
        //       succeed even if there is not enough memory but then the test may get killed by the OOM killer at the
        //       time the memory is accessed which triggers the full memory allocation.
        [ConditionalTheory(nameof(IsX64))]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        [InlineData(MaxInt)]
        [InlineData(MaximumPossibleStringLength)]
        [OuterLoop]
        public static void VeryLongInputString(int length)
        {
            // Verify that deserializer does not do any multiplication or addition on the string length
            DeserializeLongJsonString(length);
        }

        private static void DeserializeLongJsonString(int stringLength)
        {
            string json;
            char fillChar = 'x';

#if BUILDING_INBOX_LIBRARY
            json = string.Create(stringLength, fillChar, (chars, fillChar) =>
            {
                chars.Fill(fillChar);
                chars[0] = '"';
                chars[chars.Length - 1] = '"';
            });
#else
            string repeated = new string(fillChar, stringLength - 2);
            json = $"\"{repeated}\"";
#endif
            Assert.Equal(stringLength, json.Length);

            string str = JsonSerializer.Deserialize<string>(json);
            Assert.True(json.AsSpan(1, json.Length - 2).SequenceEqual(str.AsSpan()));
        }
    }
}
