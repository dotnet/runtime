// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_DblDist
{
    const int Pass = 100;
    const int Fail = -1;

    //JBTodo - remove this to use Math.Sqrt() instead once MathFN is implemented.
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double sqrt(double f)
    {
       return Math.Sqrt(f);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblDist(double x1, double y1, double x2, double y2) 
    { 
       double z = sqrt((x2-x1)*(x2-x1) + (y2-y1)*(y2-y1));
       return z; 
    }

    [Fact]
    public static int TestEntryPoint()
    {
        double y = DblDist(5f, 7f, 2f, 3f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-5d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
