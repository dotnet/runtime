// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_DblCall1
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblNeg(double x) { return -x; }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblCall1(double f) { 
        double x = DblNeg(f);
        double zero = x + f;
        return zero;
    }
                                       
    [Fact]
    public static int TestEntryPoint()
    {
        double y = DblCall1(-1d);
        Console.WriteLine(y);
        if (System.Math.Abs(y) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
