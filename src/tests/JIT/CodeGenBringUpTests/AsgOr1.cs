// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_AsgOr1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int AsgOr1(int x) { x |= 0xa; return x; }

    [Fact]
    public static int TestEntryPoint()
    {
        if (AsgOr1(4) == 0xe) return Pass;
        else return Fail;
    }
}
