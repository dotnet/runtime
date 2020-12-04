// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal struct VC
{
    public int x;
    public int x2;

    public int x3;
}


internal class A
{
    public static int Main()
    {
        VC vc = new VC();
        vc.x = 5;

        return test(vc);
    }

    public static int test(VC vc)
    {
        if (vc.x == 5)
        {
            Console.WriteLine("PASS");
            return 100;
        }

        Console.WriteLine("FAIL");
        return 0;
    }
}
