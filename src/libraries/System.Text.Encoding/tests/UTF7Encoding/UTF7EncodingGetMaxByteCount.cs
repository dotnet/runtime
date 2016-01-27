// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
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
