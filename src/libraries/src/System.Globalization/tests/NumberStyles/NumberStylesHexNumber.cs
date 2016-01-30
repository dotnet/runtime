// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
