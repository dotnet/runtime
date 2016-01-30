// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeStylesAllowInnerWhite
    {
        // PosTest1:get DateTimeStyles.AllowInnerWhite
        [Fact]
        public void PosTest1()
        {
            UInt64 expectedValue = 0x00000004;
            UInt64 actualValue;
            actualValue = (UInt64)DateTimeStyles.AllowInnerWhite;
            Assert.Equal(expectedValue, actualValue);
        }
    }
}
