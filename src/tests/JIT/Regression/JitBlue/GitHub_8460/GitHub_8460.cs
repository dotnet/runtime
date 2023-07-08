// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace bug
{
    public class Program
    {
        static int Pass = 100;
        static int Fail = -1;

        // This test is meant to check that in case of
        // GT_EQ/NE(shift, 0), JIT doesn't optimize out
        // 'test' instruction incorrectly, because shift
        // operations on xarch don't modify flags if the
        // shift count is zero.
        [Fact]
        public static int TestEntryPoint()
        {
            // Absolute bits
            int bitCount = 0;
            while ((0 != (100 >> bitCount)) && (31 > bitCount))
            {
                bitCount++;
            }
            // Sign bit
            bitCount++;

            if (bitCount != 8)
            {
                return Fail;
            }

            return Pass;
        }
    }
}
