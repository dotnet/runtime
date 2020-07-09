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
    public static double DblRem(double x, double y) { return x%y; }

    public static int Main()
    {
        double y = DblRem(81f, 45f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-36f) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
