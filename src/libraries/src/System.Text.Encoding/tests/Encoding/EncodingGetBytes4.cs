// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetBytes(System.String)
    public class EncodingGetBytes4
    {
        #region Private Fields
        private const string c_TEST_STR = "za\u0306\u01FD\u03B2\uD8FF\uDCFF";
        #endregion

        #region Positive Test Cases 
        // PosTest1: Verify method GetBytes(System.String) with UTF8.
        [Fact]
        public void PosTest1()
        {
            Encoding u8 = Encoding.UTF8;

            byte[] actualBytesUTF8 = new byte[] {
                0x7A, 0x61, 0xCC ,0x86, 0xC7 ,0xBD,
                0xCE ,0xB2 ,0xF1, 0x8F ,0xB3 ,0xBF};
            VerifyByteItemValue(u8.GetBytes(c_TEST_STR), actualBytesUTF8);
        }

        // PosTest2: Verify method GetBytes(System.String) with Unicode.
        [Fact]
        public void PosTest2()
        {
            Encoding u16LE = Encoding.Unicode;
            byte[] actualBytesUnicode = new byte[]{
                0x7A, 0x00, 0x61, 0x00, 0x06, 0x03,
                0xFD, 0x01, 0xB2, 0x03, 0xFF, 0xD8,
                0xFF, 0xDC};
            VerifyByteItemValue(u16LE.GetBytes(c_TEST_STR), actualBytesUnicode);
        }

        // PosTest3: Verify method GetBytes(System.String) with BigEndianUnicode.
        [Fact]
        public void PosTest3()
        {
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] actualBytesBigEndianUnicode = new byte[]{
                0x00, 0x7A, 0x00, 0x61, 0x03, 0x06,
                0x01, 0xFD, 0x03, 0xB2, 0xD8, 0xFF,
                0xDC, 0xFF};
            VerifyByteItemValue(u16BE.GetBytes(c_TEST_STR), actualBytesBigEndianUnicode);
        }
        #endregion

        #region Negative Test Cases
        [Fact]
        public void NegTest1()
        {
            String testStr = null;
            Encoding u16BE = Encoding.BigEndianUnicode;
            Assert.Throws<ArgumentNullException>(() =>
            {
                byte[] getBytes = u16BE.GetBytes(testStr);
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
