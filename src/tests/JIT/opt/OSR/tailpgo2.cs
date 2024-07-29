// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class X
{
    static int s;
    static int N;

    // OSR method that makes a tail call.
    //
    // If we're also adding PGO probes,
    // we need to relocate the ones for
    // the return to happen before the
    // tail calls.
    //
    internal static void T(int x, int[] a)
    {
        for (int j = 0; j < N; j++)
        {
            for (int i = 0; i < a.Length; i++)
            {
                s += a[i];
            }
        }
        
        if (x >= 3)
        {
            T(x-3, a);
        }
        else if (x > 0)
        {
            T(x-1, a);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Fact]
    public static int TestEntryPoint()
    {
        int[] a = new int[1000];
        N = 100;
        s = -349900;
        a[3] = 33;
        a[997] = 67;
        T(100, a);
        return s;
    }
}
