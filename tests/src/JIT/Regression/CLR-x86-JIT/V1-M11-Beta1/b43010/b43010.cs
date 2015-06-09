// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class App
    {
        static void Static1(ulong param2, object param3) { }

        static int Main()
        {
            ulong[] arr = new ulong[16];
            uint u = 11u;
            int i = 7;
            while (i == 0)
            {
                try
                {
                    Static1(arr[(int)u], (object)(205 + (150u * i)));
                }
                catch (Exception) { }
            }
            return 100;
        }
    }
}
