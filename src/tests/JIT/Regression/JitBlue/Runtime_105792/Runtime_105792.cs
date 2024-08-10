// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_105792
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Problem(int x)
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
    static void Update(ref int x) 
    {
        x = x - 1;
    }

    [Fact]
    public static int Test() => Problem(10) + 100;
}
