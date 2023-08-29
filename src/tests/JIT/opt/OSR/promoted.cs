// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

// OSR complications with promoted structs

struct Y
{
    public Y(int _a, int _b)
    {
        a = _a;
        b = _b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Init(int _a, int _b)
    {
        s_y = new Y(_a, _b);
    }

    public static Y s_y;
    public int a;
    public int b;
}

public class OSRMethodStructPromotion
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int F(int from, int to)
    {
        Y.Init(from, to);
        Y y = Y.s_y;
        int result = 0;
        for (int i = y.a; i < y.b; i++)
        {
            result += i;
        }
        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int final = 1_000_000;
        F(0, 10);
        int result = F(0, final);
        int expected = 1783293664;
        return result == expected ? 100 : -1;
    }  
}
