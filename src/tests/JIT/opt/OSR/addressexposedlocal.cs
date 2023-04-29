// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// OSR method has address exposed local

public class AddressExposedLocal
{
    // [MethodImpl(MethodImplOptions.NoInlining)]
    public static unsafe int I(ref int p) => p;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static unsafe void J(ref int p)  {}

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static unsafe int F(int from, int to)
    {
        int result = 0;
        J(ref result);
        for (int i = from; i < to; i++)
        {
            result = I(ref result) + i;
        }
        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine($"starting sum");
        int result = F(0, 1_000_000);
        Console.WriteLine($"done, sum is {result}");
        return (result == 1783293664) ? 100 : -1;
    }  
}
