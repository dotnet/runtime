// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_DblArea
{
    const int Pass = 100;
    const int Fail = -1;

    //JBTODO - remove this when adding support for calling MathFns
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblSqrt(double x)
    {
       return System.Math.Sqrt(x);
    }

    // Computes area of a triangle given its three sides
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblArea(double a, double b, double c) 
    {
        double s = (a+b+c)/2f;
        return DblSqrt(s*(s-a)*(s-b)*(s-c));
    }


    [Fact]
    public static int TestEntryPoint()
    {
        double y = DblArea(3d, 4d, 5d);
        Console.WriteLine(y);
        if (System.Math.Abs(y-6d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
