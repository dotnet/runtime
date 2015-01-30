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
    public static float FPAdd(float x, float y) { return x+y; }

    public static int Main()
    {
        float y = FPAdd(1f, 1f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-2f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
