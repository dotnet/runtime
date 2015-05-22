// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetBytes(System.String,System.Int32,System.Int32,System.Byte[],System.Int32)
    public class EncodingGetBytes5
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
            byte[] bytes = new byte[u8.GetMaxByteCount(3)];
            Assert.Equal(6, u8.GetBytes(c_TEST_STR, 4, 3, bytes, bytes.GetLowerBound(0)));
        }

        // PosTest2: Verify method GetBytes(System.String) with Unicode
        [Fact]
        public void PosTest2()
        {
            Encoding u16LE = Encoding.Unicode;
            byte[] bytes = new byte[u16LE.GetMaxByteCount(3)];
            Assert.Equal(6, u16LE.GetBytes(c_TEST_STR, 4, 3, bytes, bytes.GetLowerBound(0)));
        }

        // PosTest3: Verify method GetBytes(System.String) with BigEndianUnicode.
        [Fact]
        public void PosTest3()
        {
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];
            Assert.Equal(6, u16BE.GetBytes(c_TEST_STR, 4, 3, bytes, bytes.GetLowerBound(0)));
        }
        #endregion

        #region Negative Test Cases
        [Fact]
        public void NegTest1()
        {
            string testNullStr = null;
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];
            Assert.Throws<ArgumentNullException>(() =>
            {
                int i = u16BE.GetBytes(testNullStr, 4, 3, bytes, bytes.GetLowerBound(0));
            });
        }

        [Fact]
        public void NegTest2()
        {
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                int i = u16BE.GetBytes(c_TEST_STR, 4, 3, bytes, 1);
            });
        }

        [Fact]
        public void NegTest3()
        {
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int i = u16BE.GetBytes(c_TEST_STR, -1, 3, bytes, bytes.GetLowerBound(0));
            });
        }

        [Fact]
        public void NegTest4()
        {
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int i = u16BE.GetBytes(c_TEST_STR, 4, -1, bytes, bytes.GetLowerBound(0));
            });
        }

        [Fact]
        public void NegTest5()
        {
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int i = u16BE.GetBytes(c_TEST_STR, 4, 3, bytes, -1);
            });
        }

        [Fact]
        public void NegTest6()
        {
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int i = u16BE.GetBytes(c_TEST_STR, c_TEST_STR.Length - 1, 3, bytes, bytes.GetLowerBound(0));
            });
        }

        [Fact]
        public void NegTest7()
        {
            Encoding u16BE = Encoding.BigEndianUnicode;
            byte[] bytes = new byte[u16BE.GetMaxByteCount(3)];
            Assert.Throws<ArgumentException>(() =>
            {
                int i = u16BE.GetBytes(c_TEST_STR, 4, 3, bytes, bytes.Length - 1);
            });
        }
        #endregion
    }
}
