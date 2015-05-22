// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF7EncodingGetCharCount
    {
        // PosTest1: Verify method GetCharCount with a non-null Byte[]
        [Fact]
        public void PosTest1()
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};
            UTF7Encoding UTF7 = new UTF7Encoding();
            int charCount = UTF7.GetCharCount(bytes, 2, 8);
            Assert.Equal(8, charCount);
        }

        // PosTest2: Verify method GetCharCount with a null Byte[]
        [Fact]
        public void PosTest2()
        {
            Byte[] bytes = new Byte[] { };
            UTF7Encoding UTF7 = new UTF7Encoding();
            int charCount = UTF7.GetCharCount(bytes, 0, 0);
            Assert.Equal(0, charCount);
        }

        // NegTest1: ArgumentNullException is not thrown when bytes is a null reference
        [Fact]
        public void NegTest1()
        {
            Byte[] bytes = null;
            UTF7Encoding UTF7 = new UTF7Encoding();
            Assert.Throws<ArgumentNullException>(() =>
            {
                int charCount = UTF7.GetCharCount(bytes, 2, 8);
            });
        }

        // NegTest2: ArgumentOutOfRangeException is not thrown when index is less than zero.
        [Fact]
        public void NegTest2()
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};
            UTF7Encoding UTF7 = new UTF7Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charCount = UTF7.GetCharCount(bytes, -1, 8);
            });
        }

        // NegTest3: ArgumentOutOfRangeException is not thrown when count is less than zero.
        [Fact]
        public void NegTest3()
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};
            UTF7Encoding UTF7 = new UTF7Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charCount = UTF7.GetCharCount(bytes, 2, -1);
            });
        }

        // NegTest4: ArgumentOutOfRangeException is not thrown when index do not denote a valid range in bytes.
        [Fact]
        public void NegTest4()
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};
            UTF7Encoding UTF7 = new UTF7Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charCount = UTF7.GetCharCount(bytes, bytes.Length, 6);
            });
        }

        // NegTest5: ArgumentOutOfRangeException is not thrown when count do not denote a valid range in bytes.
        [Fact]
        public void NegTest5()
        {
            Byte[] bytes = new Byte[] {
                                         85,  84,  70,  56,  32,  69, 110,
                                         99, 111, 100, 105, 110, 103,  32,
                                         69, 120,  97, 109, 112, 108, 101};
            UTF7Encoding UTF7 = new UTF7Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int charCount = UTF7.GetCharCount(bytes, 2, bytes.Length);
            });
        }
    }
}
