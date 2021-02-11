// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingGetMaxByteCount
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(int.MaxValue / 2 - 1)]
        public void GetMaxByteCount(int charCount)
        {
            int expected = (charCount + 1) * 2;
            Assert.Equal(expected, new UnicodeEncoding(false, true, false).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UnicodeEncoding(false, false, false).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UnicodeEncoding(true, true, false).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UnicodeEncoding(true, false, false).GetMaxByteCount(charCount));

            Assert.Equal(expected, new UnicodeEncoding(false, true, true).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UnicodeEncoding(false, false, true).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UnicodeEncoding(true, true, true).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UnicodeEncoding(true, false, true).GetMaxByteCount(charCount));
        }
    }
}
