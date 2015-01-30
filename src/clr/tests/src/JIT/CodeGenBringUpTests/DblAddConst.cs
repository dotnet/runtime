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
    public static double DblAddConst(double x) { return x+1; }

    public static int Main()
    {
        double y = DblAddConst(13d);
        Console.WriteLine(y);
        if (System.Math.Abs(y-14d) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
