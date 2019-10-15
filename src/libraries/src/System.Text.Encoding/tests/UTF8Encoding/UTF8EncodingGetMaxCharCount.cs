// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Assert.Equal(expected, new UTF8Encoding(true, true).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF8Encoding(true, false).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF8Encoding(false, true).GetMaxCharCount(byteCount));
            Assert.Equal(expected, new UTF8Encoding(false, false).GetMaxCharCount(byteCount));
        }
    }
}
