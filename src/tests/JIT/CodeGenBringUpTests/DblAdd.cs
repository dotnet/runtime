// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_DblAdd
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblAdd(double x, double y) { return x+y; }

    [Fact]
    public static int TestEntryPoint()
    {
        double y = DblAdd(1d, 1d);
        Console.WriteLine(y);
        if (System.Math.Abs(y-2f) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
