// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class ConvertUnicodeEncodings
    {
        // The characters to encode:
        //    Latin Small Letter Z (U+007A)
        //    Latin Small Letter A (U+0061)
        //    Combining Breve (U+0306)
        //    Latin Small Letter AE With Acute (U+01FD)
        //    Greek Small Letter Beta (U+03B2)
        //    a high-surrogate value (U+D8FF)
        //    a low-surrogate value (U+DCFF)
        private static char[] s_myChars = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
        private static string s_myString = new String(s_myChars);
        private static byte[] s_leBytes = new byte[] { 0x7A, 0x00, 0x61, 0x00, 0x06, 0x03, 0xFD, 0x01, 0xB2, 0x03, 0xFF, 0xD8, 0xFF, 0xDC };
        private static byte[] s_beBytes = new byte[] { 0x00, 0x7A, 0x00, 0x61, 0x03, 0x06, 0x01, 0xFD, 0x03, 0xB2, 0xD8, 0xFF, 0xDC, 0xFF };
        private static byte[] s_utf8Bytes = new byte[] { 0x7A, 0x61, 0xCC, 0x86, 0xC7, 0xBD, 0xCE, 0xB2, 0xF1, 0x8F, 0xB3, 0xBF };

        [Fact]
        public void TestGetEncoding()
        {
            Encoding unicodeEnc = Encoding.Unicode;
            Encoding bigEndianEnc = Encoding.BigEndianUnicode;
            Encoding utf8Enc = Encoding.UTF8;

            TestEncoding(Encoding.BigEndianUnicode, 14, 16, s_beBytes);
            TestEncoding(Encoding.Unicode, 14, 16, s_leBytes);
            TestEncoding(Encoding.UTF8, 12, 24, s_utf8Bytes);

            unicodeEnc = Encoding.GetEncoding("utf-16");
            bigEndianEnc = Encoding.GetEncoding("utf-16BE");
            utf8Enc = Encoding.GetEncoding("utf-8");

            TestEncoding(Encoding.BigEndianUnicode, 14, 16, s_beBytes);
            TestEncoding(Encoding.Unicode, 14, 16, s_leBytes);
            TestEncoding(Encoding.UTF8, 12, 24, s_utf8Bytes);
        }

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

        private void TestEncoding(Encoding enc, int byteCount, int maxByteCount, byte[] bytes)
        {
            Assert.Equal(byteCount, enc.GetByteCount(s_myChars));
            Assert.Equal(maxByteCount, enc.GetMaxByteCount(s_myChars.Length));
            Assert.Equal(enc.GetBytes(s_myChars), bytes);
            Assert.Equal(enc.GetCharCount(bytes), s_myChars.Length);
            Assert.Equal(enc.GetChars(bytes), s_myChars);
            Assert.Equal(enc.GetString(bytes, 0, bytes.Length), s_myString);
            Assert.NotEqual(0, enc.GetHashCode());
        }
    }
}