// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class BringUpTest_ArrayMD1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int ArrayMD1()
    {
        int[,] a = {{1, 2}, {3, 4}};
        return a[0, 1];
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (ArrayMD1() != 2) return Fail;
        return Pass;
    }
}
