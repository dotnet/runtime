// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
