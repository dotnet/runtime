// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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