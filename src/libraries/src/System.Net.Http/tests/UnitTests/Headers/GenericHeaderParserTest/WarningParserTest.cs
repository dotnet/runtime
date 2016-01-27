// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

using Xunit;

namespace System.Net.Http.Tests
{
    public class WarningParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueWarningParser;
            Assert.True(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);

            parser = GenericHeaderParser.SingleValueWarningParser;
            Assert.False(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParsedValue("X , , 123   host \"text\", ,next", 1,
                new WarningHeaderValue(123, "host", "\"text\""), 26);
            CheckValidParsedValue("X 50  192.168.0.1  \"text  \"  \"Tue, 20 Jul 2010 01:02:03 GMT\" , ,next", 1,
                new WarningHeaderValue(50, "192.168.0.1", "\"text  \"",
                    new DateTimeOffset(2010, 7, 20, 1, 2, 3, TimeSpan.Zero)), 64);
            CheckValidParsedValue(" ,123 h \"t\",", 0, new WarningHeaderValue(123, "h", "\"t\""), 12);
            CheckValidParsedValue("1 h \"t\"", 0, new WarningHeaderValue(1, "h", "\"t\""), 7);
            CheckValidParsedValue("1 h \"t\" \"Tue, 20 Jul 2010 01:02:03 GMT\"", 0,
                new WarningHeaderValue(1, "h", "\"t\"",
                    new DateTimeOffset(2010, 7, 20, 1, 2, 3, TimeSpan.Zero)), 39);
            CheckValidParsedValue("1 会 \"t\" ,,", 0, new WarningHeaderValue(1, "会", "\"t\""), 10);

            CheckValidParsedValue(null, 0, null, 0);
            CheckValidParsedValue(string.Empty, 0, null, 0);
            CheckValidParsedValue("  ", 0, null, 2);
            CheckValidParsedValue("  ,,", 0, null, 4);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("1.1 host \"text\"", 0);
            CheckInvalidParsedValue("11 host text", 0);
            CheckInvalidParsedValue("11 host \"text\" Tue, 20 Jul 2010 01:02:03 GMT", 0);
            CheckInvalidParsedValue("11 host \"text\" 123 next \"text\"", 0);
            CheckInvalidParsedValue("会", 0);
            CheckInvalidParsedValue("123 会", 0);
            CheckInvalidParsedValue("111 [::1]:80\r(comment) \"text\"", 0);
            CheckInvalidParsedValue("111 [::1]:80\n(comment) \"text\"", 0);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, WarningHeaderValue expectedResult,
            int expectedIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueWarningParser;
            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false: {0}", input));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(result, expectedResult);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueWarningParser;
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
