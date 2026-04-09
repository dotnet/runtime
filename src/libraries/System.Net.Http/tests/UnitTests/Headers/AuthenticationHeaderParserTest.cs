// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class AuthenticationHeaderParserTest
    {
        [Fact]
        public void TryParse_InvalidValueString_ReturnsFalse()
        {
            HttpHeaderParser parser = GenericHeaderParser.MultipleValueAuthenticationParser;
            string invalidInput = "a \n";
            int startIndex = 0;

            Assert.False(parser.TryParseValue(invalidInput, null, ref startIndex, out var _));
        }
    }
}
