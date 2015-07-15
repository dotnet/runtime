// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF7EncodingGetMaxCharCount
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: Verify method GetMaxCharCount using random integer
        [Fact]
        public void PosTest1()
        {
            int byteCount = _generator.GetInt32(-55);
            UTF7Encoding utf7 = new UTF7Encoding();
            int maxCharCount = utf7.GetMaxCharCount(byteCount);
        }

        // PosTest2: Verify method GetMaxCharCount using 0
        [Fact]
        public void PosTest2()
        {
            int byteCount = 0;
            UTF7Encoding utf7 = new UTF7Encoding();
            int maxCharCount = utf7.GetMaxCharCount(byteCount);
        }

        // PosTest2: Verify method GetMaxCharCount using Int32.MaxValue
        [Fact]
        public void PosTest3()
        {
            int byteCount = Int32.MaxValue;
            UTF7Encoding utf7 = new UTF7Encoding();
            int maxCharCount = utf7.GetMaxCharCount(byteCount);
        }

        // NegTest1: ArgumentOutOfRangeException is not thrown when byteCount is less than zero.
        [Fact]
        public void NegTest1()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            int byteCount = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int maxCharCount = utf7.GetMaxCharCount(byteCount);
            });
        }
    }
}
