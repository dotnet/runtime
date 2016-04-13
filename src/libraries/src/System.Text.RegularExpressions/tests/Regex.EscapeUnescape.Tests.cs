// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexEscapeUnescapeTests
    {
        [Theory]
        [InlineData("#$^*+(){}<>\\|. ")]
        public static void EscapeUnescape(string source)
        {
            string escaped = Regex.Escape(source);
            string unescaped = Regex.Unescape(escaped);
            Assert.Equal(source, unescaped);
        }
    }
}
