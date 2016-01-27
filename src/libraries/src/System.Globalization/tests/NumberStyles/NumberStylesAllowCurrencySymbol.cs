// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
