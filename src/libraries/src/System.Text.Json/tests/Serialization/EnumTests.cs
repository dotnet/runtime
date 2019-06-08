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

        private static readonly string s_jsonByteEnum =
                @"{" +
                @"""MyByteEnum"" : " + byte.MaxValue +
                @"}";

        private const string UlongMaxPlus1 = "18446744073709551616"; // ulong.MaxValue + 1;

        [Fact]
        public static void Parse_UlongMaxPlus1_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {UlongMaxPlus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {UlongMaxPlus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {UlongMaxPlus1} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : {UlongMaxPlus1} }}"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{UlongMaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{UlongMaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{UlongMaxPlus1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt64Enum\" : \"{UlongMaxPlus1}\" }}"));
        }

        [Fact]
        public static void Parse_MaxValues_QuotedVsNotQuoted_QuotedThrows()
        {
            string json = $"{{ \"MyByteEnum\" : {byte.MaxValue}, \"MyUInt32Enum\" : {UInt32.MaxValue}, \"MyUInt64Enum\" : {ulong.MaxValue} }}";
            SimpleTestClass result = JsonSerializer.Parse<SimpleTestClass>(json);
            Assert.Equal((SampleByteEnum)byte.MaxValue, result.MyByteEnum);
            Assert.Equal((SampleUInt32Enum)UInt32.MaxValue, result.MyUInt32Enum);
            Assert.Equal((SampleUInt64Enum)ulong.MaxValue, result.MyUInt64Enum);

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : {ulong.MaxValue} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : {ulong.MaxValue} }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : {ulong.MaxValue} }}"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyEnum\" : \"{ulong.MaxValue}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{ulong.MaxValue}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{ulong.MaxValue}\" }}"));
        }

        [Fact]
        public static void Parse_MaxValuePlus1_QuotedVsNotQuoted_QuotedThrows()
        {
            SimpleTestClass result = JsonSerializer.Parse<SimpleTestClass>(
                $"{{ \"MyByteEnum\" : {(ulong)byte.MaxValue + 1}, \"MyUInt32Enum\" : {(ulong)UInt32.MaxValue + 1} }}");
            Assert.Equal((SampleByteEnum)0, result.MyByteEnum);
            Assert.Equal((SampleUInt32Enum)0, result.MyUInt32Enum);

            // Quoted throws
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyByteEnum\" : \"{(ulong)byte.MaxValue + 1}\" }}"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>($"{{ \"MyUInt32Enum\" : \"{(ulong)UInt32.MaxValue + 1}\" }}"));
        }

        [Fact]
        public static void Parse_Negative1_QuotedVsUnquoted_QuotedThrows()
        {
            string json = "{ \"MyByteEnum\" : -1, \"MyUInt32Enum\" : -1 }";
            SimpleTestClass result = JsonSerializer.Parse<SimpleTestClass>(json);
            Assert.Equal((SampleByteEnum)byte.MaxValue, result.MyByteEnum);
            Assert.Equal((SampleUInt32Enum)UInt32.MaxValue, result.MyUInt32Enum);

            // Quoted throws
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>("{ \"MyByteEnum\" : \"-1\" }"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>("{ \"MyUInt32Enum\" : \"-1\" }"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>("{ \"MyUInt64Enum\" : \"-1\" }"));
        }

        [Theory]
        [InlineData("{ \"MyUInt64Enum\" : -1 }")]
        [InlineData("{ \"MyUInt64Enum\" : -1, \"MyUInt32Enum\" : -1 }")]
        [InlineData("{ \"MyUInt64Enum\" : -1, \"MyUInt32Enum\" : -1, \"MyByteEnum\" : -1 }")]
        [ActiveIssue(38363)]
        public static void Parse_Negative1ForUInt64Enum_ShouldNotThrow(string json)
        {
            SimpleTestClass result = JsonSerializer.Parse<SimpleTestClass>(json);
            Assert.Equal((SampleUInt64Enum)UInt64.MaxValue, result.MyUInt64Enum);
        }

        [Theory]
        [InlineData((ulong)byte.MaxValue + 1, (ulong)UInt32.MaxValue + 1, (SampleByteEnum)0, (SampleUInt32Enum)0)]
        [InlineData((ulong)byte.MaxValue + 2, (ulong)UInt32.MaxValue + 2, (SampleByteEnum)1, (SampleUInt32Enum)1)]
        [InlineData((ulong)byte.MaxValue + 13, (ulong)UInt32.MaxValue + 13, (SampleByteEnum)12, (SampleUInt32Enum)12)]
        [InlineData((ulong)byte.MaxValue * 2, (ulong)UInt32.MaxValue * 2, (SampleByteEnum)byte.MaxValue - 1, (SampleUInt32Enum)UInt32.MaxValue - 1)]
        public static void Parse_Ulong_JsonWithAcceptableInvalidNumber_Success(ulong i, ulong j, SampleByteEnum e1, SampleUInt32Enum e2)
        {
            string json = $"{{ \"MyByteEnum\" : {i}, \"MyUInt32Enum\" : {j} }}";
            SimpleTestClass result = JsonSerializer.Parse<SimpleTestClass>(json);
            Assert.Equal(e1, result.MyByteEnum);
            Assert.Equal(e2, result.MyUInt32Enum);

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
        public static void EnumAsInt64Min()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(s_jsonInt64EnumMin);
            Assert.Equal(SampleInt64Enum.Min, obj.MyInt64Enum);
        }

        [Fact]
        public static void EnumAsInt64Max()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(s_jsonInt64EnumMax);
            Assert.Equal(SampleInt64Enum.Max, obj.MyInt64Enum);
        }

        [Fact]
        public static void EnumAsUInt64Max()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(s_jsonUInt64EnumMax);
            Assert.Equal(SampleUInt64Enum.Max, obj.MyUInt64Enum);
        }

        [Fact]
        public static void EnumAsByteMax()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(s_jsonByteEnum);
            Assert.Equal(SampleByteEnum.Max, obj.MyByteEnum);
        }
    }
}
