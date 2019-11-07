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
    public static double DblAvg6(double a, double b, double c, double d, double e, double f) 
    { 
       double z = (a+b+c+d+e+f)/6.0f;
       return z; 
    }

    public static int Main()
    {
        double y = DblAvg6(1d, 2d, 3d, 4d, 5d, 6d);
        Console.WriteLine(y);
        if (System.Math.Abs(y-3.5d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
