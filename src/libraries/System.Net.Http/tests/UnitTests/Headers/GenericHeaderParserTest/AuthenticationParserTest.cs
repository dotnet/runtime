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
    public class AuthenticationParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueAuthenticationParser;
            Assert.True(parser.SupportsMultipleValues);
            Assert.Null(parser.Comparer);

            parser = GenericHeaderParser.SingleValueAuthenticationParser;
            Assert.False(parser.SupportsMultipleValues);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            // Note that there is no difference between setting "SupportMultipleValues" to true or false: The parser
            // is only able to parse one authentication information per string. Setting "SupportMultipleValues" just
            // tells the caller (HttpHeaders) that parsing multiple strings is allowed.
            CheckValidParsedValue("X NTLM ", 1, new AuthenticationHeaderValue("NTLM"), 7, true);
            CheckValidParsedValue("X NTLM ", 1, new AuthenticationHeaderValue("NTLM"), 7, false);
            CheckValidParsedValue("custom x=y", 0, new AuthenticationHeaderValue("Custom", "x=y"), 10, true);
            CheckValidParsedValue("custom x=y", 0, new AuthenticationHeaderValue("Custom", "x=y"), 10, false);
            CheckValidParsedValue("C x=y, other", 0, new AuthenticationHeaderValue("C", "x=y"), 7, true);

            CheckValidParsedValue("  ", 0, null, 2, true);
            CheckValidParsedValue(null, 0, null, 0, true);
            CheckValidParsedValue("", 0, null, 0, true);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue("NTLM[", 0, true); // only delimiter ',' allowed after last range
            CheckInvalidParsedValue("NTLM[", 0, false); // only delimiter ',' allowed after last range
            CheckInvalidParsedValue("]NTLM", 0, true);
            CheckInvalidParsedValue("]NTLM", 0, false);
            CheckInvalidParsedValue("C x=y, other", 0, false);
            CheckInvalidParsedValue("C x=y,", 0, false);
            CheckInvalidParsedValue("  ", 0, false);
            CheckInvalidParsedValue(null, 0, false);
            CheckInvalidParsedValue(string.Empty, 0, false);
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, int startIndex, AuthenticationHeaderValue expectedResult,
            int expectedIndex, bool supportMultipleValues)
        {
            HttpHeaderParser parser = null;
            if (supportMultipleValues)
            {
                parser = GenericHeaderParser.MultipleValueAuthenticationParser;
            }
            else
            {
                parser = GenericHeaderParser.SingleValueAuthenticationParser;
            }

            object result = null;
            Assert.True(parser.TryParseValue(input, null, ref startIndex, out result),
                string.Format("TryParse returned false. Input: '{0}'", input));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(result, expectedResult);
        }

        private void CheckInvalidParsedValue(string input, int startIndex, bool supportMultipleValues)
        {
            HttpHeaderParser parser = null;
            if (supportMultipleValues)
            {
                parser = GenericHeaderParser.MultipleValueAuthenticationParser;
            }
            else
            {
                parser = GenericHeaderParser.SingleValueAuthenticationParser;
            }

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
