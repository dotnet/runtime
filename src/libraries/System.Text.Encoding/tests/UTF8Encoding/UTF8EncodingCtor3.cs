// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // ctor(System.Boolean,System.Boolean)
    public class UTF8EncodingCtor3
    {
        #region Positive Test Cases
        // PosTest1: Verify ctor(true,false) of UTF8Encoding
        [Fact]
        public void PosTest1()
        {
            UTF8Encoding utf8 = new UTF8Encoding(true, false);
            Assert.NotNull(utf8);
        }

        // PosTest2: Verify ctor(true,true) of UTF8Encoding
        [Fact]
        public void PosTest2()
        {
            UTF8Encoding utf8 = new UTF8Encoding(true, true);
            Assert.NotNull(utf8);
        }

        // PosTest3: Verify ctor(false,false) of UTF8Encoding
        [Fact]
        public void PosTest3()
        {
            UTF8Encoding utf8 = new UTF8Encoding(false, false);
            Assert.NotNull(utf8);
        }

        // PosTest4: Verify ctor(false,true) of UTF8Encoding
        [Fact]
        public void PosTest4()
        {
            UTF8Encoding utf8 = new UTF8Encoding(false, true);
            Assert.NotNull(utf8);
        }
        #endregion
    }
}
