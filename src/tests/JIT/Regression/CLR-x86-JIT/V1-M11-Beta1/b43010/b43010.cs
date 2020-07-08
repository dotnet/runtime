// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
