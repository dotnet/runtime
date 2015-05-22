// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF8EncodingCtor
    {
        #region Positive Test Cases
        // PosTest1: Verify ctor of UTF8Encoding
        [Fact]
        public void PosTest1()
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.NotNull(utf8);
        }
        #endregion
    }
}
