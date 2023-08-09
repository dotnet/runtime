// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_Or1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Or1(int x) { return x | 0xa; }

    [Fact]
    public static int TestEntryPoint()
    {
        int y = Or1(4);
        if (y == 14) return Pass;
        else return Fail;
    }
}
