// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_s_ldc_div_signed_cs
{
    public class Test
    {
        private static bool check(long op1, long op2, long product, bool overflow)
        {
            Console.Write("Dividing {0} by {1}...", product, op1);
            try
            {
                if (product / op1 != op2)
                    return false;
                Console.WriteLine();
                return !overflow;
            }
            catch (DivideByZeroException)
            {
                Console.WriteLine("divide by zero.");
                return overflow;
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            if (!check(0x000000007fffffff, 0x000000007fffffff, 0x3fffffff00000001, false))
                goto fail;
            if (!check(0x0000000100000000, 0x000000007fffffff, 0x7fffffff00000000, false))
                goto fail;
            if (!check(0x3fffffffffffffff, 0x0000000000000002, 0x7ffffffffffffffe, false))
                goto fail;
            if (!check(unchecked((long)0xffffffffffffffff), unchecked((long)0xfffffffffffffffe), 2, false))
                goto fail;
            if (!check(0x0000000000100000, 0x0000001000000000, 0x0100000000000000, false))
                goto fail;
            if (!check(2, 0, 1, false))
                goto fail;
            if (!check(-2, 0, 1, false))
                goto fail;
            if (!check(unchecked((long)0xffffffffffffffff), unchecked((long)0x8000000000000001), 0x7fffffffffffffff, false))
                goto fail;
            if (!check(0, 0, 1000, true))
                goto fail;

            Console.WriteLine("Test passed");
            return 100;
        fail:
            Console.WriteLine("Test failed");
            return 1;
        }
    }
}
