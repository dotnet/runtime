// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
