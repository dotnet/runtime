// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class Int32NumberHeaderParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            Int32NumberHeaderParser parser = Int32NumberHeaderParser.Parser;
            Assert.False(parser.SupportsMultipleValues, "SupportsMultipleValues");
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void Parse_ValidValue_ReturnsLongValue()
        {
            // This test verifies that Parse() correctly calls TryParse().
            Int32NumberHeaderParser parser = Int32NumberHeaderParser.Parser;
            int index = 2;
            Assert.Equal((int)15, parser.ParseValue("  15", null, ref index));
            Assert.Equal(4, index);

            index = 0;
            Assert.Equal((int)15, parser.ParseValue("  15", null, ref index));
            Assert.Equal(4, index);
        }

        [Fact]
        public void Parse_InvalidValue_Throw()
        {
            // This test verifies that Parse() correctly calls TryParse().
            Int32NumberHeaderParser parser = Int32NumberHeaderParser.Parser;
            int index = 0;

            Assert.Throws<FormatException>(() => { parser.ParseValue("a", null, ref index); });
        }

        [Fact]
        public void Parse_NullValue_Throw()
        {
            Int32NumberHeaderParser parser = Int32NumberHeaderParser.Parser;
            int index = 0;

            Assert.Throws<FormatException>(() => { parser.ParseValue(null, null, ref index); });
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParsedValue("1234567890", 0, 1234567890, 10);
            CheckValidParsedValue("0", 0, 0, 1);
            CheckValidParsedValue("000015", 0, 15, 6);
            CheckValidParsedValue(" 987", 0, 987, 4);
            CheckValidParsedValue("987 ", 0, 987, 4);
            CheckValidParsedValue("a456", 1, 456, 4);
            CheckValidParsedValue("a456 ", 1, 456, 5);
            CheckValidParsedValue("2147483647", 0, int.MaxValue, 10);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("", 0);
            CheckInvalidParsedValue("  ", 2);
            CheckInvalidParsedValue("a", 0);
            CheckInvalidParsedValue(".123", 0);
            CheckInvalidParsedValue(".", 0);
            CheckInvalidParsedValue("12a", 0);
            CheckInvalidParsedValue("a12b", 1);
            CheckInvalidParsedValue("123 1", 0);
            CheckInvalidParsedValue("123.1", 0);
            CheckInvalidParsedValue(" 123 1", 0);
            CheckInvalidParsedValue("a 123 1", 1);
            CheckInvalidParsedValue("a 123 1 ", 1);
            CheckInvalidParsedValue("-123.1", 0);
            CheckInvalidParsedValue("-123", 0);
            CheckInvalidParsedValue("123456789012345678901234567890", 0); // value >> Int32.MaxValue
            CheckInvalidParsedValue("2147483648", 0); // value = Int32.MaxValue + 1
            CheckInvalidParsedValue(" 123 \t\r\n ", 0);
            CheckInvalidParsedValue("a 5 \r\n ", 1);
            CheckInvalidParsedValue("1\n", 0);
            CheckInvalidParsedValue("2\r", 0);
            CheckInvalidParsedValue("3\r\n ", 0);
        }

        [Fact]
        public void ToString_UseDifferentValues_MatchExpectation()
        {
            Int32NumberHeaderParser parser = Int32NumberHeaderParser.Parser;

            Assert.Equal("1234567890", parser.ToString(1234567890));
            Assert.Equal("0", parser.ToString(0));
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, int expectedResult, int expectedIndex)
        {
            Int32NumberHeaderParser parser = Int32NumberHeaderParser.Parser;
            object result = 0;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false: {0}", input));
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedIndex, startIndex);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            Int32NumberHeaderParser parser = Int32NumberHeaderParser.Parser;
            object result = 0;
            int newIndex = startIndex;
            Assert.False(parser.TryParseValue(input, null, ref newIndex, out result),
                string.Format("TryParse returned true: {0}", input));
            Assert.Null(result);
            Assert.Equal(startIndex, newIndex);
        }
        #endregion
    }
}
