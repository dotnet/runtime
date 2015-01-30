// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblAvg2(double x, double y) 
    { 
       double z = (x+y)/2.0d;
       return z; 
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblCall2(double a, double b, double c, double d)
    {
        return DblAvg2(DblAvg2(a, b), DblAvg2(c, d));
    }

    public static int Main()
    {
        double y = DblCall2(1d, 2d, 3d, 4d);
        Console.WriteLine(y);
        if (System.Math.Abs(y-2.5d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
