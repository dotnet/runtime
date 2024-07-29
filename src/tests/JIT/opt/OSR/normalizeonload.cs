// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Ensure small OSR locals are marked as normalize on load

public class Runtime_83959
{
    static bool B(out byte b)
    {
        b = 1;
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void F() {}

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    static void WithOSR(int n, out char c)
    {
        c = (char) 0;
        B(out byte b);
        for (int i = 0; i < n; i++)
        {
            F();
        }
        // This load of `b` must be a single byte
        c = (char) b;
        c += (char) 99;
    }

    // Ensure stack is filled with nonzero data
    static void FillStack(int n)
    {
        Span<int> s = stackalloc int[n];
        for (int i = 0; i < n; i++)
        {
            s[i] = -1;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        char c = (char) 0;
        FillStack(100);
        WithOSR(50000, out c);
        return (int) c;
    }
}
