// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

// Original method has stackalloc, osr does not

class OriginalStackalloc
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static unsafe int F(int from, int to, int s)
    {
        int* result = stackalloc int[s];
        *result = 0;
        for (int i = from; i < to; i++)
        {
            *result += i;
        }
        return *result;
    }

    public static int Main()
    {
        Console.WriteLine($"starting sum");
        int result = F(0, 1_000_000, 1);
        Console.WriteLine($"done, sum is {result}");
        return (result == 1783293664) ? 100 : -1;
    }
}
