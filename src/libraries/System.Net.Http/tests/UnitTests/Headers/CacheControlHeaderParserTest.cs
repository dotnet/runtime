// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class CacheControlHeaderParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            HttpHeaderParser parser = CacheControlHeaderParser.Parser;
            Assert.True(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            // Just verify parser is implemented correctly. Don't try to test syntax parsed by CacheControlHeaderValue.
            CacheControlHeaderValue expected = new CacheControlHeaderValue();
            expected.NoStore = true;
            expected.MinFresh = new TimeSpan(0, 2, 3);
            CheckValidParsedValue("X , , no-store, min-fresh=123", 1, expected, 29);

            expected = new CacheControlHeaderValue();
            expected.MaxStale = true;
            expected.NoCache = true;
            expected.NoCacheHeaders.Add("t");
            CheckValidParsedValue("max-stale, no-cache=\"t\", ,,", 0, expected, 27);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("no-cache,=", 0);
            CheckInvalidParsedValue("max-age=123x", 0);
            CheckInvalidParsedValue("=no-cache", 0);
            CheckInvalidParsedValue("no-cache no-store", 0);
            CheckInvalidParsedValue("invalid =", 0);
            CheckInvalidParsedValue("\u4F1A", 0);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, CacheControlHeaderValue expectedResult,
            int expectedIndex)
        {
            HttpHeaderParser parser = CacheControlHeaderParser.Parser;
            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(result, expectedResult);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            HttpHeaderParser parser = CacheControlHeaderParser.Parser;
            object result = null;
            int newIndex = startIndex;
            Assert.False(parser.TryParseValue(input, null, ref newIndex, out result));
            Assert.Equal(null, result);
            Assert.Equal(startIndex, newIndex);
        }
        #endregion
    }
}
