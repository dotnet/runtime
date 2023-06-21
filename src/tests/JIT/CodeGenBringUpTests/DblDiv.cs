// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_DblDiv
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblDiv(double x, double y) { return x/y; }

    [Fact]
    public static int TestEntryPoint()
    {
        double y = DblDiv(81d, 3d);
        Console.WriteLine(y);
        if (System.Math.Abs(y-27d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
