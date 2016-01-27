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
    public class ContentRangeParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            HttpHeaderParser parser = GenericHeaderParser.ContentRangeParser;
            Assert.False(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            // Only verify parser functionality (i.e. ContentRangeHeaderParser.TryParse()). We don't need to validate
            // all possible range values (verification done by tests for ContentRangeHeaderValue.GetContentRangeLength()).
            CheckValidParsedValue("X bytes 1-2/3 ", 1, new ContentRangeHeaderValue(1, 2, 3), 14);
            CheckValidParsedValue("bytes  *  /  3", 0, new ContentRangeHeaderValue(3), 14);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("bytes 1-2/3,", 0); // no character after 'length' allowed
            CheckInvalidParsedValue("x bytes 1-2/3", 0);
            CheckInvalidParsedValue("bytes 1-2/3.4", 0);
            CheckInvalidParsedValue(null, 0);
            CheckInvalidParsedValue(string.Empty, 0);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, ContentRangeHeaderValue expectedResult,
            int expectedIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.ContentRangeParser;
            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false. Input: '{0}'", input));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.ContentRangeParser;
            object result = null;
            int newIndex = startIndex;
            Assert.False(parser.TryParseValue(input, null, ref newIndex, out result),
                string.Format("TryParse returned true. Input: '{0}'", input));
            Assert.Equal(null, result);
            Assert.Equal(startIndex, newIndex);
        }
        #endregion
    }
}
