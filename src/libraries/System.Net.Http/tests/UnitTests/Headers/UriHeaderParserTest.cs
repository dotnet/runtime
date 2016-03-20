// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class UriHeaderParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            UriHeaderParser parser = UriHeaderParser.RelativeOrAbsoluteUriParser;
            Assert.False(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            // We don't need to validate all possible Uri values, since we use Uri.TryParse().
            // Just make sure the parser calls Uri.TryParse() correctly.
            CheckValidParsedValue("/this/is/a/rel/uri", 0, new Uri("/this/is/a/rel/uri", UriKind.Relative), 18);
            CheckValidParsedValue("!!  http://example.com/path,/ ", 2, new Uri("http://example.com/path,/"), 30);

            // Note that Uri.TryParse(.., UriKind.Relative) doesn't remove whitespace
            CheckValidParsedValue("!!  /path/x,/  ", 2, new Uri("  /path/x,/  ", UriKind.Relative), 15);
            CheckValidParsedValue("  http://example.com/path/?query=value   ", 2, new Uri("http://example.com/path/?query=value"), 41);
            CheckValidParsedValue("  http://example.com/path/?query=value \r\n  ", 2, new Uri("http://example.com/path/?query=value"), 43);
            CheckValidParsedValue("http://idn-iis1.\u65E5\u672C\u56FD.microsoft.com/", 0, new Uri("http://idn-iis1.\u65E5\u672C\u56FD.microsoft.com/"), 34);
            CheckValidParsedValue("http://idn-iis1.\u00E6\u0097\u00A5\u00E6\u009C\u00AC\u00E5\u009B\u00BD.microsoft.com/", 0, new Uri("http://idn-iis1.\u65E5\u672C\u56FD.microsoft.com/"), 40);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("http://example.com,", 0);

            CheckInvalidParsedValue(null, 0);
            CheckInvalidParsedValue(null, 0);
            CheckInvalidParsedValue(string.Empty, 0);
            CheckInvalidParsedValue(string.Empty, 0);
            CheckInvalidParsedValue("  ", 2);
            CheckInvalidParsedValue("  ", 2);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, Uri expectedResult, int expectedIndex)
        {
            UriHeaderParser parser = UriHeaderParser.RelativeOrAbsoluteUriParser;

            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false: {0}", input));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            UriHeaderParser parser = UriHeaderParser.RelativeOrAbsoluteUriParser;

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
