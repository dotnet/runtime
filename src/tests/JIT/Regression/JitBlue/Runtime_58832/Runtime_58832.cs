// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_58832
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Test(double.MaxValue);
        }
        catch (OverflowException)
        {
            return 100;
        }
        return 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test(double x)
    {
        try { Console.WriteLine(checked((ulong)x)); } catch { }

        if ((ulong)x == checked((ulong)x))
            Console.WriteLine("Should not be invoked");
    }
}
