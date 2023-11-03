// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_CnsLng1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static long CnsLng1() { return 1; }

    [Fact]
    public static int TestEntryPoint()
    {
        long y = CnsLng1();
        if (y == 1) return Pass;
        else return Fail;
    }
}
