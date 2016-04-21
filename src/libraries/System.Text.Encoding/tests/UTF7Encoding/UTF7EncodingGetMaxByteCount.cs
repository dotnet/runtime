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
        [InlineData((int.MaxValue - 2) / 3)]
        public void GetMaxByteCount(int charCount)
        {
            Assert.Equal(charCount * 3 + 2, new UTF7Encoding().GetMaxByteCount(charCount));
        }
    }
}
