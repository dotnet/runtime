// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
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
            Encoding u16LE = Encoding.Unicode;
            Encoding u16BE = Encoding.BigEndianUnicode;

            byte[] actualBytesUnicode = new byte[]{
                0x7A, 0x00, 0x61, 0x00, 0x06, 0x03,
                0xFD, 0x01, 0xB2, 0x03, 0xFF, 0xD8,
                0xFF, 0xDC};

            byte[] actualBytesBigEndianUnicode = new byte[]{
                0x00, 0x7A, 0x00, 0x61, 0x03, 0x06,
                0x01, 0xFD, 0x03, 0xB2, 0xD8, 0xFF,
                0xDC, 0xFF};
            VerifyByteItemValue(u16LE.GetBytes(testChar, 0, testChar.Length), actualBytesUnicode);
            VerifyByteItemValue(u16BE.GetBytes(testChar, 0, testChar.Length), actualBytesBigEndianUnicode);
        }

        // PosTest2: Verify method GetBytes when chars is null.
        [Fact]
        public void PosTest2()
        {
            char[] testChar = new char[0];
            Encoding u16LE = Encoding.Unicode;
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] result = new byte[0];
            
            VerifyByteItemValue(u16LE.GetBytes(testChar, 0, testChar.Length), result);
            VerifyByteItemValue(u16BE.GetBytes(testChar, 0, testChar.Length), result);
        }
        #endregion
        

        private void VerifyByteItemValue(byte[] getBytes, byte[] actualBytes)
        {
            Assert.Equal(getBytes.Length, actualBytes.Length);
            Assert.Equal(getBytes, actualBytes);
        }
    }
}
