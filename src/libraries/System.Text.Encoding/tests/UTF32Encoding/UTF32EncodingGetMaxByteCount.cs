// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UTF32EncodingGetMaxByteCount
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(10)]
        [InlineData(268435455)]
        [InlineData(int.MaxValue / 4 - 1)]
        public void GetMaxByteCount(int charCount)
        {
            int expected = (charCount + 1) * 4;
            Assert.Equal(expected, new UTF32Encoding(true, false, false).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF32Encoding(true, true, false).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF32Encoding(true, false, true).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF32Encoding(true, true, true).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF32Encoding(false, true, true).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF32Encoding(false, true, false).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF32Encoding(false, false, true).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF32Encoding(false, false, false).GetMaxByteCount(charCount));
        }
    }
}
