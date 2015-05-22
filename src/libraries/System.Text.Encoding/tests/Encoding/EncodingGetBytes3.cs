// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32)
    public class EncodingGetBytes3
    {
        #region Private Fileds
        private char[] _testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
        #endregion

        #region Positive Test Cases
        // PosTest1: Verify method GetBytes(System.Char[],System.Int32,System.Int32,System.Byte[],System.Int32) with UTF8.
        [Fact]
        public void PosTest1()
        {
            Encoding u8 = Encoding.UTF8;

            byte[] u8Bytes = u8.GetBytes(_testChar, 4, 3);
            int u8ByteIndex = u8Bytes.GetLowerBound(0);
            Assert.Equal(6, u8.GetBytes(_testChar, 4, 3, u8Bytes, u8ByteIndex));
        }

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

        #region Negative Test Cases
        [Fact]
        public void NegTest1()
        {
            char[] testNullChar = null;
            Encoding u7 = Encoding.UTF8;
            byte[] u7Bytes = u7.GetBytes(_testChar, 4, 3);
            int u7ByteIndex = u7Bytes.GetLowerBound(0);
            Assert.Throws<ArgumentNullException>(() =>
            {
                int result = u7.GetBytes(testNullChar, 4, 3, u7Bytes, u7ByteIndex);
            });
        }

        [Fact]
        public void NegTest2()
        {
            Encoding u7 = Encoding.UTF8;
            byte[] u7Bytes = null;
            int u7ByteIndex = 1;
            Assert.Throws<ArgumentNullException>(() =>
            {
                int result = u7.GetBytes(_testChar, 4, 3, u7Bytes, u7ByteIndex);
            });
        }

        [Fact]
        public void NegTest3()
        {
            Encoding u7 = Encoding.UTF8;
            byte[] u7Bytes = u7.GetBytes(_testChar, 4, 3);
            int u7ByteIndex = u7Bytes.GetLowerBound(0);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int result = u7.GetBytes(_testChar, -1, 3, u7Bytes, u7ByteIndex);
            });
        }

        [Fact]
        public void NegTest4()
        {
            Encoding u7 = Encoding.UTF8;
            byte[] u7Bytes = u7.GetBytes(_testChar, 4, 3);
            int u7ByteIndex = u7Bytes.GetLowerBound(0);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int result = u7.GetBytes(_testChar, 4, -1, u7Bytes, u7ByteIndex);
            });
        }

        [Fact]
        public void NegTest5()
        {
            Encoding u7 = Encoding.UTF8;
            byte[] u7Bytes = u7.GetBytes(_testChar, 4, 3);
            int u7ByteIndex = u7Bytes.GetLowerBound(0);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int result = u7.GetBytes(_testChar, _testChar.Length - 1, 3, u7Bytes, u7ByteIndex);
            });
        }

        [Fact]
        public void NegTest6()
        {
            Encoding u7 = Encoding.UTF8;
            byte[] u7Bytes = u7.GetBytes(_testChar, 4, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int result = u7.GetBytes(_testChar, 4, 3, u7Bytes, -1);
            });
        }

        [Fact]
        public void NegTest7()
        {
            Encoding u7 = Encoding.UTF8;
            byte[] u7Bytes = u7.GetBytes(_testChar, 4, 3);
            int u7ByteIndex = u7Bytes.GetLowerBound(0);
            Assert.Throws<ArgumentException>(() =>
            {
                int result = u7.GetBytes(_testChar, 4, 3, u7Bytes, u7Bytes.Length - 1);
            });
        }
        #endregion
    }
}
