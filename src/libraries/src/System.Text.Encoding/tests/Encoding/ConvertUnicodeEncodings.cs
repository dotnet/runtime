// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class ConvertUnicodeEncodings
    {
        private static byte[] s_leBytes = new byte[] { 0x7A, 0x00, 0x61, 0x00, 0x06, 0x03, 0xFD, 0x01, 0xB2, 0x03, 0xFF, 0xD8, 0xFF, 0xDC };
        private static byte[] s_beBytes = new byte[] { 0x00, 0x7A, 0x00, 0x61, 0x03, 0x06, 0x01, 0xFD, 0x03, 0xB2, 0xD8, 0xFF, 0xDC, 0xFF };
        private static byte[] s_utf8Bytes = new byte[] { 0x7A, 0x61, 0xCC, 0x86, 0xC7, 0xBD, 0xCE, 0xB2, 0xF1, 0x8F, 0xB3, 0xBF };
        
        [Fact]
        public void TestConversion()
        {
            Assert.Equal(Encoding.Convert(Encoding.Unicode, Encoding.UTF8, s_leBytes), s_utf8Bytes);
            Assert.Equal(Encoding.Convert(Encoding.UTF8, Encoding.Unicode, s_utf8Bytes), s_leBytes);

            Assert.Equal(Encoding.Convert(Encoding.BigEndianUnicode, Encoding.UTF8, s_beBytes), s_utf8Bytes);
            Assert.Equal(Encoding.Convert(Encoding.UTF8, Encoding.BigEndianUnicode, s_utf8Bytes), s_beBytes);

            Assert.Equal(Encoding.Convert(Encoding.BigEndianUnicode, Encoding.Unicode, s_beBytes), s_leBytes);
            Assert.Equal(Encoding.Convert(Encoding.Unicode, Encoding.BigEndianUnicode, s_leBytes), s_beBytes);
        }
    }
}
