// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
