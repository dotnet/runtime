// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_DblDivConst
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblDivConst(double x) { return x/2; }

    [Fact]
    public static int TestEntryPoint()
    {
        double y = DblDivConst(5d);
        Console.WriteLine(y);
        if (System.Math.Abs(y-2.5d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
