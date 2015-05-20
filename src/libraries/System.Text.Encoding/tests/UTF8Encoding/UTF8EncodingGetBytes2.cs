// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetBytes(System.String,System.Int32,System.Int32,System.Byte[],System.Int32)
    public class UTF8EncodingGetBytes2
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetBytes(Char[],Int32,Int32,Byte[],Int32) with non-null chars
        [Fact]
        public void PosTest1()
        {
            Byte[] bytes;
            String chars = "UTF8 Encoding Example";
            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = chars.Length;
            bytes = new Byte[byteCount];
            int bytesEncodedCount = utf8.GetBytes(chars, 1, 2, bytes, 0);
        }

        // PosTest2: Verify method GetBytes(String,Int32,Int32,Byte[],Int32) with null chars
        [Fact]
        public void PosTest2()
        {
            Byte[] bytes;
            String chars = "";

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = chars.Length;
            bytes = new Byte[byteCount];
            int bytesEncodedCount = utf8.GetBytes(chars, 0, byteCount, bytes, 0);
            Assert.Equal(0, bytesEncodedCount);
        }
        #endregion

        #region Negative Test Cases
        // NegTest1: ArgumentNullException is not thrown when string is a null reference
        [Fact]
        public void NegTest1()
        {
            Byte[] bytes;
            String chars = null;
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = 10;
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentNullException>(() =>
           {
               int bytesEncodedCount = utf8.GetBytes(chars, 1, 2, bytes, 0);
           });
        }

        // NegTest2: ArgumentNullException is not thrown when bytes is a null reference
        [Fact]
        public void NegTest2()
        {
            Byte[] bytes;
            String chars = "UTF8 Encoding Example";
            UTF8Encoding utf8 = new UTF8Encoding();
            bytes = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                int bytesEncodedCount = utf8.GetBytes(chars, 1, 2, bytes, 0);
            });
        }

        // NegTest3: ArgumentOutOfRangeException is not thrown when charIndex is less than zero
        [Fact]
        public void NegTest3()
        {
            Byte[] bytes;
            String chars = "UTF8 Encoding Example";
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = chars.Length;
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int bytesEncodedCount = utf8.GetBytes(chars, -1, 2, bytes, 0);
            });
        }

        // NegTest4: ArgumentOutOfRangeException is not thrown when charCount is less than zero
        [Fact]
        public void NegTest4()
        {
            Byte[] bytes;
            String chars = "UTF8 Encoding Example";

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = chars.Length;
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int bytesEncodedCount = utf8.GetBytes(chars, 1, -2, bytes, 0);
            });
        }

        // NegTest5: ArgumentOutOfRangeException is not thrown when byteIndex is less than zero
        [Fact]
        public void NegTest5()
        {
            Byte[] bytes;
            String chars = "UTF8 Encoding Example";
            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = chars.Length;
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int bytesEncodedCount = utf8.GetBytes(chars, 1, 2, bytes, -1);
            });
        }

        // NegTest6: ArgumentOutOfRangeException is not thrown when charIndex and charCount do not denote a valid range in chars
        [Fact]
        public void NegTest6()
        {
            Byte[] bytes;
            String chars = "UTF8 Encoding Example";
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = chars.Length;
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int bytesEncodedCount = utf8.GetBytes(chars, 1, chars.Length, bytes, 0);
            });
        }

        // NegTest7: ArgumentException is not thrown when bytes does not have enough capacity
        // from byteIndex to the end of the array to accommodate the resulting bytes
        [Fact]
        public void NegTest7()
        {
            Byte[] bytes;
            String chars = "UTF8 Encoding Example";

            UTF8Encoding utf8 = new UTF8Encoding();

            int byteCount = chars.Length;
            bytes = new Byte[byteCount];
            Assert.Throws<ArgumentException>(() =>
            {
                int bytesEncodedCount = utf8.GetBytes(chars, 1, 2, bytes, bytes.Length);
            });
        }
        #endregion
    }
}
