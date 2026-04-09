// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_Ind1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    internal static void Ind1(ref int x) { x = 1; return; }

    [Fact]
    public static int TestEntryPoint()
    {
        int y = 0;
        Ind1(ref y);
        if (y == 1) return Pass;
        else return Fail;
    }
}
