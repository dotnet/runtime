// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.Http.Headers;
using System.Text;

using Xunit;

namespace System.Net.Http.Tests
{
    public class MailAddressParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            HttpHeaderParser parser = GenericHeaderParser.MailAddressParser;
            Assert.False(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            // We don't need to validate all possible date values, since they're already tested MailAddress.
            // Just make sure the parser calls MailAddressParser with correct parameters (like startIndex must be
            // honored).

            // Note that we still have trailing whitespace since we don't do the parsing of the email address.
            CheckValidParsedValue("!!      info@example.com   ", 2, "info@example.com   ", 27);
            CheckValidParsedValue("\r\n \"My name\" info@example.com", 0,
                "\"My name\" info@example.com", 29);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("[info@example.com", 0);
            CheckInvalidParsedValue("info@example.com\r\nother", 0);
            CheckInvalidParsedValue("info@example.com\r\n other", 0);
            CheckInvalidParsedValue("info@example.com\r\n", 0);
            CheckInvalidParsedValue("info@example.com,", 0);
            CheckInvalidParsedValue("\r\ninfo@example.com", 0);
            CheckInvalidParsedValue(null, 0);
            CheckInvalidParsedValue(string.Empty, 0);
            CheckInvalidParsedValue("  ", 2);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, string expectedResult,
            int expectedIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.MailAddressParser;
            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false: {0}", input));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParsedValue(string input, int startIndex)
        {
            HttpHeaderParser parser = GenericHeaderParser.MailAddressParser;
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
