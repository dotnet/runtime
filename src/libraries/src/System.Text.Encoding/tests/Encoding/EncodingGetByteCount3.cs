// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetByteCount(System.String)
    public class EncodingGetByteCount3
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetByteCount(System.String)
        [Fact]
        public void PosTest1()
        {
            string testStr = "za\u0306\u01FD\u03B2\uD8ff\uDCFF";
            Encoding u8 = Encoding.UTF8;
            Encoding u16LE = Encoding.Unicode;
            Encoding u16BE = Encoding.BigEndianUnicode;

            Assert.Equal(12, u8.GetByteCount(testStr));
            Assert.Equal(14, u16LE.GetByteCount(testStr));
            Assert.Equal(14, u16BE.GetByteCount(testStr));
        }
        #endregion

        #region Negative Test Cases
        [Fact]
        public void NegTest1()
        {
            string testStr = null;
            Encoding u7 = Encoding.UTF8;
            Assert.Throws<ArgumentNullException>(() =>
            {
                int i = u7.GetByteCount(testStr);
            });
        }
        #endregion
    }
}
