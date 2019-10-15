// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

class BadMath
{
    public static double[,] Res = new double[2, 40];
    static int Main(string[] args)
    {

        double t0 = 1.5;
        int i = 0;
        for (i = 0; i < 4; i++)
        {
            double dd = t0 / 3;
            Res[0, i] = t0;
            Res[1, i] = dd;
            t0 -= dd;
            if (dd > 2)
            {
                break;
            }
        }

        for (int j = 0; (j < i); j++)
            Console.WriteLine(Res[0, j] + " " + Res[1, j]);

        Console.WriteLine();

        if (Res[0, 0] != 1.5)
        {
            Console.WriteLine("Res[0,0] is {0}", Res[0, 0]);
            Console.WriteLine("FAILED");
            return 1;
        }
        if (Res[1, 0] != 0.5)
        {
            Console.WriteLine("Res[1,0] is {0}", Res[1, 0]);
            Console.WriteLine("FAILED");
            return 1;
        }
        if (Res[0, 1] != 1.0)
        {
            Console.WriteLine("Res[0,1] is {0}", Res[0, 1]);
            Console.WriteLine("FAILED");
            return 1;
        }
        if ((Res[1, 1] - 0.333333333333333) > 0.000001)
        {
            Console.WriteLine("Res[1,1] is {0}", Res[1, 1]);
            Console.WriteLine("FAILED");
            return 1;
        }
        if ((Res[0, 2] - 0.666666666666667) > 0.000001)
        {
            Console.WriteLine("Res[0,2] is {0}", Res[0, 2]);
            Console.WriteLine("FAILED");
            return 1;
        }
        if ((Res[1, 2] - 0.222222222222222) > 0.000001)
        {
            Console.WriteLine("Res[1,2] is {0}", Res[1, 2]);
            Console.WriteLine("FAILED");
            return 1;
        }
        if ((Res[0, 3] - 0.444444444444445) > 0.000001)
        {
            Console.WriteLine("Res[0,3] is {0}", Res[0, 3]);
            Console.WriteLine("FAILED");
            return 1;
        }
        if ((Res[1, 3] - 0.148148148148148) > 0.000001)
        {
            Console.WriteLine("Res[1,3] is {0}", Res[1, 3]);
            Console.WriteLine("FAILED");
            return 1;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}
