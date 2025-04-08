// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_NestedCall
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int NestedCall(int x)
    {
        return x * x;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int NestedCall(int a, int b)
    {
        int c = NestedCall(NestedCall(a)) + NestedCall(NestedCall(b));
        return c;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int y = NestedCall(2, 3);
        if (y == 97) return Pass;
        else return Fail;
    }
}
