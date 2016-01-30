// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
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
