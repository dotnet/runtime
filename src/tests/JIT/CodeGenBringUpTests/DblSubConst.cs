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
    public static double DblSubConst(double x) { return x-1d; }

    public static int Main()
    {
        double y = DblSubConst(1d);
        Console.WriteLine(y);
        if (System.Math.Abs(y) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
