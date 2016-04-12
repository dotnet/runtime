// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingGetCharCount
    {
        [Theory]
        [InlineData(new byte[] { 85, 84, 70, 56, 32, 69, 110, 99, 111, 100, 105, 110, 103, 32, 69, 120, 97, 109, 112, 108, 101 }, 2, 8, 8)]
        [InlineData(new byte[0], 0, 0, 0)]
        public void GetCharCount(byte[] bytes, int index, int count, int expected)
        {
            EncodingHelpers.GetCharCount(new UTF8Encoding(), bytes, index, count, expected);
        }
    }
}
