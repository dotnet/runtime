// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_DblNeg
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblNeg(double x) { return -x; }

    [Fact]
    public static int TestEntryPoint()
    {
        double y = DblNeg(-1f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-1f) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
