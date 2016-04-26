// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingGetMaxCharCount
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 2)]
        [InlineData(10, 6)]
        [InlineData(int.MaxValue, 1073741825)]
        public void GetMaxCharCount(int byteCount, int expected)
        {
            Assert.Equal(expected, new UnicodeEncoding(false, true, false).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UnicodeEncoding(false, false, false).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UnicodeEncoding(true, true, false).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UnicodeEncoding(true, false, false).GetMaxCharCount(byteCount));

            Assert.Equal(expected, new UnicodeEncoding(false, true, true).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UnicodeEncoding(false, false, true).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UnicodeEncoding(true, true, true).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UnicodeEncoding(true, false, true).GetMaxCharCount(byteCount));
        }
    }
}
