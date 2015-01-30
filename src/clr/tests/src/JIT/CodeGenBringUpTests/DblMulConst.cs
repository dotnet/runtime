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
    public static double DblMulConst(double r) { return 3.14d *r*r; }

    public static int Main()
    {
        double y = DblMulConst(10d);
        Console.WriteLine(y);
        if (System.Math.Abs(y-314d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
