// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesAllowTrailingSign
    {
        // PosTest1: Verify value of NumberStyles.AllowTrailingSign
        [Fact]
        public void TestAllowTrailingSign()
        {
            int expected = 0x00000008;
            int actual = (int)NumberStyles.AllowTrailingSign;
            Assert.Equal(expected, actual);
        }
    }
}
