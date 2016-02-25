// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoToUpper
    {
        [Theory]
        [InlineData("en-US", "a", "A")]
        [InlineData("en-US", "A", "A")]
        [InlineData("en-US", "1", "1")]
        [InlineData("en-US", "9", "9")]
        [InlineData("fr-FR", "g", "G")]
        [InlineData("fr-FR", "G", "G")]
        [InlineData("tr-TR", "i", "\u0130")]
        [InlineData("tr-TR", "\u0130", "\u0130")]
        [InlineData("en-US", "HelloWorld!", "HELLOWORLD!")]
        [InlineData("en-US", "", "")]
        [InlineData("en-US", "Hello\n\0World\u0009!", "HELLO\n\0WORLD\t!")]
        [InlineData("fr-FR", "HelloWorld!", "HELLOWORLD!")]
        [InlineData("fr-FR", "Hello\n\0World\u0009!", "HELLO\n\0WORLD\t!")]
        [InlineData("tr-TR", "H\u0131!", "HI!")]
        [InlineData("tr-TR", "H\u0131\n\0Hi\u0009!", "HI\n\0H\u0130\t!")]
        public void ToUpper(string name, string str, string expected)
        {
            Assert.Equal(expected, new CultureInfo(name).TextInfo.ToUpper(str));
            if (str.Length == 1)
            {
                Assert.Equal(expected[0], new CultureInfo(name).TextInfo.ToUpper(str[0]));
            }
        }

        [Fact]
        public void ToUpper_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CultureInfo("en-US").TextInfo.ToUpper(null));
        }
    }
}
