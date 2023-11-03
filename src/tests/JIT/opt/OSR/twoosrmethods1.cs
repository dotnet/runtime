// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Two OSR methods from one original method

public class X
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int F(int from, int to, bool sumUp)
    {
        int result = 0;

        if (sumUp)
        {
            for (int i = from; i < to; i++)
            {
                result += i;
            }
        }
        else
        {
            for (int i = to; i > from; i--)
            {
                result += (i-1);
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
