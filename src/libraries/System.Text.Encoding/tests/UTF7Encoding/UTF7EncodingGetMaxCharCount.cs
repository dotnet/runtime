// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingGetMaxCharCount
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(int.MaxValue)]
        public void GetMaxCharCount(int byteCount)
        {
            int expected = Math.Max(byteCount, 1);
            Assert.Equal(expected, new UTF7Encoding(true).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF7Encoding(false).GetMaxCharCount(byteCount));
        }
    }
}
