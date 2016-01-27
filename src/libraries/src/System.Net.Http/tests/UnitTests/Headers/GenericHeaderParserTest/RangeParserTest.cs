// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class RangeParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            HttpHeaderParser parser = GenericHeaderParser.RangeParser;
            Assert.False(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParsedValue("X bytes=1-2 ", 1, new RangeHeaderValue(1, 2), 12);

            RangeHeaderValue expected = new RangeHeaderValue();
            expected.Unit = "custom";
            expected.Ranges.Add(new RangeItemHeaderValue(null, 5));
            expected.Ranges.Add(new RangeItemHeaderValue(1, 4));
            CheckValidParsedValue("custom = -  5 , 1 - 4 ,,", 0, expected, 24);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("bytes=1-2x", 0); // only delimiter ',' allowed after last range
            CheckInvalidParsedValue("x bytes=1-2", 0);
            CheckInvalidParsedValue("bytes=1-2.4", 0);
            CheckInvalidParsedValue(null, 0);
            CheckInvalidParsedValue(string.Empty, 0);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, RangeHeaderValue expectedResult,
            int expectedIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.RangeParser;
            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false. Input: '{0}'", input));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.RangeParser;
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
