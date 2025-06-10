// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class CookieHeaderParserTest
    {
        [Fact]
        public void TryParse_ValidValueString_ReturnsTrue()
        {
            HttpHeaderParser parser = CookieHeaderParser.Parser;
            string validInput = "Hello World";
            int startIndex = 0;

            Assert.True(parser.TryParseValue(validInput, null, ref startIndex, out object? parsedValue));
            Assert.Equal(validInput.Length, startIndex);
            Assert.Same(validInput, parsedValue);
        }

        [Fact]
        public void TryParse_InvalidValueString_ReturnsFalse()
        {
            HttpHeaderParser parser = CookieHeaderParser.Parser;
            string invalidInput = "Hello\r\nWorld";
            int startIndex = 0;

            Assert.False(parser.TryParseValue(invalidInput, null, ref startIndex, out object? parsedValue));
            Assert.Equal(0, startIndex);
            Assert.Null(parsedValue);
        }
    }
}
