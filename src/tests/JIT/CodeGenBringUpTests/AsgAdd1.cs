// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_AsgAdd1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int AsgAdd1(int x) { x += 1; return x; }

    [Fact]
    public static int TestEntryPoint()
    {
        if (AsgAdd1(0) == 1) return Pass;
        else return Fail;
    }
}
