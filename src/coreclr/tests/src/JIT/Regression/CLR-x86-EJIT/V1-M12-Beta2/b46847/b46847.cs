// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class App
    {
        static int Main()
        {
            ulong a = 0x00000000005d909c;
            ulong b = 0x00004021bfa15862;
            try
            {
                ulong ab = checked(a * b);
                Console.WriteLine("BAD - it must throw an exception");
                return 1;
            }
            catch (OverflowException)
            {
                Console.WriteLine("GOOD");
                return 100;
            }
        }
    }
}
