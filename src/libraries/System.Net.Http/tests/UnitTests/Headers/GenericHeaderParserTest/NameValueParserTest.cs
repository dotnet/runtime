// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class NameValueParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueNameValueParser;
            Assert.True(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);

            parser = GenericHeaderParser.SingleValueNameValueParser;
            Assert.False(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParsedValue("X , , name = value  ,  ,next", 1, new NameValueHeaderValue("name", "value"), 24);
            CheckValidParsedValue("X name,", 1, new NameValueHeaderValue("name"), 7);
            CheckValidParsedValue(" ,name=\"value\"", 0, new NameValueHeaderValue("name", "\"value\""), 14);
            CheckValidParsedValue("name=value", 0, new NameValueHeaderValue("name", "value"), 10);

            CheckValidParsedValue(null, 0, null, 0);
            CheckValidParsedValue(string.Empty, 0, null, 0);
            CheckValidParsedValue("  ", 0, null, 2);
            CheckValidParsedValue("  ,,", 0, null, 4);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("name[value", 0);
            CheckInvalidParsedValue("name=value=", 0);
            CheckInvalidParsedValue("name=\u4F1A", 0);
            CheckInvalidParsedValue("name==value", 0);
            CheckInvalidParsedValue("=value", 0);
            CheckInvalidParsedValue("name value", 0);
            CheckInvalidParsedValue("name=,value", 0);
            CheckInvalidParsedValue("\u4F1A", 0);
            CheckInvalidParsedValue("name=\nvalue", 0);
            CheckInvalidParsedValue("name=val\nue", 0);
            CheckInvalidParsedValue("name=value\n", 0);
            CheckInvalidParsedValue("\"name=\nvalue\"", 0);
            CheckInvalidParsedValue("\"name=val\nue\"", 0);
            CheckInvalidParsedValue("\"name=value\n\"", 0);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, NameValueHeaderValue expectedResult,
            int expectedIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueNameValueParser;
            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false: {0}", input));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(result, expectedResult);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueNameValueParser;
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
