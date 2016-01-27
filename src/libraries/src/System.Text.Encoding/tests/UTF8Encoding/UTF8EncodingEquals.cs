// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    // Equals(System.Object)
    public class UTF8EncodingEquals
    {
        #region Positive Test Cases
        // PosTest1: Verify a instance of UTF8Encoding equals to itself
        [Fact]
        public void PosTest1()
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.True(utf8.Equals(utf8));
        }

        // PosTest2: Verify a instance of UTF8Encoding equals to another one
        [Fact]
        public void PosTest2()
        {
            UTF8Encoding utf8a = new UTF8Encoding();
            UTF8Encoding utf8b = new UTF8Encoding();
            Assert.True(utf8a.Equals(utf8b));
        }

        // PosTest3: Verify a instance of UTF8Encoding is not equal to different one
        [Fact]
        public void PosTest3()
        {
            UTF8Encoding utf8a = new UTF8Encoding();
            UTF8Encoding utf8b = new UTF8Encoding(true);
            Assert.False(utf8a.Equals(utf8b));
        }
        #endregion
    }
}
