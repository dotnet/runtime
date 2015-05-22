// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF7EncodingGetMaxByteCount
    {
        // PosTest1: Verify method GetMaxByteCount using 0
        [Fact]
        public void PosTest1()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            int charCount = 0;
            int maxByteCount = utf7.GetMaxByteCount(charCount);
        }

        // PosTest2: Verify method GetMaxByteCount using an integer
        [Fact]
        public void PosTest2()
        {
            int charCount = 8;
            UTF7Encoding utf7 = new UTF7Encoding();
            int maxByteCount = utf7.GetMaxByteCount(charCount);
        }

        // NegTest1: ArgumentOutOfRangeException is not thrown when charCount is less than zero.
        [Fact]
        public void NegTest1()
        {
            UTF7Encoding utf7 = new UTF7Encoding();
            int charCount = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int maxByteCount = utf7.GetMaxByteCount(charCount);
            });
        }
    }
}
