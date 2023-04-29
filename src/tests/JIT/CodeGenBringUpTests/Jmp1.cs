// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_Jmp1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Jmp1(int x)
    {
        goto L1;
L2:
        x = x+1;
        goto L3;
L1:
        x = x+1;
        goto L2;
L3:
        return x+1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int y = Jmp1(1);
        if (y == 4) return Pass;
        else return Fail;
    }
}
