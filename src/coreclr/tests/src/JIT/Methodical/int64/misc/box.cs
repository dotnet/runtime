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
            try
            {
                ulong L1 = 0x8000123480001234;
                if (L1 != (ulong)(object)(ulong)(object)(ulong)(object)L1)
                    goto fail;
                long L2 = unchecked((long)0x8000123480001234);
                if (L2 != (long)(object)(long)(object)(long)(object)L2)
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
