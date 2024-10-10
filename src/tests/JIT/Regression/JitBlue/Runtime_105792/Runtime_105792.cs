// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_105792
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Problem1(int x)
    {
        int y = 0;
        while (x != 0)
        {
            if (y == x) return -1;
            y = x;
            Update(ref x);
        }
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Problem2(int x)
    {
        int y = 0;
        while (x != 0)
        {
            if (y == x + 1) return -1;
            y = x + 1;
            Update(ref x);
        }
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Update(ref int x) 
    {
        x = x - 1;
    }

    [Fact]
    public static int Test1() => Problem1(10) + 100;

    [Fact]
    public static int Test2() => Problem2(10) + 100;
}
