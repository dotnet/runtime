// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesAllowThousands
    {
        // PosTest1: Verify value of NumberStyles.AllowThousands
        [Fact]
        public void TestAllowThousands()
        {
            int expected = 0x00000040;
            int actual = (int)NumberStyles.AllowThousands;
            Assert.Equal(expected, actual);
        }
    }
}
