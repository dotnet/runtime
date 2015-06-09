// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace DefaultNamespace
{
    using System;
    class JITcrash
    {
        public
        static
        int Main(String[] args)
        {
            UInt32 x = (0xFFFFFFFF);
            Int64 y = x;

            //	just added few cases of WriteLine
            Console.WriteLine("Running test");
            Console.WriteLine("x = " + x);
            Console.WriteLine("x = " + x + ".");
            Console.WriteLine("x = " + x + " y = " + y + ".");
            Console.WriteLine("Test passed.");
            return 100;
        }
    }
}
