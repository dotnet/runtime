// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_AsgSub1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int AsgSub1(int x) { x -= 1; return x; }

    [Fact]
    public static int TestEntryPoint()
    {
        if (AsgSub1(1) == 0) return Pass;
        else return Fail;
    }
}
