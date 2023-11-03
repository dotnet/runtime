// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Simple dev unit test for constant propagation assertion.

using System;
using Xunit;

public class Sample1
{
    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int func(int a)
    {
        int x, y;
        x = 5;
        y = a;
        if (a != 1)
            return x;
        else
            return y;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        bool failed = false;
        if (func(0) != 5)
            failed = true;
        if (func(1) != 1)
            failed = true;
        if (func(2) != 5)
            failed = true;
        if (failed)
        {
            Console.WriteLine("Test Failed");
            return 101;
        }
        else
        {
            Console.WriteLine("Passed");
            return 100;
        }
    }
}
