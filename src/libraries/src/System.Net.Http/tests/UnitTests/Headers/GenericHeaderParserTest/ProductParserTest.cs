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
    public class ProductParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueProductParser;
            Assert.True(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);

            parser = GenericHeaderParser.SingleValueProductParser;
            Assert.False(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParsedValue("X y/1 ", 1, new ProductHeaderValue("y", "1"), 6);
            CheckValidParsedValue(", , custom / 1.0 ,,Y", 0, new ProductHeaderValue("custom", "1.0"), 19);
            CheckValidParsedValue(", , custom / 1.0 ,,", 0, new ProductHeaderValue("custom", "1.0"), 19);
            CheckValidParsedValue(", , custom / 1.0", 0, new ProductHeaderValue("custom", "1.0"), 16);

            CheckValidParsedValue(null, 0, null, 0);
            CheckValidParsedValue(string.Empty, 0, null, 0);
            CheckValidParsedValue("  ", 0, null, 2);
            CheckValidParsedValue("  ,,", 0, null, 4);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("product/version=", 0); // only delimiter ',' allowed after last product
            CheckInvalidParsedValue("product otherproduct", 0);
            CheckInvalidParsedValue("product[", 0);
            CheckInvalidParsedValue("=", 0);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, ProductHeaderValue expectedResult,
            int expectedIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueProductParser;
            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false. Input: '{0}'", input));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(result, expectedResult);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueProductParser;
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
