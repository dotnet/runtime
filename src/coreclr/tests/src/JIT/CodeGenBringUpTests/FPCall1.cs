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
    public static float FPNeg(float x) { return -x; }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPCall1(float f) { 
        float x = FPNeg(f);
        float zero = x + f;
        return zero;
    }
                                       
    public static int Main()
    {
        float y = FPCall1(-1f);
        Console.WriteLine(y);
        if (System.Math.Abs(y) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
