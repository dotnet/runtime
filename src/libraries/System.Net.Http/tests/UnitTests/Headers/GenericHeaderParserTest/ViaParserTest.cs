// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class ViaParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueViaParser;
            Assert.True(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);

            parser = GenericHeaderParser.SingleValueViaParser;
            Assert.False(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParsedValue("X , , 1.1   host, ,next", 1, new ViaHeaderValue("1.1", "host"), 19);
            CheckValidParsedValue("X HTTP  /  x11   192.168.0.1 (comment) , ,next", 1,
                new ViaHeaderValue("x11", "192.168.0.1", "HTTP", "(comment)"), 42);
            CheckValidParsedValue(" ,HTTP/1.1 [::1]", 0, new ViaHeaderValue("1.1", "[::1]", "HTTP"), 16);
            CheckValidParsedValue("1.1 host", 0, new ViaHeaderValue("1.1", "host"), 8);

            CheckValidParsedValue(null, 0, null, 0);
            CheckValidParsedValue(string.Empty, 0, null, 0);
            CheckValidParsedValue("  ", 0, null, 2);
            CheckValidParsedValue("  ,,", 0, null, 4);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("HTTP/1.1 host (comment)invalid", 0);
            CheckInvalidParsedValue("HTTP/1.1 host (comment)=", 0);
            CheckInvalidParsedValue("HTTP/1.1 host (comment) invalid", 0);
            CheckInvalidParsedValue("HTTP/1.1 host (comment) =", 0);
            CheckInvalidParsedValue("HTTP/1.1 host invalid", 0);
            CheckInvalidParsedValue("HTTP/1.1 host =", 0);
            CheckInvalidParsedValue("1.1 host invalid", 0);
            CheckInvalidParsedValue("1.1 host =", 0);
            CheckInvalidParsedValue("\u4F1A", 0);
            CheckInvalidParsedValue("HTTP/test [::1]:80\r(comment)", 0);
            CheckInvalidParsedValue("HTTP/test [::1]:80\n(comment)", 0);
            CheckInvalidParsedValue("X HTTP  /  x11   192.168.0.1 (comment) , ,next", 0);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, ViaHeaderValue expectedResult,
            int expectedIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueViaParser;
            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false: {0}", input));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(result, expectedResult);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueViaParser;
            object result = null;
            int newIndex = startIndex;
            Assert.False(parser.TryParseValue(input, null, ref newIndex, out result),
                string.Format("TryParse returned true: {0}", input));
            Assert.Null(result);
            Assert.Equal(startIndex, newIndex);
        }
        #endregion
    }
}
