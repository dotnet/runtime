// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // Encoding.GetChars(Byte[],Int32,Int32)
    public class EncodingGetChars2
    {
        #region PositiveTest
        [Fact]
        public void PosTest1()
        {
            byte[] bytes = new byte[0];
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            int startIndex = 0;
            int count = 0;
            char[] charsVal = myEncode.GetChars(bytes, startIndex, count);
            Assert.Equal(0, charsVal.Length);
        }

        [Fact]
        public void PosTest2()
        {
            string myStr = "za\u0306\u01fd\u03b2";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int startIndex = 0;
            int count = 0;
            char[] charsVal = myEncode.GetChars(bytes, startIndex, count);
            Assert.Equal(0, charsVal.Length);
        }

        [Fact]
        public void PosTest3()
        {
            string myStr = "za\u0306\u01fd\u03b2";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int startIndex = 0;
            int count = bytes.Length;
            char[] charsVal = myEncode.GetChars(bytes, startIndex, count);
            string strVal = new string(charsVal);
            Assert.Equal(myStr, strVal);
        }
        #endregion

        #region NegativeTest
        // NegTest1:The byte array is null
        [Fact]
        public void NegTest1()
        {
            byte[] bytes = null;
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            int startIndex = 0;
            int count = 0;
            Assert.Throws<ArgumentNullException>(() =>
            {
                char[] charsVal = myEncode.GetChars(bytes, startIndex, count);
            });
        }

        // NegTest2:The startIndex is less than zero
        [Fact]
        public void NegTest2()
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int startIndex = -1;
            int count = myStr.Length;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                char[] charsVal = myEncode.GetChars(bytes, startIndex, count);
            });
        }

        // NegTest3:The count is less than zero
        [Fact]
        public void NegTest3()
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int startIndex = 0;
            int count = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                char[] charsVal = myEncode.GetChars(bytes, startIndex, count);
            });
        }

        // NegTest4:The startIndex and count do not denote a valid range of bytes
        [Fact]
        public void NegTest4()
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int startIndex = 0;
            int count = bytes.Length + 1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                char[] charsVal = myEncode.GetChars(bytes, startIndex, count);
            });
        }
        #endregion
    }
}