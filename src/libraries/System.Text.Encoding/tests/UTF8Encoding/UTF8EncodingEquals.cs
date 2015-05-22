// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
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
