// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static uint PerformMod_1(uint i)
    {
        return i % 8;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int PerformMod_2(int i)
    {
        return i % 16;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int PerformMod_3(int i, int j)
    {
        return i % j;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int MSUB(int a, int b, int c)
    {
        return a - b * c;
    }

    static int Main(string[] args)
    {
        var result = 100;

        if (PerformMod_1(23) != 7)
        {
            result = -1;
            Console.WriteLine("Failed Mod1!");
        }

        if (PerformMod_2(-23) != -7)
        {
            result = -1;
            Console.WriteLine("Failed Mod2!");
        }

        if (PerformMod_3(23, 8) != 7)
        {
            result = -1;
            Console.WriteLine("Failed Mod3!");
        }

        if (MSUB(3, 7, 8) != -53)
        {
            result = -1;
            Console.WriteLine("Failed MSUB");
        }

        return result;
    }
}
