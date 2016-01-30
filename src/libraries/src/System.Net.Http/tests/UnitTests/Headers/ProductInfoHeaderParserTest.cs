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
    public class ProductInfoHeaderParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            ProductInfoHeaderParser parser = ProductInfoHeaderParser.SingleValueParser;
            Assert.False(parser.SupportsMultipleValues, "SupportsMultipleValues");
            Assert.Null(parser.Comparer);

            parser = ProductInfoHeaderParser.MultipleValueParser;
            Assert.True(parser.SupportsMultipleValues, "SupportsMultipleValues");
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParsedValue("X product ", 1, new ProductInfoHeaderValue("product", null), 10);

            // Note that the following is considered valid, since we have a valid product and after the ',' delimiter
            // we have non-whitespace characters. It's the callers responsibility to consider the whole string invalid.
            CheckValidParsedValue("p/1.0 =", 0, new ProductInfoHeaderValue("p", "1.0"), 6);

            CheckValidParsedValue(" (comment)   p", 0, new ProductInfoHeaderValue("(comment)"), 13);

            CheckValidParsedValue(" Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0) ", 0,
                new ProductInfoHeaderValue("Mozilla", "5.0"), 13);
            CheckValidParsedValue(" Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0) ", 13,
                new ProductInfoHeaderValue("(compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)"), 13 + 59);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("p/1.0,", 0);
            CheckInvalidParsedValue("p/1.0\r\n", 0); // for \r\n to be a valid whitespace, it must be followed by space/tab
            CheckInvalidParsedValue("p/1.0(comment)", 0);
            CheckInvalidParsedValue("(comment)[", 0);

            // "User-Agent" and "Server" don't allow empty values (unlike most other headers supporting lists of values)
            CheckInvalidParsedValue(null, 0);
            CheckInvalidParsedValue(string.Empty, 0);
            CheckInvalidParsedValue("  ", 0);
            CheckInvalidParsedValue("\t", 0);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, ProductInfoHeaderValue expectedResult,
            int expectedIndex)
        {
            ProductInfoHeaderParser parser = ProductInfoHeaderParser.MultipleValueParser;
            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false. Input: '{0}'", input));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            ProductInfoHeaderParser parser = ProductInfoHeaderParser.MultipleValueParser;
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
