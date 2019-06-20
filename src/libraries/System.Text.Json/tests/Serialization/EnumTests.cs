// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class EnumTests
    {
        private static readonly string s_jsonStringEnum =
                @"{" +
                @"""MyEnum"" : ""Two""" +
                @"}";

        private static readonly string s_jsonIntEnum =
                @"{" +
                @"""MyEnum"" : 2" +
                @"}";

        private static readonly string s_jsonInt16Enum =
                @"{" +
                @"""MyInt16Enum"" : 2" +
                @"}";

        private static readonly string s_jsonInt64EnumMin =
                @"{" +
                @"""MyInt64Enum"" : " + long.MinValue +
                @"}";

        private static readonly string s_jsonInt64EnumMax =
                @"{" +
                @"""MyInt64Enum"" : " + long.MaxValue +
                @"}";

        private static readonly string s_jsonUInt64EnumMax =
                @"{" +
                @"""MyUInt64Enum"" : " + ulong.MaxValue +
                @"}";

        private static readonly string s_jsonUInt16EnumMax =
                @"{" +
                @"""MyUInt16Enum"" : " + ushort.MaxValue +
                @"}";

        private static readonly string s_jsonByteEnum =
                @"{" +
                @"""MyByteEnum"" : " + byte.MaxValue +
                @"}";

        private static readonly string s_jsonSByteEnum =
        @"{" +
        @"""MySByteEnum"" : " + sbyte.MaxValue +
        @"}";

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

        private const string Int16MaxPlus1 = "32768"; // int.MaxValue + 1;
        private const string Int16MinMinus1 = "-32769"; // int.MinValue - 1

        private const string ByteMaxPlus1 = "256"; // byte.MaxValue + 1;
        private const string MinusByteMaxMinus1 = "-257"; // -byte.MaxValue - 1

        [Fact]
        public static void Parse_MinusByteMaxMinus1_Throws()
        {
            Assert.Equal(int.Parse(MinusByteMaxMinus1), (int)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {MinusByteMaxMinus1} }}").MyEnum);
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {MinusByteMaxMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {MinusByteMaxMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {MinusByteMaxMinus1} }}"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{MinusByteMaxMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{MinusByteMaxMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{MinusByteMaxMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{MinusByteMaxMinus1}\" }}"));
        }

        [Fact]
        public static void Parse_ByteMaxPlus1_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {ByteMaxPlus1} }}"));
            Assert.Equal(int.Parse(ByteMaxPlus1), (int)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {ByteMaxPlus1} }}").MyEnum);
            Assert.Equal(int.Parse(ByteMaxPlus1), (int)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {ByteMaxPlus1} }}").MyUInt32Enum);
            Assert.Equal(int.Parse(ByteMaxPlus1), (int)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {ByteMaxPlus1} }}").MyUInt64Enum);

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{ByteMaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{ByteMaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{ByteMaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{ByteMaxPlus1}\" }}"));
        }

        [Fact]
        public static void Parse_Int16MinMinus1_Throws()
        {
            Assert.Equal(int.Parse(Int16MinMinus1), (int)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {Int16MinMinus1} }}").MyEnum);
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {Int16MinMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {Int16MinMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {Int16MinMinus1} }}"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{Int16MinMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{Int16MinMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{Int16MinMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{Int16MinMinus1}\" }}"));
        }

        [Fact]
        public static void Parse_Int16MaxPlus1_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {Int16MaxPlus1} }}"));
            Assert.Equal(int.Parse(Int16MaxPlus1), (int)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {Int16MaxPlus1} }}").MyEnum);
            Assert.Equal(int.Parse(Int16MaxPlus1), (int)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {Int16MaxPlus1} }}").MyUInt32Enum);
            Assert.Equal(int.Parse(Int16MaxPlus1), (int)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {Int16MaxPlus1} }}").MyUInt64Enum);

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{Int16MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{Int16MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{Int16MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{Int16MaxPlus1}\" }}"));
        }

        [Fact]
        public static void Parse_MinusUInt16MaxMinus1_Throws()
        {
            Assert.Equal(int.Parse(MinusUInt16MaxMinus1), (int)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {MinusUInt16MaxMinus1} }}").MyEnum);
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {MinusUInt16MaxMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {MinusUInt16MaxMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {MinusUInt16MaxMinus1} }}"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{MinusUInt16MaxMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{MinusUInt16MaxMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{MinusUInt16MaxMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{MinusUInt16MaxMinus1}\" }}"));
        }

        [Fact]
        public static void Parse_UInt16MaxPlus1_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {UInt16MaxPlus1} }}"));
            Assert.Equal(int.Parse(UInt16MaxPlus1), (int)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {UInt16MaxPlus1} }}").MyEnum);
            Assert.Equal(int.Parse(UInt16MaxPlus1), (int)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {UInt16MaxPlus1} }}").MyUInt32Enum);
            Assert.Equal(int.Parse(UInt16MaxPlus1), (int)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {UInt16MaxPlus1} }}").MyUInt64Enum);

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{UInt16MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{UInt16MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{UInt16MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{UInt16MaxPlus1}\" }}"));
        }

        [Fact]
        public static void Parse_Int32MinMinus1_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {Int32MinMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {Int32MinMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {Int32MinMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {Int32MinMinus1} }}"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{Int32MinMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{Int32MinMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{Int32MinMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{Int32MinMinus1}\" }}"));
        }

        [Fact]
        public static void Parse_Int32MaxPlus1_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {Int32MaxPlus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {Int32MaxPlus1} }}"));
            Assert.Equal(ulong.Parse(Int32MaxPlus1), (ulong)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {Int32MaxPlus1} }}").MyUInt32Enum);
            Assert.Equal(ulong.Parse(Int32MaxPlus1), (ulong)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {Int32MaxPlus1} }}").MyUInt64Enum);

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{Int32MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{Int32MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{Int32MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{Int32MaxPlus1}\" }}"));
        }

        [Fact]
        public static void Parse_MinusUInt32MaxMinus1_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {MinusUInt32MaxMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {MinusUInt32MaxMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {MinusUInt32MaxMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {MinusUInt32MaxMinus1} }}"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{Int64MinMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{Int64MinMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{Int64MinMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{Int64MinMinus1}\" }}"));
        }

        [Fact]
        public static void Parse_UInt32MaxPlus1_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {UInt32MaxPlus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {UInt32MaxPlus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {UInt32MaxPlus1} }}"));
            Assert.Equal(ulong.Parse(UInt32MaxPlus1), (ulong)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {UInt32MaxPlus1} }}").MyUInt64Enum);

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{UInt32MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{UInt32MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{UInt32MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{UInt32MaxPlus1}\" }}"));
        }

        [Fact]
        public static void Parse_Int64MinMinus1_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {Int64MinMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {Int64MinMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {Int64MinMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {Int64MinMinus1} }}"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{Int64MinMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{Int64MinMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{Int64MinMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{Int64MinMinus1}\" }}"));
        }

        [Fact]
        public static void Parse_Int64MaxPlus1_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {Int64MaxPlus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {Int64MaxPlus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {Int64MaxPlus1} }}"));
            Assert.Equal(ulong.Parse(Int64MaxPlus1), (ulong)JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {Int64MaxPlus1} }}").MyUInt64Enum);

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{Int64MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{Int64MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{Int64MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{Int64MaxPlus1}\" }}"));
        }

        [Fact]
        public static void Parse_MinusUInt64MaxMinus1_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {MinusUInt64MaxMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {MinusUInt64MaxMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {MinusUInt64MaxMinus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {MinusUInt64MaxMinus1} }}"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{MinusUInt64MaxMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{MinusUInt64MaxMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{MinusUInt64MaxMinus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{MinusUInt64MaxMinus1}\" }}"));
        }

        [Fact]
        public static void Parse_UInt64MaxPlus1_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {UInt64MaxPlus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {UInt64MaxPlus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {UInt64MaxPlus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {UInt64MaxPlus1} }}"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{UInt64MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{UInt64MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{UInt64MaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{UInt64MaxPlus1}\" }}"));
        }

        [Fact]
        public static void Parse_MaxValues_QuotedVsNotQuoted_QuotedThrows()
        {
            string json = $"{{ \"MyByteEnum\" : {byte.MaxValue}, \"MyUInt32Enum\" : {UInt32.MaxValue}, \"MyUInt64Enum\" : {ulong.MaxValue} }}";
            SimpleTestClass result = JsonSerializer.Parse<SimpleTestClass>(json);
            Assert.Equal((SampleEnumByte)byte.MaxValue, result.MyByteEnum);
            Assert.Equal((SampleEnumUInt32)UInt32.MaxValue, result.MyUInt32Enum);
            Assert.Equal((SampleEnumUInt64)ulong.MaxValue, result.MyUInt64Enum);

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {ulong.MaxValue} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {ulong.MaxValue} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {ulong.MaxValue} }}"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{ulong.MaxValue}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{ulong.MaxValue}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{ulong.MaxValue}\" }}"));
        }

        [Fact]
        public static void Parse_MaxValuePlus1_QuotedVsNotQuoted_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {ByteMaxPlus1}, \"MyUInt32Enum\" : {UInt32MaxPlus1} }}"));

            // Quoted throws
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{ByteMaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{UInt32MaxPlus1}\" }}"));
        }

        [Fact]
        public static void Parse_Negative1_QuotedVsUnquoted_QuotedThrows()
        {
            string json = "{ \"MyByteEnum\" : -1, \"MyUInt32Enum\" : -1 }";

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>(json));
            
            // Quoted throws
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>("{ \"MyByteEnum\" : \"-1\" }"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>("{ \"MyUInt32Enum\" : \"-1\" }"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>("{ \"MyUInt64Enum\" : \"-1\" }"));
        }

        [Theory]
        [InlineData("{ \"MyUInt64Enum\" : -1 }")]
        [InlineData("{ \"MyUInt64Enum\" : -1, \"MyUInt32Enum\" : -1 }")]
        [InlineData("{ \"MyUInt64Enum\" : -1, \"MyUInt32Enum\" : -1, \"MyByteEnum\" : -1 }")]
        public static void Parse_Negative1ForUInt64Enum_Throw(string json)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>(json));
        }

        [Theory]
        [InlineData((ulong)byte.MaxValue + 1, (ulong)UInt32.MaxValue + 1, (SampleEnumByte)0, (SampleEnumUInt32)0)]
        [InlineData((ulong)byte.MaxValue + 2, (ulong)UInt32.MaxValue + 2, (SampleEnumByte)1, (SampleEnumUInt32)1)]
        [InlineData((ulong)byte.MaxValue + 13, (ulong)UInt32.MaxValue + 13, (SampleEnumByte)12, (SampleEnumUInt32)12)]
        [InlineData((ulong)byte.MaxValue * 2, (ulong)UInt32.MaxValue * 2, (SampleEnumByte)byte.MaxValue - 1, (SampleEnumUInt32)UInt32.MaxValue - 1)]
        public static void Parse_Ulong_InvalidNumber_Throw(ulong i, ulong j, SampleEnumByte e1, SampleEnumUInt32 e2)
        {
            string json = $"{{ \"MyByteEnum\" : {i}, \"MyUInt32Enum\" : {j} }}";
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>(json));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{i}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{j}\" }}"));
        }

        [Fact]
        public static void EnumAsStringFail()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>(s_jsonStringEnum));
        }

        [Fact]
        public static void EnumAsInt()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(s_jsonIntEnum);
            Assert.Equal(SampleEnum.Two, obj.MyEnum);
        }

        [Fact]
        public static void EnumAsInt16()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(s_jsonInt16Enum);
            Assert.Equal(SampleEnumInt16.Two, obj.MyInt16Enum);
        }

        [Fact]
        public static void EnumAsInt64Min()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(s_jsonInt64EnumMin);
            Assert.Equal(SampleEnumInt64.MinNegative, obj.MyInt64Enum);
        }

        [Fact]
        public static void EnumAsInt64Max()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(s_jsonInt64EnumMax);
            Assert.Equal(SampleEnumInt64.Max, obj.MyInt64Enum);
        }

        [Fact]
        public static void EnumAsUInt64Max()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(s_jsonUInt64EnumMax);
            Assert.Equal(SampleEnumUInt64.Max, obj.MyUInt64Enum);
        }

        [Fact]
        public static void EnumAsUInt16Max()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(s_jsonUInt16EnumMax);
            Assert.Equal(SampleEnumUInt16.Max, obj.MyUInt16Enum);
        }

        [Fact]
        public static void EnumAsByteMax()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(s_jsonByteEnum);
            Assert.Equal(SampleEnumByte.Max, obj.MyByteEnum);
        }

        [Fact]
        public static void EnumAsSByteMax()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(s_jsonSByteEnum);
            Assert.Equal(SampleEnumSByte.Max, obj.MySByteEnum);
        }

        [Theory]
        [InlineData(sbyte.MinValue, SampleEnumSByte.MinNegative)]
        [InlineData(short.MinValue, SampleEnumInt16.MinNegative)]
        [InlineData(int.MinValue, SampleEnumInt32.MinNegative)]
        [InlineData(long.MinValue, SampleEnumInt64.MinNegative)]
        [InlineData(0, SampleEnum.MinZero)]
        [InlineData(0, SampleEnumByte.MinZero)]
        [InlineData(0, SampleEnumUInt16.MinZero)]
        [InlineData(0, SampleEnumUInt32.MinZero)]
        [InlineData(0, SampleEnumUInt64.MinZero)]
        [InlineData(0, SampleEnumSByte.Zero)]
        [InlineData(0, SampleEnumInt16.Zero)]
        [InlineData(0, SampleEnumInt32.Zero)]
        [InlineData(0, SampleEnumInt64.Zero)]
        [InlineData(1, SampleEnum.One)]
        [InlineData(1, SampleEnumByte.One)]
        [InlineData(1, SampleEnumSByte.One)]
        [InlineData(1, SampleEnumInt16.One)]
        [InlineData(1, SampleEnumUInt16.One)]
        [InlineData(1, SampleEnumInt32.One)]
        [InlineData(1, SampleEnumUInt32.One)]
        [InlineData(1, SampleEnumInt64.One)]
        [InlineData(1, SampleEnumUInt64.One)]
        [InlineData(2, SampleEnum.Two)]
        [InlineData(2, SampleEnumByte.Two)]
        [InlineData(2, SampleEnumSByte.Two)]
        [InlineData(2, SampleEnumInt16.Two)]
        [InlineData(2, SampleEnumUInt16.Two)]
        [InlineData(2, SampleEnumInt32.Two)]
        [InlineData(2, SampleEnumUInt32.Two)]
        [InlineData(2, SampleEnumInt64.Two)]
        [InlineData(2, SampleEnumUInt64.Two)]
        [InlineData(byte.MaxValue, SampleEnumByte.Max)]
        [InlineData(sbyte.MaxValue, SampleEnumSByte.Max)]
        [InlineData(short.MaxValue, SampleEnumInt16.Max)]
        [InlineData(ushort.MaxValue, SampleEnumUInt16.Max)]
        [InlineData(int.MaxValue, SampleEnumInt32.Max)]
        [InlineData(uint.MaxValue, SampleEnumUInt32.Max)]
        [InlineData(long.MaxValue, SampleEnumInt64.Max)]
        [InlineData(ulong.MaxValue, SampleEnumUInt64.Max)]
        public static void ToString_Max(object expected, object enumValue)
        {
            string json = JsonSerializer.ToString(enumValue);
            Assert.Equal(expected.ToString(), json);
        }

        [Theory]
        [InlineData(1, (long)byte.MaxValue + 1, typeof(SampleEnumByte), SampleEnumByte.MinZero)]
        [InlineData(2, (long)byte.MaxValue + 1, typeof(SampleEnumByte), SampleEnumByte.MinZero)]
        [InlineData(3, (long)byte.MaxValue + 1, typeof(SampleEnumByte), SampleEnumByte.MinZero)]
        [InlineData(4, (long)byte.MaxValue + 1, typeof(SampleEnumByte), SampleEnumByte.MinZero)]
        [InlineData(1, (long)UInt16.MaxValue + 1, typeof(SampleEnumUInt16), SampleEnumUInt16.MinZero)]
        [InlineData(2, (long)UInt16.MaxValue + 1, typeof(SampleEnumUInt16), SampleEnumUInt16.MinZero)]
        [InlineData(3, (long)UInt16.MaxValue + 1, typeof(SampleEnumUInt16), SampleEnumUInt16.MinZero)]
        [InlineData(4, (long)UInt16.MaxValue + 1, typeof(SampleEnumUInt16), SampleEnumUInt16.MinZero)]
        [InlineData(1, (long)UInt32.MaxValue + 1, typeof(SampleEnumUInt32), SampleEnumUInt32.MinZero)]
        [InlineData(2, (long)UInt32.MaxValue + 1, typeof(SampleEnumUInt32), SampleEnumUInt32.MinZero)]
        [InlineData(3, (long)UInt32.MaxValue + 1, typeof(SampleEnumUInt32), SampleEnumUInt32.MinZero)]
        [InlineData(4, (long)UInt32.MaxValue + 1, typeof(SampleEnumUInt32), SampleEnumUInt32.MinZero)]
        [InlineData(1, (long)sbyte.MaxValue + 1, typeof(SampleEnumSByte), SampleEnumSByte.MinNegative)]
        [InlineData(2, (long)sbyte.MaxValue + 1, typeof(SampleEnumSByte), SampleEnumSByte.Zero)]
        [InlineData(3, (long)sbyte.MaxValue + 1, typeof(SampleEnumSByte), SampleEnumSByte.MinNegative)]
        [InlineData(4, (long)sbyte.MaxValue + 1, typeof(SampleEnumSByte), SampleEnumSByte.Zero)]
        [InlineData(1, (long)Int16.MaxValue + 1, typeof(SampleEnumInt16), SampleEnumInt16.MinNegative)]
        [InlineData(2, (long)Int16.MaxValue + 1, typeof(SampleEnumInt16), SampleEnumInt16.Zero)]
        [InlineData(3, (long)Int16.MaxValue + 1, typeof(SampleEnumInt16), SampleEnumInt16.MinNegative)]
        [InlineData(4, (long)Int16.MaxValue + 1, typeof(SampleEnumInt16), SampleEnumInt16.Zero)]
        [InlineData(1, (long)Int32.MaxValue + 1, typeof(SampleEnumInt32), SampleEnumInt32.MinNegative)]
        [InlineData(2, (long)Int32.MaxValue + 1, typeof(SampleEnumInt32), SampleEnumInt32.Zero)]
        [InlineData(3, (long)Int32.MaxValue + 1, typeof(SampleEnumInt32), SampleEnumInt32.MinNegative)]
        [InlineData(4, (long)Int32.MaxValue + 1, typeof(SampleEnumInt32), SampleEnumInt32.Zero)]
        public static void ToString_ExceedMaxCapacity_ResetsBackToMinimum(int timesOverflow, long maxCapacity, Type type, long expected)
        {
            object enumValue = Enum.ToObject(type, maxCapacity * timesOverflow);
            string json = JsonSerializer.ToString(enumValue);
            string expectedString = expected.ToString();
            Assert.Equal(expectedString, json);
        }
    }
}
