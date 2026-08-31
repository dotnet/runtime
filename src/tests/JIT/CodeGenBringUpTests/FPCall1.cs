// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPCall1
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
                                       
    [Fact]
    public static int TestEntryPoint()
    {
        float y = FPCall1(-1f);
        Console.WriteLine(y);
        if (System.Math.Abs(y) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
