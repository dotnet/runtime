// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
