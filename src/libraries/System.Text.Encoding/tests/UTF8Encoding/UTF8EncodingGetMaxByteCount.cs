// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
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
