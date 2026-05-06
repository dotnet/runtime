// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Method creates has two OSR methods

public class TwoOSRMethods
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void I(ref int p, int i) => p = p + i;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int F(int from, int to, bool b)
    {
        int result = 0;

        if (b)
        {
            for (int i = from; i < to; i++)
            {
                I(ref result, i);
            }
        }
        else
        {
            for (int i = from; i < to; i++)
            {
                result += i;
            }

        }
        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int final = 1_000_000;
        int result1 = F(0, final, true);
        int result2 = F(0, final, false);
        return (result1 == result2) && (result1 == 1783293664) ? 100 : -1;
    }  
}
