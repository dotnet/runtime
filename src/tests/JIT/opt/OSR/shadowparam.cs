// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// F is an OSR method with parameter shadowing

public unsafe struct ShadowParam
{
    public int a;
    public fixed int data[2];
    public int b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int F(int x, string sa, ShadowParam u, string sb, int a, int b, int y)
    {
        for (int i = 0; i < a + b; i++)
        {
            x += u.data[0];
        }

        for (int i = 0; i < a + b; i++)
        {
            y += u.data[1];
        }

        return x + y + u.a + u.b + sb.Length - sa.Length;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var u = new ShadowParam();
        u.data[0] = 1;
        u.data[1] = -1;
        u.a = 3;
        u.b = -3;

        int r = F(0, "a", u, "b", 100_001, -1, 100);
        Console.WriteLine($"Result is {r}");
        return r;
    }
}

