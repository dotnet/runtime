// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF7EncodingGetString
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: start index is zero and count of bytes decoded equals whole length of bytes array
        [Fact]
        public void PosTest1()
        {
            Byte[] bytes = new Byte[] {
                             85,  84,  70,  56,  32,  69, 110,
                             99, 111, 100, 105, 110, 103,  32,
                             69, 120,  97, 109, 112, 108, 101};
            UTF7Encoding utf7 = new UTF7Encoding();
            string str = utf7.GetString(bytes, 0, bytes.Length);
        }

        // PosTest2: start index and count of bytes decoded are both random valid value
        [Fact]
        public void PosTest2()
        {
            int startIndex = 0;
            int count = 0;
            Byte[] bytes = new Byte[] {
                             85,  84,  70,  56,  32,  69, 110,
                             99, 111, 100, 105, 110, 103,  32,
                             69, 120,  97, 109, 112, 108, 101};
            startIndex = _generator.GetInt32(-55) % bytes.Length;
            count = _generator.GetInt32(-55) % (bytes.Length - startIndex) + 1;
            UTF7Encoding utf7 = new UTF7Encoding();
            string str = utf7.GetString(bytes, startIndex, count);
        }

        // NegTest1: ArgumentNullException is not thrown when bytes is a null reference
        [Fact]
        public void NegTest1()
        {
            Byte[] bytes = null;
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.Throws<ArgumentNullException>(() =>
            {
                string str = utf7.GetString(bytes, 0, 2);
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
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                string str = utf7.GetString(bytes, -1, bytes.Length);
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
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                string str = utf7.GetString(bytes, 0, -1);
            });
        }

        // NegTest4: ArgumentOutOfRangeException is not thrown when index and count do not denote a valid range in bytes.
        [Fact]
        public void NegTest4()
        {
            Byte[] bytes = new Byte[] {
                             85,  84,  70,  56,  32,  69, 110,
                             99, 111, 100, 105, 110, 103,  32,
                             69, 120,  97, 109, 112, 108, 101};
            UTF7Encoding utf7 = new UTF7Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                string str = utf7.GetString(bytes, 1, bytes.Length);
            });
        }
    }
}
