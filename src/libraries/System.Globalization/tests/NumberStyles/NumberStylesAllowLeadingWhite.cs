// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesAllowLeadingWhite
    {
        // PosTest1: Verify value of NumberStyles.AllowLeadingWhite
        [Fact]
        public void TestAllowLeadingWhite()
        {
            int expected = 0x00000001;
            int actual = (int)NumberStyles.AllowLeadingWhite;
            Assert.Equal(expected, actual);
        }
    }
}
