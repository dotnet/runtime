// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeStylesAssumeLocal
    {
        // PosTest1:get DateTimeStyles.AssumeLocal
        [Fact]
        public void PosTest1()
        {
            UInt64 expectedValue = 0x00000020;
            UInt64 actualValue;
            actualValue = (UInt64)DateTimeStyles.AssumeLocal;
            Assert.Equal(actualValue, expectedValue);
        }
    }
}
