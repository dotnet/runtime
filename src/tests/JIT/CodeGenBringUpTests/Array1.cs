// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class BringUpTest_Array1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void Array1(int[] a)
    {
        a[1] = 5;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int[] a = {1, 2, 3, 4};
        Array1(a);

        if (a[1] != 5) return Fail;
        return Pass;
    }
}
