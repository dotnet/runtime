// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesAllowLeadingSign
    {
        // PosTest1: Verify value of NumberStyles.AllowLeadingSign
        [Fact]
        public void TestAllowLeadingSign()
        {
            int expected = 0x00000004;
            int actual = (int)NumberStyles.AllowLeadingSign;
            Assert.Equal(expected, actual);
        }
    }
}
