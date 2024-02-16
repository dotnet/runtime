// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingGetMaxByteCount
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(int.MaxValue / 3 - 1)]
        public void GetMaxByteCount(int charCount)
        {
            int expected = (charCount + 1) * 3;
            Assert.Equal(expected, Encoding.UTF8.GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF8Encoding(true, true).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF8Encoding(true, false).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF8Encoding(false, true).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF8Encoding(false, false).GetMaxByteCount(charCount));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        [InlineData(-1_000_000_000)]
        [InlineData(-1_300_000_000)] // yields positive result when *3
        [InlineData(int.MaxValue / 3)]
        [InlineData(int.MaxValue)]
        public void GetMaxByteCount_NegativeTests(int charCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(nameof(charCount), () => Encoding.UTF8.GetMaxByteCount(charCount));
            Assert.Throws<ArgumentOutOfRangeException>(nameof(charCount), () => new UTF8Encoding().GetMaxByteCount(charCount));
        }
    }
}
