// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace JitTest
{
    internal class Test
    {
        private long _op1, _op2;
        private bool check(long product, bool overflow)
        {
            Console.Write("Multiplying {0} and {1}...", _op1, _op2);
            try
            {
                if (unchecked(_op1 * _op2) != product)
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
            Test app = new Test();
            app._op1 = 0x000000007fffffff;
            app._op2 = 0x000000007fffffff;
            if (!app.check(0x3fffffff00000001, false))
                goto fail;
            app._op1 = 0x0000000100000000;
            app._op2 = 0x000000007fffffff;
            if (!app.check(0x7fffffff00000000, false))
                goto fail;
            app._op1 = 0x0000000100000000;
            app._op2 = 0x0000000100000000;
            if (!app.check(0x0000000000000000, false))
                goto fail;
            app._op1 = 0x3fffffffffffffff;
            app._op2 = 0x0000000000000002;
            if (!app.check(0x7ffffffffffffffe, false))
                goto fail;
            app._op1 = unchecked((long)0xffffffffffffffff);
            app._op2 = unchecked((long)0xfffffffffffffffe);
            if (!app.check(2, false))
                goto fail;
            app._op1 = 0x0000000000100000;
            app._op2 = 0x0000001000000000;
            if (!app.check(0x0100000000000000, false))
                goto fail;
            app._op1 = unchecked((long)0xffffffffffffffff);
            app._op2 = unchecked((long)0x8000000000000001);
            if (!app.check(0x7fffffffffffffff, false))
                goto fail;
            app._op1 = unchecked((long)0xfffffffffffffffe);
            app._op2 = unchecked((long)0x8000000000000001);
            if (!app.check(unchecked((long)0xfffffffffffffffe), false))
                goto fail;
            app._op1 = 2;
            app._op2 = unchecked((long)0x8000000000000001);
            if (!app.check(2, false))
                goto fail;

            Console.WriteLine("Test passed");
            return 100;
        fail:
            Console.WriteLine("Test failed");
            return 1;
        }
    }
}
