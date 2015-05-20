// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetMaxCharCount(System.Int32)
    public class UTF8EncodingGetMaxCharCount
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetMaxCharCount
        [Fact]
        public void PosTest1()
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = 8;
            int maxCharCount = utf8.GetMaxCharCount(byteCount);
        }
        #endregion

        #region Negative Test Cases
        // NegTest1: ArgumentOutOfRangeException is not thrown when byteCount is less than zero
        [Fact]
        public void NegTest1()
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                int maxCharCount = utf8.GetMaxCharCount(byteCount);
            });
        }
        #endregion
    }
}
