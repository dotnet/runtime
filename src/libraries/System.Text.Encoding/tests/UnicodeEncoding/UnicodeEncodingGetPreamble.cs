// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingGetPreamble
    {
        [Theory]
        [InlineData(true, false, new byte[0])]
        [InlineData(true, true, new byte[] { 0xfe, 0xff })]
        [InlineData(false, true, new byte[] { 0xff, 0xfe })]
        [InlineData(false, false, new byte[0])]
        public void GetPreamble(bool bigEndian, bool byteOrderMark, byte[] expected)
        {
            UnicodeEncoding encoding = new UnicodeEncoding(bigEndian, byteOrderMark);
            Assert.Equal(expected, encoding.GetPreamble());
        }
    }
}
