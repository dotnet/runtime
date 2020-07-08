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
    public static double DblMul(double x, double y) { return x*y; }

    public static int Main()
    {
        double y = DblMul(17d, 9d);
        Console.WriteLine(y);
        if (System.Math.Abs(y-153f) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
