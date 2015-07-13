// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesAllowTrailingWhite
    {
        // PosTest1: Verify value of NumberStyles.AllowTrailingWhite
        [Fact]
        public void TestAllowTrailingWhite()
        {
            int expected = 0x00000002;
            int actual = (int)NumberStyles.AllowTrailingWhite;
            Assert.Equal(expected, actual);
        }
    }
}
