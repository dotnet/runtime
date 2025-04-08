// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPDivConst
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPDivConst(float x) { return 1/x; }

    [Fact]
    public static int TestEntryPoint()
    {
        float y = FPDivConst(5f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-0.2f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
