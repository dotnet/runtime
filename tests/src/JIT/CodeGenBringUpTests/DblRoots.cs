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

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void DblRoots(double a, double b, double c, ref double r1, ref double r2) 
    { 
       r1 = (-b + Math.Sqrt(b*b - 4*a*c))/(2*a);
       r2 = (-b - Math.Sqrt(b*b - 4*a*c))/(2*a);
       return ; 
    }

    public static int Main()
    {
        double x1 = 0;
        double x2 = 0;
        DblRoots(1d, -5d, 6d, ref x1, ref x2);
        Console.WriteLine(x1 + "," + x2);
        if (System.Math.Abs(x1-3d) > Double.Epsilon) return Fail;
        if (System.Math.Abs(x2-2d) > Double.Epsilon) return Fail;
        return Pass;
    }
}
