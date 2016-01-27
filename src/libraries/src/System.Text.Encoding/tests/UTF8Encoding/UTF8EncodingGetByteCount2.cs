// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    // GetByteCount(System.String)
    public class UTF8EncodingGetByteCount2
    {
        #region Positive Test Cases
        // PosTest1: Verify method GetByteCount(string) with non-null string
        [Fact]
        public void PosTest1()
        {
            String chars = "UTF8 Encoding Example";
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars);
            Assert.Equal(chars.Length, byteCount);
        }

        // PosTest2: Verify method GetByteCount(string) with null string
        [Fact]
        public void PosTest2()
        {
            String chars = "";
            UTF8Encoding utf8 = new UTF8Encoding();
            int byteCount = utf8.GetByteCount(chars);
            Assert.Equal(0, byteCount);
        }
        #endregion

        #region Negative Test Cases
        // NegTest1: ArgumentNullException is not thrown when string is a null reference
        [Fact]
        public void NegTest1()
        {
            String chars = null;
            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentNullException>(() =>
            {
                int byteCount = utf8.GetByteCount(chars);
            });
        }
        #endregion
    }
}
