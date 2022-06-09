// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingGetMaxCharCount
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(int.MaxValue - 1)]
        public void GetMaxCharCount(int byteCount)
        {
            int expected = byteCount + 1;
            Assert.Equal(expected, Encoding.UTF8.GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF8Encoding(true, true).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF8Encoding(true, false).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF8Encoding(false, true).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF8Encoding(false, false).GetMaxCharCount(byteCount));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        [InlineData(-1_000_000_000)]
        [InlineData(int.MaxValue)]
        public void GetMaxCharCount_NegativeTests(int byteCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(byteCount), () => Encoding.UTF8.GetMaxCharCount(byteCount));
            Assert.Throws<ArgumentOutOfRangeException>(nameof(byteCount), () => new UTF8Encoding().GetMaxCharCount(byteCount));
        }
    }
}
