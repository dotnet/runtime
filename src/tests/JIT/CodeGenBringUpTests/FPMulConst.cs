// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPMulConst
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPMulConst(float r) { return 3.14f *r*r; }

    [Fact]
    public static int TestEntryPoint()
    {
        float y = FPMulConst(10f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-314f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
