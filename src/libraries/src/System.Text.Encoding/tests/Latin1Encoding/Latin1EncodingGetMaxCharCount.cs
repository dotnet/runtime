// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class Latin1EncodingGetMaxCharCount
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void GetMaxCharCount(int byteCount)
        {
            Assert.Equal(byteCount, Encoding.GetEncoding("latin1").GetMaxCharCount(byteCount));
        }
    }
}
