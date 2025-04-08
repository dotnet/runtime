// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_NegRMW
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    internal static void NegRMW(ref int x) { x = -x; }

    [Fact]
    public static int TestEntryPoint()
    {
        int x = 12;
        NegRMW(ref x);
        if (x == -12) return Pass;
        else return Fail;
    }
}
