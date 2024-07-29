// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_DblAvg2
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblAvg2(double x, double y) 
    { 
       double z = (x+y)/2.0f;
       return z; 
    }

    [Fact]
    public static int TestEntryPoint()
    {
        double y = DblAvg2(5f, 7f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-6f) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
