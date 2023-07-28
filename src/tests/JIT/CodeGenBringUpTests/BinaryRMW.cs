// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_BinaryRMW
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    internal static void BinaryRMW(ref int x, int y)
    {
        x += y;
        x |= 2;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int x = 12;
        BinaryRMW(ref x, 17);
        if (x == 31) return Pass;
        else return Fail;
    }
}
