// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

internal class BasicMath
{
    public static int Main(String[] Args)
    {
        int ec = 0;
        int x = 10;
        for (int i = 0; i <= 6; i++)
        {
            x += 5;
            x -= 5;
            x *= 5;
        }

        if (x == 781250)
        {
            Console.WriteLine("pass");
            Console.WriteLine(x);
            ec = 100;
        }

        if (x != 781250)
        {
            Console.WriteLine("fail");
            Console.WriteLine(x);
            ec = 1;
        }

        return ec;
    }
}
