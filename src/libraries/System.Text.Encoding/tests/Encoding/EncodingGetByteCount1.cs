// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingGetByteCount1
    {
        #region Positive Test Cases
        // PosTest1: Verify method  GetByteCount(System.Char[])
        [Fact]
        public void PosTest1()
        {
            char[] testChar = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
            Encoding u8 = Encoding.UTF8;
            Encoding u16LE = Encoding.Unicode;
            Encoding u16BE = Encoding.BigEndianUnicode;
            Assert.Equal(12, u8.GetByteCount(testChar));
            Assert.Equal(14, u16LE.GetByteCount(testChar));
            Assert.Equal(14, u16BE.GetByteCount(testChar));
        }
        #endregion

        #region Nagetive Test Cases
        // NegTest1: ArgumentNullException is not thrown.
        [Fact]
        public void NegTest1()
        {
            char[] testChar = null;
            Encoding u7 = Encoding.UTF8;
            Assert.Throws<ArgumentNullException>(() =>
            {
                int i = u7.GetByteCount(testChar);
            });
        }
        #endregion
    }
}
