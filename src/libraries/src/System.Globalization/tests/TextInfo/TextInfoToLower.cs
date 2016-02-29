// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoToLower
    {
        [Theory]
        [InlineData("en-US", "A", "a")]
        [InlineData("en-US", "a", "a")]
        [InlineData("en-US", "1", "1")]
        [InlineData("en-US", "9", "9")]
        [InlineData("fr-FR", "G", "g")]
        [InlineData("fr-FR", "g", "g")]
        [InlineData("tr-TR", "\u0130", "i")]
        [InlineData("tr-TR", "I", "\u0131")]
        [InlineData("tr-TR", "i", "i")]
        public void ToLower(string name, string str, string expected)
        {
            Assert.Equal(expected, new CultureInfo(name).TextInfo.ToLower(str));
            if (str.Length == 1)
            {
                Assert.Equal(expected[0], new CultureInfo(name).TextInfo.ToLower(str[0]));
            }
        }
    }
}
