// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace PimpMyApp;

public class Program
{
    public static int Main()
    {
        // return Bambala(6, 23);
        return (int)Bambala(2, 6, 23, 45, 66, 2);
    }

    // [MethodImpl(MethodImplOptions.NoInlining)]
    // private static int Bambala(int x, int y) => x | y | 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Bambala(int m, int p, int u, int x, int y, int z) => m * System.Math.BigMul(((u | 2) | (x | 5) | (y | 3) | (z | 6)), p);
}
