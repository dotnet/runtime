// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Unit test for copy propagation assertion.

using System;
using Xunit;

public class Sample2
{
    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static sbyte func(int a, int b)
    {
        int res = 1000;
        if (a < b)
            res = -1;
        else if (a > b)
            res = +1;
        else
            res = (sbyte)((a != b) ? 1 : 0);

        return (sbyte)res;
    }

    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int func(int y)
    {
        int sum = 0;
        int x = y;
        y = 4;
        for (int i = 0; i < x; i++)
            sum++;
        use(x);
        return sum;
    }

    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void use(int x) { }

    [Fact]
    public static int TestEntryPoint()
    {
        bool failed = false;
        if (func(1, 2) != -1)
            failed = true;
        if (func(3, 2) != +1)
            failed = true;
        if (func(2, 2) != 0)
            failed = true;
        if (func(2) != 2)
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
