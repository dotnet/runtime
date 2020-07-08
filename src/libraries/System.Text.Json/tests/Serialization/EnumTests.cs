// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class EnumTests
    {
        private const string UInt64MaxPlus1 = "18446744073709551616"; // ulong.MaxValue + 1;
        private const string MinusUInt64MaxMinus1 = "-18446744073709551616"; // -ulong.MaxValue - 1

        private const string Int64MaxPlus1 = "9223372036854775808"; // long.MaxValue + 1;
        private const string Int64MinMinus1 = "-9223372036854775809"; // long.MinValue - 1

        private const string UInt32MaxPlus1 = "4294967296"; // uint.MaxValue + 1;
        private const string MinusUInt32MaxMinus1 = "-4294967296"; // -uint.MaxValue - 1

        private const string Int32MaxPlus1 = "2147483648"; // int.MaxValue + 1;
        private const string Int32MinMinus1 = "-2147483649"; // int.MinValue - 1

        private const string UInt16MaxPlus1 = "65536"; // ushort.MaxValue + 1;
        private const string MinusUInt16MaxMinus1 = "-65536"; // -ushort.MaxValue - 1

        private const string Int16MaxPlus1 = "32768"; // short.MaxValue + 1;
        private const string Int16MinMinus1 = "-32769"; // short.MinValue - 1

        private const string ByteMaxPlus1 = "256"; // byte.MaxValue + 1;
        private const string MinusByteMaxMinus1 = "-257"; // -byte.MaxValue - 1

        private const string SByteMaxPlus1 = "128"; // sbyte.MaxValue + 1;
        private const string MinusSByteMaxMinus1 = "-129"; // -sbyte.MaxValue - 1

        [Fact]
        public static void EnumAsStringFail()
        {
            string json = @"{ ""MyEnum"" : ""Two"" }";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SimpleTestClass>(json));
        }

        [Theory]
        [MemberData(nameof(Parse_OutOfRange))]
        public static void Parse_OutOfRange_Throws(object value, string name)
        {
            string json = $"{{ \"{ name }\" : { value } }}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SimpleTestClass>(json));
            json = $"{{ \"{ name }\" : \"{ value }\" }}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SimpleTestClass>(json));
        }

        [Theory]
        [MemberData(nameof(Parse_WithinRange_SignedAsString))]
        public static void Parse_WithinRange_Signed_ReturnsWithCorrectType(Type expectedType, string value, string name)
        {
            string json = $"{{ \"{ name }\" : { value } }}";
            SimpleTestClass result = JsonSerializer.Deserialize<SimpleTestClass>(json);
            object expected = Enum.ToObject(expectedType, long.Parse(value));
            Assert.Equal(expected, GetProperty(result, name));

            json = $"{{ \"{ name }\" : \"{ value }\" }}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SimpleTestClass>(json));
        }

        [Theory]
        [MemberData(nameof(Parse_WithinRange_Signed))]
        public static void Parse_WithinRange_Signed_ReturnsExpected(object expected, long value, string name)
        {
            string json = $"{{ \"{ name }\" : { value } }}";
            SimpleTestClass result = JsonSerializer.Deserialize<SimpleTestClass>(json);
            Assert.Equal(expected, GetProperty(result, name));

            json = $"{{ \"{ name }\" : \"{ value }\" }}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SimpleTestClass>(json));
        }

        [Theory]
        [MemberData(nameof(Parse_WithinRange_UnsignedAsString))]
        public static void Parse_WithinRange_Unsigned_ReturnsWithCorrectType(Type expectedType, string value, string name)
        {
            string json = $"{{ \"{ name }\" : { value } }}";
            SimpleTestClass result = JsonSerializer.Deserialize<SimpleTestClass>(json);
            object expected = Enum.ToObject(expectedType, ulong.Parse(value));
            Assert.Equal(expected, GetProperty(result, name));

            json = $"{{ \"{ name }\" : \"{ value }\" }}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SimpleTestClass>(json));
        }

        [Theory]
        [MemberData(nameof(Parse_WithinRange_Unsigned))]
        public static void Parse_WithinRange_Unsigned_ReturnsExpected(object expected, ulong value, string name)
        {
            string json = $"{{ \"{ name }\" : { value } }}";
            SimpleTestClass result = JsonSerializer.Deserialize<SimpleTestClass>(json);
            Assert.Equal(expected, GetProperty(result, name));

            json = $"{{ \"{ name }\" : \"{ value }\" }}";
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SimpleTestClass>(json));
        }

        private static object GetProperty(SimpleTestClass testClass, string propertyName)
            => testClass.GetType().GetProperty(propertyName).GetValue(testClass, null);

        [Theory]
        [MemberData(nameof(ToString_WithinRange))]
        public static void ToString_WithinRange_ReturnsSameValue(object expected, object enumValue)
        {
            string json = JsonSerializer.Serialize(enumValue);
            Assert.Equal(expected.ToString(), json);
        }

        [Theory]
        [MemberData(nameof(ToString_ExceedMaxCapacity))]
        public static void ToString_ExceedMaxCapacity_ResetsBackToMinimum(int timesOverflow, string maxCapacity, Type type, long expected)
        {
            object enumValue = Enum.ToObject(type, long.Parse(maxCapacity) * timesOverflow);
            string json = JsonSerializer.Serialize(enumValue);
            Assert.Equal(expected.ToString(), json);
        }

        public static IEnumerable<object[]> Parse_OutOfRange
        {
            get
            {
                yield return new object[] { -1, "MyByteEnum" };
                yield return new object[] { -1, "MyUInt32Enum" };
                yield return new object[] { -1, "MyUInt64Enum" };
                yield return new object[] { UInt32.MaxValue, "MyByteEnum" };
                yield return new object[] { UInt32.MaxValue, "MyEnum" };
                yield return new object[] { UInt64.MaxValue, "MyByteEnum" };
                yield return new object[] { UInt64.MaxValue, "MyEnum" };
                yield return new object[] { UInt64.MaxValue, "MyUInt32Enum" };
                yield return new object[] { SByteMaxPlus1, "MySByteEnum" };
                yield return new object[] { Int16MaxPlus1, "MyInt16Enum" };
                yield return new object[] { Int32MaxPlus1, "MyInt32Enum" };
                yield return new object[] { Int32MaxPlus1, "MyEnum" };
                yield return new object[] { Int32MaxPlus1, "MyByteEnum" };
                yield return new object[] { Int64MaxPlus1, "MyEnum" };
                yield return new object[] { Int64MaxPlus1, "MyByteEnum" };
                yield return new object[] { Int64MaxPlus1, "MyUInt32Enum" };
                yield return new object[] { ByteMaxPlus1, "MyByteEnum" };
                yield return new object[] { UInt32MaxPlus1, "MyEnum" };
                yield return new object[] { UInt32MaxPlus1, "MyByteEnum" };
                yield return new object[] { UInt32MaxPlus1, "MyUInt32Enum" };
                yield return new object[] { UInt64MaxPlus1, "MyEnum" };
                yield return new object[] { UInt64MaxPlus1, "MyByteEnum" };
                yield return new object[] { UInt64MaxPlus1, "MyUInt32Enum" };
                yield return new object[] { UInt64MaxPlus1, "MyUInt64Enum" };
                yield return new object[] { UInt16MaxPlus1, "MyByteEnum" };
                yield return new object[] { UInt16MaxPlus1, "MyUInt16Enum" };
                yield return new object[] { Int16MinMinus1, "MyByteEnum" };
                yield return new object[] { Int16MinMinus1, "MyUInt32Enum" };
                yield return new object[] { Int16MinMinus1, "MyUInt64Enum" };
                yield return new object[] { Int32MinMinus1, "MyEnum" };
                yield return new object[] { Int32MinMinus1, "MyByteEnum" };
                yield return new object[] { Int32MinMinus1, "MyUInt32Enum" };
                yield return new object[] { Int32MinMinus1, "MyUInt64Enum" };
                yield return new object[] { Int64MinMinus1, "MyEnum" };
                yield return new object[] { Int64MinMinus1, "MyByteEnum" };
                yield return new object[] { Int64MinMinus1, "MyUInt32Enum" };
                yield return new object[] { Int64MinMinus1, "MyUInt64Enum" };
                yield return new object[] { MinusByteMaxMinus1, "MyByteEnum" };
                yield return new object[] { MinusByteMaxMinus1, "MyUInt32Enum" };
                yield return new object[] { MinusByteMaxMinus1, "MyUInt64Enum" };
                yield return new object[] { MinusUInt16MaxMinus1, "MyByteEnum" };
                yield return new object[] { MinusUInt16MaxMinus1, "MyUInt32Enum" };
                yield return new object[] { MinusUInt16MaxMinus1, "MyUInt64Enum" };
                yield return new object[] { MinusUInt32MaxMinus1, "MyEnum" };
                yield return new object[] { MinusUInt32MaxMinus1, "MyByteEnum" };
                yield return new object[] { MinusUInt32MaxMinus1, "MyUInt32Enum" };
                yield return new object[] { MinusUInt32MaxMinus1, "MyUInt64Enum" };
                yield return new object[] { MinusUInt64MaxMinus1, "MyEnum" };
                yield return new object[] { MinusUInt64MaxMinus1, "MyByteEnum" };
                yield return new object[] { MinusUInt64MaxMinus1, "MyUInt32Enum" };
                yield return new object[] { MinusUInt64MaxMinus1, "MyUInt64Enum" };
            }
        }

        public static IEnumerable<object[]> Parse_WithinRange_SignedAsString
        {
            get
            {
                yield return new object[] { typeof(SampleEnum), ByteMaxPlus1, "MyEnum" };
                yield return new object[] { typeof(SampleEnum), Int16MaxPlus1, "MyEnum" };
                yield return new object[] { typeof(SampleEnum), UInt16MaxPlus1, "MyEnum" };
                yield return new object[] { typeof(SampleEnum), MinusByteMaxMinus1, "MyEnum" };
                yield return new object[] { typeof(SampleEnum), MinusUInt16MaxMinus1, "MyEnum" };
            }
        }

        public static IEnumerable<object[]> Parse_WithinRange_Signed
        {
            get
            {
                yield return new object[] { SampleEnumSByte.MinNegative, sbyte.MinValue, "MySByteEnum" };
                yield return new object[] { SampleEnumInt16.MinNegative, short.MinValue, "MyInt16Enum" };
                yield return new object[] { SampleEnumInt32.MinNegative, int.MinValue, "MyInt32Enum" };
                yield return new object[] { SampleEnumInt64.MinNegative, long.MinValue, "MyInt64Enum" };
                yield return new object[] { SampleEnumSByte.Max, sbyte.MaxValue, "MySByteEnum" };
                yield return new object[] { SampleEnumInt16.Max, short.MaxValue, "MyInt16Enum" };
                yield return new object[] { SampleEnumInt32.Max, int.MaxValue, "MyInt32Enum" };
                yield return new object[] { SampleEnumInt64.Max, long.MaxValue, "MyInt64Enum" };
                yield return new object[] { SampleEnumSByte.Zero, 0, "MySByteEnum" };
                yield return new object[] { SampleEnumInt16.Zero, 0, "MyInt16Enum" };
                yield return new object[] { SampleEnumInt32.Zero, 0, "MyInt32Enum" };
                yield return new object[] { SampleEnumInt64.Zero, 0, "MyInt64Enum" };
                yield return new object[] { SampleEnum.MinZero, 0, "MyEnum" };
                yield return new object[] { SampleEnumSByte.One, 1, "MySByteEnum" };
                yield return new object[] { SampleEnumInt16.One, 1, "MyInt16Enum" };
                yield return new object[] { SampleEnumInt32.One, 1, "MyInt32Enum" };
                yield return new object[] { SampleEnumInt64.One, 1, "MyInt64Enum" };
                yield return new object[] { SampleEnum.One, 1, "MyEnum" };
                yield return new object[] { SampleEnumSByte.Two, 2, "MySByteEnum" };
                yield return new object[] { SampleEnumInt16.Two, 2, "MyInt16Enum" };
                yield return new object[] { SampleEnumInt32.Two, 2, "MyInt32Enum" };
                yield return new object[] { SampleEnumInt64.Two, 2, "MyInt64Enum" };
                yield return new object[] { SampleEnum.Two, 2, "MyEnum" };
            }
        }

        public static IEnumerable<object[]> Parse_WithinRange_UnsignedAsString
        {
            get
            {
                yield return new object[] { typeof(SampleEnumUInt32), ByteMaxPlus1, "MyUInt32Enum" };
                yield return new object[] { typeof(SampleEnumUInt32), Int16MaxPlus1, "MyUInt32Enum" };
                yield return new object[] { typeof(SampleEnumUInt32), Int32MaxPlus1, "MyUInt32Enum" };
                yield return new object[] { typeof(SampleEnumUInt32), UInt16MaxPlus1, "MyUInt32Enum" };
                yield return new object[] { typeof(SampleEnumUInt64), ByteMaxPlus1, "MyUInt64Enum" };
                yield return new object[] { typeof(SampleEnumUInt64), Int16MaxPlus1, "MyUInt64Enum" };
                yield return new object[] { typeof(SampleEnumUInt64), Int32MaxPlus1, "MyUInt64Enum" };
                yield return new object[] { typeof(SampleEnumUInt64), Int64MaxPlus1, "MyUInt64Enum" };
                yield return new object[] { typeof(SampleEnumUInt64), UInt16MaxPlus1, "MyUInt64Enum" };
                yield return new object[] { typeof(SampleEnumUInt64), UInt32MaxPlus1, "MyUInt64Enum" };
            }
        }

        public static IEnumerable<object[]> Parse_WithinRange_Unsigned
        {
            get
            {
                yield return new object[] { SampleEnumByte.MinZero, byte.MinValue, "MyByteEnum" };
                yield return new object[] { SampleEnumUInt16.MinZero, ushort.MinValue, "MyUInt16Enum" };
                yield return new object[] { SampleEnumUInt32.MinZero, uint.MinValue, "MyUInt32Enum" };
                yield return new object[] { SampleEnumUInt64.MinZero, ulong.MinValue, "MyUInt64Enum" };
                yield return new object[] { SampleEnumByte.Max, byte.MaxValue, "MyByteEnum" };
                yield return new object[] { SampleEnumUInt16.Max, ushort.MaxValue, "MyUInt16Enum" };
                yield return new object[] { SampleEnumUInt32.Max, uint.MaxValue, "MyUInt32Enum" };
                yield return new object[] { SampleEnumUInt64.Max, ulong.MaxValue, "MyUInt64Enum" };
                yield return new object[] { SampleEnumByte.MinZero, 0, "MyByteEnum" };
                yield return new object[] { SampleEnumUInt16.MinZero, 0, "MyUInt16Enum" };
                yield return new object[] { SampleEnumUInt32.MinZero, 0, "MyUInt32Enum" };
                yield return new object[] { SampleEnumUInt64.MinZero, 0, "MyUInt64Enum" };
                yield return new object[] { SampleEnumByte.One, 1, "MyByteEnum" };
                yield return new object[] { SampleEnumUInt16.One, 1, "MyUInt16Enum" };
                yield return new object[] { SampleEnumUInt32.One, 1, "MyUInt32Enum" };
                yield return new object[] { SampleEnumUInt64.One, 1, "MyUInt64Enum" };
                yield return new object[] { SampleEnumByte.Two, 2, "MyByteEnum" };
                yield return new object[] { SampleEnumUInt16.Two, 2, "MyUInt16Enum" };
                yield return new object[] { SampleEnumUInt32.Two, 2, "MyUInt32Enum" };
                yield return new object[] { SampleEnumUInt64.Two, 2, "MyUInt64Enum" };
            }
        }

        public static IEnumerable<object[]> ToString_WithinRange
        {
            get
            {
                yield return new object[] { sbyte.MinValue, SampleEnumSByte.MinNegative };
                yield return new object[] { short.MinValue, SampleEnumInt16.MinNegative };
                yield return new object[] { int.MinValue, SampleEnumInt32.MinNegative };
                yield return new object[] { long.MinValue, SampleEnumInt64.MinNegative };
                yield return new object[] { 0, SampleEnum.MinZero };
                yield return new object[] { 0, SampleEnumByte.MinZero };
                yield return new object[] { 0, SampleEnumUInt16.MinZero };
                yield return new object[] { 0, SampleEnumUInt32.MinZero };
                yield return new object[] { 0, SampleEnumUInt64.MinZero };
                yield return new object[] { 0, SampleEnumSByte.Zero };
                yield return new object[] { 0, SampleEnumInt16.Zero };
                yield return new object[] { 0, SampleEnumInt32.Zero };
                yield return new object[] { 0, SampleEnumInt64.Zero };
                yield return new object[] { 2, SampleEnum.Two };
                yield return new object[] { 2, SampleEnumByte.Two };
                yield return new object[] { 2, SampleEnumSByte.Two };
                yield return new object[] { 2, SampleEnumInt16.Two };
                yield return new object[] { 2, SampleEnumUInt16.Two };
                yield return new object[] { 2, SampleEnumInt32.Two };
                yield return new object[] { 2, SampleEnumUInt32.Two };
                yield return new object[] { 2, SampleEnumInt64.Two };
                yield return new object[] { 2, SampleEnumUInt64.Two };
                yield return new object[] { byte.MaxValue, SampleEnumByte.Max };
                yield return new object[] { sbyte.MaxValue, SampleEnumSByte.Max };
                yield return new object[] { short.MaxValue, SampleEnumInt16.Max };
                yield return new object[] { ushort.MaxValue, SampleEnumUInt16.Max };
                yield return new object[] { int.MaxValue, SampleEnumInt32.Max };
                yield return new object[] { uint.MaxValue, SampleEnumUInt32.Max };
                yield return new object[] { long.MaxValue, SampleEnumInt64.Max };
                yield return new object[] { ulong.MaxValue, SampleEnumUInt64.Max };
            }
        }

        public static IEnumerable<object[]> ToString_ExceedMaxCapacity
        {
            get
            {
                // The rationale for setting macCapacity:
                // e.g. for byte, including 0, we have byte.MaxValue + 1 numbers that can be represented by byte
                yield return new object[] { 1, ByteMaxPlus1, typeof(SampleEnumByte), SampleEnumByte.MinZero };
                yield return new object[] { 2, ByteMaxPlus1, typeof(SampleEnumByte), SampleEnumByte.MinZero };
                yield return new object[] { 3, ByteMaxPlus1, typeof(SampleEnumByte), SampleEnumByte.MinZero };
                yield return new object[] { 4, ByteMaxPlus1, typeof(SampleEnumByte), SampleEnumByte.MinZero };
                yield return new object[] { 1, UInt16MaxPlus1, typeof(SampleEnumUInt16), SampleEnumUInt16.MinZero };
                yield return new object[] { 2, UInt16MaxPlus1, typeof(SampleEnumUInt16), SampleEnumUInt16.MinZero };
                yield return new object[] { 3, UInt16MaxPlus1, typeof(SampleEnumUInt16), SampleEnumUInt16.MinZero };
                yield return new object[] { 4, UInt16MaxPlus1, typeof(SampleEnumUInt16), SampleEnumUInt16.MinZero };
                yield return new object[] { 1, UInt32MaxPlus1, typeof(SampleEnumUInt32), SampleEnumUInt32.MinZero };
                yield return new object[] { 2, UInt32MaxPlus1, typeof(SampleEnumUInt32), SampleEnumUInt32.MinZero };
                yield return new object[] { 3, UInt32MaxPlus1, typeof(SampleEnumUInt32), SampleEnumUInt32.MinZero };
                yield return new object[] { 4, UInt32MaxPlus1, typeof(SampleEnumUInt32), SampleEnumUInt32.MinZero };
                yield return new object[] { 1, SByteMaxPlus1, typeof(SampleEnumSByte), SampleEnumSByte.MinNegative };
                yield return new object[] { 2, SByteMaxPlus1, typeof(SampleEnumSByte), SampleEnumSByte.Zero };
                yield return new object[] { 3, SByteMaxPlus1, typeof(SampleEnumSByte), SampleEnumSByte.MinNegative };
                yield return new object[] { 4, SByteMaxPlus1, typeof(SampleEnumSByte), SampleEnumSByte.Zero };
                yield return new object[] { 1, Int16MaxPlus1, typeof(SampleEnumInt16), SampleEnumInt16.MinNegative };
                yield return new object[] { 2, Int16MaxPlus1, typeof(SampleEnumInt16), SampleEnumInt16.Zero };
                yield return new object[] { 3, Int16MaxPlus1, typeof(SampleEnumInt16), SampleEnumInt16.MinNegative };
                yield return new object[] { 4, Int16MaxPlus1, typeof(SampleEnumInt16), SampleEnumInt16.Zero };
                yield return new object[] { 1, Int32MaxPlus1, typeof(SampleEnumInt32), SampleEnumInt32.MinNegative };
                yield return new object[] { 2, Int32MaxPlus1, typeof(SampleEnumInt32), SampleEnumInt32.Zero };
                yield return new object[] { 3, Int32MaxPlus1, typeof(SampleEnumInt32), SampleEnumInt32.MinNegative };
                yield return new object[] { 4, Int32MaxPlus1, typeof(SampleEnumInt32), SampleEnumInt32.Zero };
            }
        }
    }
}
