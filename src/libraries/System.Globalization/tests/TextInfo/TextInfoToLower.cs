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
        [InlineData("en-US", "HELLOWORLD!", "helloworld!")]
        [InlineData("en-US", "HelloWorld123!", "helloworld123!")]
        [InlineData("en-US", "", "")]
        [InlineData("en-US", "HELLO\n\0WORLD\t!", "hello\n\0world\u0009!")]
        [InlineData("fr-FR", "HELLOWORLD!", "helloworld!")]
        [InlineData("fr-FR", "HELLO\n\0WORLD\t!", "hello\n\0world\u0009!")]
        [InlineData("tr-TR", "HI!", "h\u0131!")]
        [InlineData("tr-TR", "HI\n\0H\u0130\t!", "h\u0131\n\0hi\u0009!")]
        public void ToLower(string name, string str, string expected)
        {
            Assert.Equal(expected, new CultureInfo(name).TextInfo.ToLower(str));
            if (str.Length == 1)
            {
                Assert.Equal(expected[0], new CultureInfo(name).TextInfo.ToLower(str[0]));
            }
        }

        [Fact]
        public void ToLower_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CultureInfo("en-US").TextInfo.ToLower(null));
        }
    }
}
