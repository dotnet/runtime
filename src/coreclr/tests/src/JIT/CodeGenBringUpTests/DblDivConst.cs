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
    public static double DblDivConst(double x) { return x/2; }

    public static int Main()
    {
        double y = DblDivConst(5d);
        Console.WriteLine(y);
        if (System.Math.Abs(y-2.5d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
