// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace JitTest
{
    internal class Test
    {
        private static ulong s_op1,s_op2;

        private static bool check(ulong product, bool overflow)
        {
            Console.Write("Multiplying {0} and {1}...", s_op1, s_op2);
            try
            {
                if (checked(s_op1 * s_op2) != product)
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

        private static int Main()
        {
            s_op1 = 0x00000000ffffffff;
            s_op2 = 0x00000000ffffffff;
            if (!check(0xfffffffe00000001, false))
                goto fail;
            s_op1 = 0x0000000100000000;
            s_op2 = 0x00000000ffffffff;
            if (!check(0xffffffff00000000, false))
                goto fail;
            s_op1 = 0x0000000100000000;
            s_op2 = 0x0000000100000000;
            if (!check(0x0000000000000000, true))
                goto fail;
            s_op1 = 0x7fffffffffffffff;
            s_op2 = 0x0000000000000002;
            if (!check(0xfffffffffffffffe, false))
                goto fail;
            s_op1 = 0x8000000000000000;
            s_op2 = 0x0000000000000002;
            if (!check(0x0000000000000000, true))
                goto fail;
            s_op1 = 0x0000000000100000;
            s_op2 = 0x0000001000000000;
            if (!check(0x0100000000000000, false))
                goto fail;

            Console.WriteLine("Test passed");
            return 100;
        fail:
            Console.WriteLine("Test failed");
            return 1;
        }
    }
}
