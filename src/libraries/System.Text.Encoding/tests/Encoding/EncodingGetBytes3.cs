// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    // GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32)
    public class EncodingGetBytes3
    {
        #region Private Fields
        private char[] _testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
        #endregion

        #region Positive Test Cases

        // PosTest2: Verify method GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32) with Unicode.
        [Fact]
        public void PosTest2()
        {
            Encoding u16LE = Encoding.Unicode;
            byte[] u16LEBytes = u16LE.GetBytes(_testChar, 4, 3);
            int u16LEByteIndex = u16LEBytes.GetLowerBound(0);
            Assert.Equal(6, u16LE.GetBytes(_testChar, 4, 3, u16LEBytes, u16LEByteIndex));
        }

        // PosTest3: Verify method GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32) with BigEndianUnicode.
        [Fact]
        public void PosTest3()
        {
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] u16BEBytes = u16BE.GetBytes(_testChar, 4, 3);
            int u16BEByteIndex = u16BEBytes.GetLowerBound(0);
            Assert.Equal(6, u16BE.GetBytes(_testChar, 4, 3, u16BEBytes, u16BEByteIndex));
        }
        #endregion
    }
}
