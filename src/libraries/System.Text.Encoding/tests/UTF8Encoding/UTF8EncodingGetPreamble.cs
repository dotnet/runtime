// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingGetPreamble
    {
        [Theory]
        [InlineData(true, new byte[] { 0xEF, 0xBB, 0xBF })]
        [InlineData(false, new byte[0])]
        public void GetPreamble(bool emitUTF8Identifier, byte[] expected)
        {
            Assert.Equal(expected, new UTF8Encoding(emitUTF8Identifier).GetPreamble());
        }
    }
}
