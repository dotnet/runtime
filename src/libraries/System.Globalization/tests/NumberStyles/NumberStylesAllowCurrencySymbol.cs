// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesAllowCurrencySymbol
    {
        // PosTest1: Verify value of NumberStyles.AllowCurrencySymbol
        [Fact]
        public void TestAllowCurrencySymbol()
        {
            int expected = 0x00000100;
            int actual = (int)NumberStyles.AllowCurrencySymbol;
            Assert.Equal(expected, actual);
        }
    }
}
