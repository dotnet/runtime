// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
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

    public static int Main()
    {
        double y = DblDist(5f, 7f, 2f, 3f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-5d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
