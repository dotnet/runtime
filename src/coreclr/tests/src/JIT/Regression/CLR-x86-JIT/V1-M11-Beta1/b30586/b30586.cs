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
            try
            {
                Int16 foo = 0;
                for (int i = 0; i < 5; i++)
                {
                    checked { foo += 32000; }
                    Console.WriteLine("foo=" + foo);
                }
            }
            catch (OverflowException) { return 100; }
            return 1;
        }
    }
}
