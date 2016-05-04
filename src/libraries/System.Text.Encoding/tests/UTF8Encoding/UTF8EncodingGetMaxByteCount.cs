// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Assert.Equal(expected, new UTF8Encoding(true, true).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF8Encoding(true, false).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF8Encoding(false, true).GetMaxByteCount(charCount));
            Assert.Equal(expected, new UTF8Encoding(false, false).GetMaxByteCount(charCount));
        }
    }
}
