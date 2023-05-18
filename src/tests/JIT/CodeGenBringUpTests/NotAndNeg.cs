// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_NotAndNeg
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int NotAndNeg(int x, int y) { return -x ^ ~y; }

    [Fact]
    public static int TestEntryPoint()
    {
        int y = NotAndNeg(1, 0);
        if (y == 0) return Pass;
        else return Fail;
    }
}
