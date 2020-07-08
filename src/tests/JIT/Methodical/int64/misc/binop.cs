// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace JitTest
{
    internal class Test
    {
        private static int Main()
        {
            long L1, L2;
            ulong U1, U2;
            try
            {
                L1 = 0x7000123480001234;
                L2 = 0x7123400081234000;
                if ((L1 & L2) != 0x7000000080000000)
                    goto fail;

                L1 = 0x7000123480001234;
                L2 = 0x7123400081234000;
                if ((L1 | L2) != 0x7123523481235234)
                    goto fail;

                U1 = 0x8000123480001234;
                U2 = 0x8123400081234000;
                if (~(U1 & U2) != 0x7fffffff7fffffff)
                    goto fail;

                U1 = 0x8000123480001234;
                U2 = 0x8123400081234000;
                if ((U1 | U2) != 0x8123523481235234)
                    goto fail;
            }
            catch (Exception)
            {
                Console.WriteLine("Exception handled!");
                goto fail;
            }
            Console.WriteLine("Passed");
            return 100;
        fail:
            Console.WriteLine("Failed");
            return 1;
        }
    }
}
