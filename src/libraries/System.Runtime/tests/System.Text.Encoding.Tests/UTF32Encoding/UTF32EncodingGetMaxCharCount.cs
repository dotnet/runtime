// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public class UTF32EncodingGetMaxCharCount
    {
        [Theory]
        [InlineData(0, 2)]
        [InlineData(1, 2)]
        [InlineData(2, 3)]
        [InlineData(3, 3)]
        [InlineData(4, 4)]
        [InlineData(5, 4)]
        [InlineData(6, 5)]
        [InlineData(7, 5)]
        [InlineData(8, 6)]
        [InlineData(9, 6)]
        [InlineData(10, 7)]
        [InlineData(11, 7)]
        [InlineData(12, 8)]
        [InlineData(13, 8)]
        [InlineData(14, 9)]
        [InlineData(int.MaxValue, 1073741825)]
        public void GetMaxCharCount(int byteCount, int expected)
        {
            Assert.Equal(expected, new UTF32Encoding(true, false, false).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF32Encoding(true, true, false).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF32Encoding(true, false, true).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF32Encoding(true, true, true).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF32Encoding(false, true, true).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF32Encoding(false, true, false).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF32Encoding(false, false, true).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF32Encoding(false, false, false).GetMaxCharCount(byteCount));
        }
    }
}
