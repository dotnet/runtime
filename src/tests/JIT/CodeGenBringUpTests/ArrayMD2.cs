// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class BringUpTest_ArrayMD2
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int ArrayMD2(int x, int y)
    {
        int[,] a = new int[2, 3];
        a[x, y] = 42;
        return a[x, y];
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (ArrayMD2(1, 1) != 42) return Fail;
        return Pass;
    }
}
