// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System;

namespace PimpMyApp;

public class Program
{
    public static int Main()
    {
        Console.WriteLine("Hi");
        int result = Bambala1(6, 23);
        result += Bambala2(6, 23, 45, 66, 2);
        long result2 = Bambala3(6, 23, 45, 66, 2, 2);

        if (Or10Or5(14, 23) != Or15(14, 23) || Or10Or5(78, 11) != Or15(78, 11))
        {
            Console.WriteLine("Oups");
        }

        return result + (int)result2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Bambala1(int x, int y) => (x | 3) | (y | 5);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Bambala2(int p, int u, int x, int y, int z) => ((u | 2) | (x | 5) | (y | 3) | (z | 6)) * p + ((x | 6) |  (u | 7));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Bambala3(long m, int p, int u, int x, int y, int z) => m * System.Math.BigMul(((u | 2) | (x | 5) | (y | 3) | (z | 6)), p);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Or10Or5(int x, int y)
    {
        return (x | 10) | (y | 5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Or15(int x, int y)
    {
        return (x | y) | 15;
    }
}
