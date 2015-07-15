// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // Encoding.GetChars(byte[],Int32,Int32,char[],Int32)
    public class EncodingGetChars3
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        #region PositiveTest
        [Fact]
        public void PosTest1()
        {
            byte[] bytes = new byte[0];
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            int byteIndex = 0;
            int bytecount = 0;
            char[] chars = new char[] { _generator.GetChar(-55) };
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            Assert.Equal(0, intVal);
            Assert.Equal(1, chars.Length);
        }

        [Fact]
        public void PosTest2()
        {
            string myStr = "za\u0306\u01fd\u03b2";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = 0;
            char[] chars = new char[] { _generator.GetChar(-55) };
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            Assert.Equal(0, intVal);
            Assert.Equal(1, chars.Length);
        }

        [Fact]
        public void PosTest3()
        {
            string myStr = "za\u0306\u01fd\u03b2";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length;
            char[] chars = new char[myStr.Length];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            Assert.Equal(myStr.Length, intVal);
            Assert.Equal(myStr.Length, chars.Length);
        }

        [Fact]
        public void PosTest4()
        {
            string myStr = "za\u0306\u01fd\u03b2";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length;
            char[] chars = new char[myStr.Length + myStr.Length];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, myStr.Length - 1);
            string subchars = null;
            for (int i = 0; i < myStr.Length - 1; i++)
            {
                subchars += chars[i].ToString();
            }
            Assert.Equal(myStr.Length, intVal);
            Assert.Equal("\0\0\0\0", subchars);
        }

        [Fact]
        public void PosTest5()
        {
            string myStr = "za\u0306\u01fd\u03b2";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length - 2;
            char[] chars = new char[myStr.Length - 1];
            int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            string strVal = new string(chars);
            Assert.Equal((myStr.Length - 1), intVal);
            Assert.Equal("za\u0306\u01fd", strVal);
        }
        #endregion
        #region NegativeTest
        // NegTest1:the byte array is null
        [Fact]
        public void NegTest1()
        {
            byte[] bytes = null;
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            int byteIndex = 0;
            int bytecount = 0;
            char[] chars = new char[0];
            Assert.Throws<ArgumentNullException>(() =>
            {
                int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            });
        }

        // NegTest2:the char array is null
        [Fact]
        public void NegTest2()
        {
            byte[] bytes = new byte[0];
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            int byteIndex = 0;
            int bytecount = 0;
            char[] chars = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            });
        }

        // NegTest3:the char array has no enough capacity to hold the chars
        [Fact]
        public void NegTest3()
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length;
            char[] chars = new char[0];
            Assert.Throws<ArgumentException>(() =>
            {
                int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            });
        }

        // NegTest4:the byteIndex is less than zero
        [Fact]
        public void NegTest4()
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = -1;
            int bytecount = bytes.Length;
            char[] chars = new char[myStr.Length];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            });
        }

        // NegTest5:the bytecount is less than zero
        [Fact]
        public void NegTest5()
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = -1;
            char[] chars = new char[myStr.Length];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, 0);
            });
        }

        // NegTest6:the charIndex is less than zero
        [Fact]
        public void NegTest6()
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length;
            char[] chars = new char[myStr.Length];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
           {
               int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, -1);
           });
        }

        // NegTest7:the charIndex is not valid index in chars array
        [Fact]
        public void NegTest7()
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length;
            char[] chars = new char[myStr.Length];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
           {
               int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, myStr.Length + 1);
           });
        }

        // NegTest8:the byteIndex and bytecount do not denote valid range of the bytes array
        [Fact]
        public void NegTest8()
        {
            string myStr = "helloworld";
            Encoding myEncode = Encoding.GetEncoding("utf-16");
            byte[] bytes = myEncode.GetBytes(myStr);
            int byteIndex = 0;
            int bytecount = bytes.Length + 1;
            char[] chars = new char[myStr.Length];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
           {
               int intVal = myEncode.GetChars(bytes, byteIndex, bytecount, chars, myStr.Length + 1);
           });
        }
        #endregion
    }
}
