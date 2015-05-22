// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetBytes(System.Char[],System.Int32,System.Int32)
    public class EncodingGetBytes2
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetBytes.
        [Fact]
        public void PosTest1()
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
            Encoding u8 = Encoding.UTF8;
            Encoding u16LE = Encoding.Unicode;
            Encoding u16BE = Encoding.BigEndianUnicode;

            byte[] actualBytesUTF8 = new byte[] {
                0x7A, 0x61, 0xCC ,0x86, 0xC7 ,0xBD,
                0xCE ,0xB2 ,0xF1, 0x8F ,0xB3 ,0xBF};

            byte[] actualBytesUnicode = new byte[]{
                0x7A, 0x00, 0x61, 0x00, 0x06, 0x03,
                0xFD, 0x01, 0xB2, 0x03, 0xFF, 0xD8,
                0xFF, 0xDC};

            byte[] actualBytesBigEndianUnicode = new byte[]{
                0x00, 0x7A, 0x00, 0x61, 0x03, 0x06,
                0x01, 0xFD, 0x03, 0xB2, 0xD8, 0xFF,
                0xDC, 0xFF};
            VerifyByteItemValue(u8.GetBytes(testChar, 0, testChar.Length), actualBytesUTF8);
            VerifyByteItemValue(u16LE.GetBytes(testChar, 0, testChar.Length), actualBytesUnicode);
            VerifyByteItemValue(u16BE.GetBytes(testChar, 0, testChar.Length), actualBytesBigEndianUnicode);
        }

        // PosTest2: Verify method GetBytes when chars is null.
        [Fact]
        public void PosTest2()
        {
            char[] testChar = new char[0];
            Encoding u8 = Encoding.UTF8;
            Encoding u16LE = Encoding.Unicode;
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] result = new byte[0];

            VerifyByteItemValue(u8.GetBytes(testChar, 0, testChar.Length), result);
            VerifyByteItemValue(u16LE.GetBytes(testChar, 0, testChar.Length), result);
            VerifyByteItemValue(u16BE.GetBytes(testChar, 0, testChar.Length), result);
        }
        #endregion

        #region Nagetive Test Cases
        [Fact]
        public void NegTest1()
        {
            char[] testChar = null;
            Encoding u7 = Encoding.UTF8;
            Assert.Throws<ArgumentNullException>(() =>
           {
               byte[] result = u7.GetBytes(testChar, 2, 1);
           });
        }

        [Fact]
        public void NegTest2()
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
            Encoding u7 = Encoding.UTF8;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                byte[] result = u7.GetBytes(testChar, -1, 1);
            });
        }

        [Fact]
        public void NegTest3()
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
            Encoding u7 = Encoding.UTF8;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                byte[] result = u7.GetBytes(testChar, 0, -1);
            });
        }

        [Fact]
        public void NegTest4()
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
            Encoding u7 = Encoding.UTF8;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                byte[] result = u7.GetBytes(testChar, 0, testChar.Length + 1);
            });
        }
        #endregion

        private void VerifyByteItemValue(byte[] getBytes, byte[] actualBytes)
        {
            Assert.Equal(getBytes.Length, actualBytes.Length);
            Assert.Equal(getBytes, actualBytes);
        }
    }
}
