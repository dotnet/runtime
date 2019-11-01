// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace JitTest
{
    using System;

    class Test
    {
        static int Main()
        {
            ulong a = 0x0000000000000020;
            ulong b = 0xa697fcbfd6d232d1;
            try
            {
                ulong c = checked(a * b);
                Console.WriteLine("BAD! It should throw an exception!");
                return -1;
            }
            catch (OverflowException)
            {
                Console.WriteLine("GOOD.");
                return 100;
            }
        }
    }
}
