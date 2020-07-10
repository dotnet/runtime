// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
       double z = (x+y)/2.0f;
       return z; 
    }

    public static int Main()
    {
        double y = DblAvg2(5f, 7f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-6f) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
