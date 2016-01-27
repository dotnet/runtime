// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingCtor2
    {
        #region Positive Test Cases
        // PosTest1: Verify ctor(true) of UTF8Encoding
        [Fact]
        public void PosTest1()
        {
            UTF8Encoding utf8 = new UTF8Encoding(true);
            Assert.NotNull(utf8);
        }

        // PosTest2: Verify ctor(false) of UTF8Encoding
        [Fact]
        public void PosTest2()
        {
            UTF8Encoding utf8 = new UTF8Encoding(false);
            Assert.NotNull(utf8);
        }
        #endregion
    }
}
