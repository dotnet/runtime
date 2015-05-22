// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetMaxByteCount(System.Int32)
    public class UTF8EncodingGetMaxByteCount
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetMaxByteCount
        [Fact]
        public void PosTest1()
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            int charCount = 0;
            int maxByteCount = utf8.GetMaxByteCount(charCount);
        }
        #endregion

        #region Negative Test Cases
        // NegTest1: ArgumentOutOfRangeException is not thrown when charCount is less than zero
        [Fact]
        public void NegTest1()
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            int charCount = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int maxByteCount = utf8.GetMaxByteCount(charCount);
            });
        }
        #endregion
    }
}
