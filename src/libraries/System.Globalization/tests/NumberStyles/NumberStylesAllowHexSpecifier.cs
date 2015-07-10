// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesAllowHexSpecifier
    {
        // PosTest1: Verify value of NumberStyles.AllowHexSpecifier 
        [Fact]
        public void TestAllowHexSpecifier()
        {
            int expected = 0x00000200;
            int actual = (int)NumberStyles.AllowHexSpecifier;
            Assert.Equal(expected, actual);
        }
    }
}
