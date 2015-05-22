// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingGetByteCount2
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetByteCount(System.Char[],System.Int32,System.Int32)
        [Fact]
        public void PosTest1()
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
            Encoding u8 = Encoding.UTF8;
            Encoding u16LE = Encoding.Unicode;
            Encoding u16BE = Encoding.BigEndianUnicode;
            Assert.Equal(6, u8.GetByteCount(testChar, 4, 3));
            Assert.Equal(6, u16LE.GetByteCount(testChar, 4, 3));
            Assert.Equal(6, u16BE.GetByteCount(testChar, 4, 3));
        }
        #endregion

        #region Nagetive Test Cases
        // NegTest1: ArgumentNullException is not thrown
        [Fact]
        public void NegTest1()
        {
            char[] testChar = null;
            Encoding u7 = Encoding.UTF8;
            Assert.Throws<ArgumentNullException>(() =>
            {
                int i = u7.GetByteCount(testChar, 1, 2);
            });
        }

        // NegTest2: ArgumentOutOfRangeException is not thrown
        [Fact]
        public void NegTest2()
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
            Encoding u7 = Encoding.UTF8;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int i = u7.GetByteCount(testChar, -1, 0);
            });
        }

        // NegTest3: ArgumentOutOfRangeException is not thrown
        [Fact]
        public void NegTest3()
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
            Encoding u7 = Encoding.UTF8;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int i = u7.GetByteCount(testChar, 0, -1);
            });
        }

        // NegTest4: ArgumentOutOfRangeException is not thrown
        [Fact]
        public void NegTest4()
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
            Encoding u7 = Encoding.UTF8;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int i = u7.GetByteCount(testChar, 0, testChar.Length + 1);
            });
        }
        #endregion
    }
}
