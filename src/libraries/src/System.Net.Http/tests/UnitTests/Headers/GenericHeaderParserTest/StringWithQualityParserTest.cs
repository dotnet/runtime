// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class StringWithQualityParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueStringWithQualityParser;
            Assert.True(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);

            parser = GenericHeaderParser.SingleValueStringWithQualityParser;
            Assert.False(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParsedValue("text", 0, new StringWithQualityHeaderValue("text"), 4);
            CheckValidParsedValue("text,", 0, new StringWithQualityHeaderValue("text"), 5);
            CheckValidParsedValue("\r\n text ; q = 0.5, next_text  ", 0, new StringWithQualityHeaderValue("text", 0.5), 19);
            CheckValidParsedValue("  text,next_text  ", 2, new StringWithQualityHeaderValue("text"), 7);
            CheckValidParsedValue(" ,, text, , ,next", 0, new StringWithQualityHeaderValue("text"), 13);
            CheckValidParsedValue(" ,, text, , ,", 0, new StringWithQualityHeaderValue("text"), 13);
            CheckValidParsedValue(", \r\n text \r\n ; \r\n q = 0.123", 0,
                new StringWithQualityHeaderValue("text", 0.123), 27);

            CheckValidParsedValue(null, 0, null, 0);
            CheckValidParsedValue(string.Empty, 0, null, 0);
            CheckValidParsedValue("  ", 0, null, 2);
            CheckValidParsedValue("  ,,", 0, null, 4);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("teäxt", 0);
            CheckInvalidParsedValue("text会", 0);
            CheckInvalidParsedValue("会", 0);
            CheckInvalidParsedValue("t;q=会", 0);
            CheckInvalidParsedValue("t;q=", 0);
            CheckInvalidParsedValue("t;q", 0);
            CheckInvalidParsedValue("t;会=1", 0);
            CheckInvalidParsedValue("t;q会=1", 0);
            CheckInvalidParsedValue("t y", 0);
            CheckInvalidParsedValue("t;q=1 y", 0);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, StringWithQualityHeaderValue expectedResult,
            int expectedIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueStringWithQualityParser;
            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false: {0}", input));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(result, expectedResult);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueStringWithQualityParser;
            object result = null;
            int newIndex = startIndex;
            Assert.False(parser.TryParseValue(input, null, ref newIndex, out result),
                string.Format("TryParse returned true: {0}", input));
            Assert.Equal(null, result);
            Assert.Equal(startIndex, newIndex);
        }
        #endregion
    }
}
