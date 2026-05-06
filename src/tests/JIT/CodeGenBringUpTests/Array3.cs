// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class BringUpTest_Array3
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int Array3()
    {
        int[] a = {1, 2, 3, 4};
        a[1] = 5;
        return a[1];
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (Array3() != 5) return Fail;
        return Pass;
    }
}
