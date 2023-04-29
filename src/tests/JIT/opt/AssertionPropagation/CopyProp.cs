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
