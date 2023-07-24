// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//simple and recursive call

using System;
using Xunit;

public class pow0
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool pass = true;
        double x, y, z;
        double n;

        y = 0;
        z = 0;
        for (n = 1; n < 500; n++)
            y += Math.Pow(n, 2);

        n = n - 1;

        z = n * (n + 1) * (2 * n + 1) / 6.0;
        if (y != z)
        {
            Console.WriteLine("n: {0}, y: {1}, z: {2}", n, y, z);
            pass = false;
        }

        x = 1;
        for (n = 1; n < 100000;)
        {
            n += Math.Pow(n, 0);
            if (n != ++x)
            {
                Console.WriteLine("n is {0}", n);
                pass = false;
            }
        }

        x = 2;
        for (n = 1; n < 20; n++)
            x *= Math.Pow(2, n);

        if (x != 3.1385508676933404E57)
        {
            Console.WriteLine("x is {0}, should be 3.1385508676933404E57", x);
            pass = false;
        }

        if (pass)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 1;
        }
    }
}

