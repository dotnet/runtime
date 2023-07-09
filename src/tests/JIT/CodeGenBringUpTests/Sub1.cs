// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_Sub1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Sub1(int x) { return x - 1; }

    [Fact]
    public static int TestEntryPoint()
    {
        int y = Sub1(1);
        if (y == 0) return Pass;
        else return Fail;
    }
}
