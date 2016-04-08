// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingGetMaxCharCount
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(8, 9)]
        public void GetMaxCharCount(int byteCount, int expected)
        {
            Assert.Equal(expected, new UTF8Encoding().GetMaxCharCount(byteCount));
        }
    }
}
