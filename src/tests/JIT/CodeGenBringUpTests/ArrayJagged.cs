// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class BringUpTest_ArrayJagged
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int ArrayJagged(int i)
    {
        int[][] a = new int[2][];
        a[0] = new int[2] {0, 1};
        a[1] = new int[2] {2, 3};
        return a[1][i];
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (ArrayJagged(1) != 3) return Fail;
        return Pass;
    }
}
