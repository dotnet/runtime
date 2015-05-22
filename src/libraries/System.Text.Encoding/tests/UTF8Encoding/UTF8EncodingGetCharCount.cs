// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetByteCount(System.Char[],System.Int32,System.Int32)
    public class UTF8EncodingGetCharCount
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetByteCount with a non-null Byte[]
        [Fact]
        public void PosTest1()
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};

            UTF8Encoding utf8 = new UTF8Encoding();
            int charCount = utf8.GetCharCount(bytes, 2, 8);
            Assert.Equal(8, charCount);
        }

        // PosTest2: Verify method GetByteCount with a null Byte[]
        [Fact]
        public void PosTest2()
        {
            Byte[] bytes = new Byte[] { };
            UTF8Encoding utf8 = new UTF8Encoding();
            int charCount = utf8.GetCharCount(bytes, 0, 0);
            Assert.Equal(0, charCount);
        }
        #endregion

        #region Negative Test Cases
        // NegTest1: ArgumentNullException is not thrown when bytes is a null reference
        [Fact]
        public void NegTest1()
        {
            Byte[] bytes = null;
            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentNullException>(() =>
           {
               int charCount = utf8.GetCharCount(bytes, 2, 8);
           });
        }

        // NegTest2: ArgumentOutOfRangeException is not thrown when index is less than zero
        [Fact]
        public void NegTest2()
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};
            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charCount = utf8.GetCharCount(bytes, -1, 8);
            });
        }

        // NegTest3: ArgumentOutOfRangeException is not thrown when count is less than zero
        [Fact]
        public void NegTest3()
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};
            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charCount = utf8.GetCharCount(bytes, 2, -1);
            });
        }

        // NegTest4: ArgumentOutOfRangeException is not thrown when index and count do not denote a valid range in bytes
        [Fact]
        public void NegTest4()
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};
            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charCount = utf8.GetCharCount(bytes, 2, bytes.Length);
            });
        }
        #endregion
    }
}
