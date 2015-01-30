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
    public static Double DblSub(Double x, Double y) { return x-y; }

    public static int Main()
    {
        Double y = DblSub(17d, 9d);
        Console.WriteLine(y);
        if (System.Math.Abs(y-8f) <= Double.Epsilon) return Pass;
        else return Fail;
    }
}
