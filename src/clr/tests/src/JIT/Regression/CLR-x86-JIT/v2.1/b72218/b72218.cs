// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;

class My
{

    static void Worker()
    {
        GC.Collect();
        Thread.Sleep(5);
    }

    static int Main()
    {

        Thread t = new Thread(new ThreadStart(Worker));
        t.Start();

        long x = 1;
        for (long i = 0; i < 100000; i++)
        {
            x *= i;
        }
        Console.WriteLine((object)x);

        return 100;
    }

}
