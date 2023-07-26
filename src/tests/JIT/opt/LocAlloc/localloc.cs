// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Conversion of small fixed localloc to locals
// and inlining of localloc callees

using System;
using Xunit;

public class L
{
    unsafe static int Use4()
    {
        byte* i = stackalloc byte[4];
        i[2] = 50;
        return i[2] * 2;
    }

    unsafe static int Use(int x)
    {
        byte* i = stackalloc byte[x];
        i[1] = 50;
        return i[1] * 2;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int v0 = Use4();
        int v1 = Use(10);
        int v2 = Use(100);
        int v3 = Use(v0);
        int v4 = 0;
        int v5 = 0;
        int v6 = 0;

        for (int i = 0; i < 7; i++)
        {
            v5 += Use4();
            v5 += Use(4);
            v6 += Use(v0);
        }

        return v0 + v1 + v2 + v3 + v4 + v5 + v6 - 2400;
    }
}
