// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
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


    public static int Main()
    {
        double y = DblArea(3d, 4d, 5d);
        Console.WriteLine(y);
        if (System.Math.Abs(y-6d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
