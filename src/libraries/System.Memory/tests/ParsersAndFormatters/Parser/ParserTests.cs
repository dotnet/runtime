// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Buffers.Text.Tests
{
    public static partial class ParserTests
    {
        [Theory]
        [MemberData(nameof(TestData.BooleanParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserBoolean(ParserTestData<bool> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.SByteParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserSByte(ParserTestData<sbyte> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.ByteParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserByte(ParserTestData<byte> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.Int16ParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserInt16(ParserTestData<short> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.UInt16ParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserUInt16(ParserTestData<ushort> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.Int32ParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserInt32(ParserTestData<int> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.UInt32ParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserUInt32(ParserTestData<uint> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.Int64ParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserInt64(ParserTestData<long> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.UInt64ParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserUInt64(ParserTestData<ulong> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.DecimalParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserDecimal(ParserTestData<decimal> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.DoubleParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserDouble(ParserTestData<double> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.SingleParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserSingle(ParserTestData<float> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.GuidParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserGuid(ParserTestData<Guid> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.DateTimeParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserDateTime(ParserTestData<DateTime> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.DateTimeOffsetParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserDateTimeOffset(ParserTestData<DateTimeOffset> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [MemberData(nameof(TestData.TimeSpanParserTheoryData), MemberType = typeof(TestData))]
        public static void TestParserTimeSpan(ParserTestData<TimeSpan> testData)
        {
            ValidateParser(testData);
        }

        [Theory]
        [InlineData("9999-12-31T23:59:59.9999999", 'O')]
        [InlineData("12/31/9999 23:59:59", 'G')]
        public static void TestParserDateEndOfTime(string text, char formatSymbol)
        {
            // In timezones with a negative UTC offset, these inputs will blow up DateTimeOffset's constructor
            // due to the overflow associated with adding the UTC offset. Make sure the call doesn't
            // surface the ArgumentOutOfRangeException.
            ReadOnlySpan<byte> utf8Text = text.ToUtf8Span();
            Utf8Parser.TryParse(utf8Text, out DateTimeOffset dto, out int bytesConsumed, formatSymbol);
        }

        [Theory]
        [MemberData(nameof(TestData.IntegerTypesTheoryData), MemberType = typeof(TestData))]
        public static void FakeTestParserIntegerN(Type integerType)
        {
            //
            // [ActiveIssue("https://github.com/dotnet/corefx/issues/24986 - UTF8Parser parsing integers with 'N' format not implemented.")]
            //
            // This "test" may look ludicrous but it serves two useful purposes:
            //
            //  - It maintains Utf8Parser code coverage at 100% so that endless dev cycles aren't wasted drilling down into it to inspect for code coverage regressions.
            //
            //  - As a guide to enabling the 'N' tests when the parsing is implemented.
            //
            try
            {
                TryParseUtf8(integerType, Array.Empty<byte>(), out _, out _, 'N');
                Assert.False(true,
                    $"Thank you for implementing the TryParse 'N' format. You can now disable this test and change {nameof(TestData.IsParsingImplemented)}() so it no longer suppresses 'N' testing.");
            }
            catch (NotImplementedException)
            {
            }
        }
    }
}

