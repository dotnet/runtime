// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class test1
{

    public static double f1()
    {
        return 1.0;
    }

    public static void foo()
    {
        Console.Write(".");
    }

    public static int Main()
    {
        double c = 100.0;
        double a = f1();
        double b = f1();
        int x = 0;
        while (c > 0.0)
        {
            c = c * a;
            c = c - b;
            x++;
        }
        Console.WriteLine("\nx=" + x);
        return x;
    }
}