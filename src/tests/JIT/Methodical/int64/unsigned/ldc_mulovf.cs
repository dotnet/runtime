// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_ldc_mulovf_unsigned_cs
{
    public class Test
    {
        private static bool check(ulong op1, ulong op2, ulong product, bool overflow)
        {
            Console.Write("Multiplying {0} and {1}...", op1, op2);
            try
            {
                if (checked(op1 * op2) != product)
                    return false;
                Console.WriteLine();
                return !overflow;
            }
            catch (OverflowException)
            {
                Console.WriteLine("overflow.");
                return overflow;
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            if (!check(0x00000000ffffffff, 0x00000000ffffffff, 0xfffffffe00000001, false))
                goto fail;
            if (!check(0x0000000100000000, 0x00000000ffffffff, 0xffffffff00000000, false))
                goto fail;
            if (!check(0x0000000100000000, 0x0000000100000000, 0x0000000000000000, true))
                goto fail;
            if (!check(0x7fffffffffffffff, 0x0000000000000002, 0xfffffffffffffffe, false))
                goto fail;
            if (!check(0x8000000000000000, 0x0000000000000002, 0x0000000000000000, true))
                goto fail;
            if (!check(0x0000000000100000, 0x0000001000000000, 0x0100000000000000, false))
                goto fail;

            Console.WriteLine("Test passed");
            return 100;
        fail:
            Console.WriteLine("Test failed");
            return 1;
        }
    }
}
