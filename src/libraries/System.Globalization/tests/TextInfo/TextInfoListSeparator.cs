// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoListSeparator
    {
        [Fact]
        public void ListSeparator_EnUS()
        {
            Assert.NotEqual(string.Empty, new CultureInfo("en-US").TextInfo.ListSeparator);
        }
        
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("abcdef")]
        public void ListSeparator_Set(string newListSeparator)
        {
            TextInfo textInfo = new CultureInfo("en-US").TextInfo;
            textInfo.ListSeparator = newListSeparator;
            Assert.Equal(newListSeparator, textInfo.ListSeparator);
        }

        [Fact]
        public void ListSeparator_Set_Invalid()
        {
            Assert.Throws<InvalidOperationException>(() => CultureInfo.InvariantCulture.TextInfo.ListSeparator = "");
            AssertExtensions.Throws<ArgumentNullException>("value", () => new CultureInfo("en-US").TextInfo.ListSeparator = null);
        }
    }
}
