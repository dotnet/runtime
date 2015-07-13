// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberStylesHexNumber
    {
        // PosTest1: Verify value of NumberStyles.HexNumber
        [Fact]
        public void TestHexNumber()
        {
            int AllowLeadingWhite = 0x00000001,
                 AllowTrailingWhite = 0x00000002,
                 AllowHexSpecifier = 0x00000200;

            int expected = AllowLeadingWhite | AllowTrailingWhite | AllowHexSpecifier;
            int actual = (int)NumberStyles.HexNumber;

            Assert.Equal(expected, actual);
        }
    }
}
