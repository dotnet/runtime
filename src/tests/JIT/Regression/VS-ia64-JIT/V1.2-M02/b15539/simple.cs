// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

internal struct VC
{
    public int x;
    public int x2;

    public int x3;
}


public class A
{
    [Fact]
    public static int TestEntryPoint()
    {
        VC vc = new VC();
        vc.x = 5;

        return test(vc);
    }

    static int test(VC vc)
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
