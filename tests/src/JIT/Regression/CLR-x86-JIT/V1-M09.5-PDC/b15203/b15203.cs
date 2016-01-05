// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


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
