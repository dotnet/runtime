// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPError
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPError(float x, float y) { 
         return x - (x/y)*y;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        float y = FPError(81f, 16f);
        Console.WriteLine(y);
        if (System.Math.Abs(y) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
