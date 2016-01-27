// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
