// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class TokenListParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            HttpHeaderParser parser = GenericHeaderParser.TokenListParser;
            Assert.True(parser.SupportsMultipleValues);
            Assert.Equal(StringComparer.OrdinalIgnoreCase, parser.Comparer);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParsedValue("text", 0, "text", 4);
            CheckValidParsedValue("text,", 0, "text", 5);
            CheckValidParsedValue("\r\n text , next_text  ", 0, "text", 10);
            CheckValidParsedValue("  text,next_text  ", 2, "text", 7);
            CheckValidParsedValue(" ,, text, , ,next", 0, "text", 13);
            CheckValidParsedValue(" ,, text, , ,", 0, "text", 13);

            CheckValidParsedValue(null, 0, null, 0);
            CheckValidParsedValue(string.Empty, 0, null, 0);
            CheckValidParsedValue("   ", 0, null, 3);
            CheckValidParsedValue("  ,,", 0, null, 4);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("teäxt", 0);
            CheckInvalidParsedValue("text会", 0);
            CheckInvalidParsedValue("会", 0);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, string expectedResult, int expectedIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.TokenListParser;
            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false: {0}", input));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(result, expectedResult);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.TokenListParser;
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
