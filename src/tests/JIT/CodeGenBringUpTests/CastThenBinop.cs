// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;

// Test for https://github.com/dotnet/runtime/issues/13816
public class Test
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int DowncastOr(int a, int b)
    {
        return (byte)a | (byte)b;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long UpcastAnd(int a, int b)
    {
        return (long)a & (long)b;
    }

    public static int Main()
    {
        const int Pass = 100;
        const int Fail = -1;

        if (DowncastOr(0x0F, 0xF0) != 0xFF)
        {
            return Fail;
        }
        if (UpcastAnd(0x0FF, 0xFF0) != 0xF0)
        {
            return Fail;
        }

        return Pass;
    }
}
