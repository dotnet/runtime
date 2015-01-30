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
    public static double DblNeg(double x) { return -x; }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double DblCall1(double f) { 
        double x = DblNeg(f);
        double zero = x + f;
        return zero;
    }
                                       
    public static int Main()
    {
        double y = DblCall1(-1d);
        Console.WriteLine(y);
        if (System.Math.Abs(y) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
