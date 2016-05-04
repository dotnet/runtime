// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingGetMaxByteCount
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(715827881)]
        [InlineData((int.MaxValue - 2) / 3)]
        public void GetMaxByteCount(int charCount)
        {
            int expected = charCount * 3 + 2;
            Assert.Equal(expected, new UTF7Encoding(true).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF7Encoding(false).GetMaxByteCount(charCount));
        }
    }
}
