// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPDiv
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPDiv(float x, float y) { return x/y; }

    [Fact]
    public static int TestEntryPoint()
    {
        float y = FPDiv(81f, 3f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-27f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
