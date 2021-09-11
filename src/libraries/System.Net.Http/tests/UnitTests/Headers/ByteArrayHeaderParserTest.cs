// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class ByteArrayHeaderParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            ByteArrayHeaderParser parser = ByteArrayHeaderParser.Parser;
            Assert.False(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void Parse_NullValue_Throw()
        {
            ByteArrayHeaderParser parser = ByteArrayHeaderParser.Parser;
            int index = 0;
            Assert.Throws<FormatException>(() => { parser.ParseValue(null, null, ref index); });
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParsedValue("X  A/b+CQ== ", 1, new byte[] { 3, 246, 254, 9 }, 12);
            CheckValidParsedValue("AQ==", 0, new byte[] { 1 }, 4);

            // Note that Convert.FromBase64String() is tolerant with whitespace characters in the middle of the Base64
            // string:
            CheckValidParsedValue(" AbCdE fGhI  jKl+/Mn \r\n \t", 0,
                new byte[] { 1, 176, 157, 17, 241, 161, 34, 50, 165, 251, 243, 39 }, 25);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("", 0);
            CheckInvalidParsedValue("  ", 2);
            CheckInvalidParsedValue("a", 0);
            CheckInvalidParsedValue("AQ", 0);
            CheckInvalidParsedValue("AQ== X", 0);
            CheckInvalidParsedValue("AQ==,", 0);
            CheckInvalidParsedValue("AQ==A", 0);
            CheckInvalidParsedValue("AQ== ,", 0);
            CheckInvalidParsedValue(", AQ==", 0);
            CheckInvalidParsedValue(" ,AQ==", 0);
            CheckInvalidParsedValue("=", 0);
        }

        [Fact]
        public void ToString_UseDifferentValues_MatchExpectation()
        {
            ByteArrayHeaderParser parser = ByteArrayHeaderParser.Parser;
            Assert.Equal("A/b+CQ==", parser.ToString(new byte[] { 3, 246, 254, 9 }));
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, byte[] expectedResult, int expectedIndex)
        {
            ByteArrayHeaderParser parser = ByteArrayHeaderParser.Parser;
            object result = 0;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result));
            Assert.Equal(expectedIndex, startIndex);

            if (result == null)
            {
                Assert.Null(expectedResult);
            }
            else
            {
                byte[] arrayResult = (byte[])result;
                Assert.Equal(expectedResult, arrayResult);
            }
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            ByteArrayHeaderParser parser = ByteArrayHeaderParser.Parser;
            object result = 0;
            int newIndex = startIndex;
            Assert.False(parser.TryParseValue(input, null, ref newIndex, out result));
            Assert.Null(result);
            Assert.Equal(startIndex, newIndex);
        }
        #endregion
    }
}
