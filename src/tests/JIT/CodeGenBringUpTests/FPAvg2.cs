// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPAvg2
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPAvg2(float x, float y) 
    { 
       float z = (x+y)/2.0f;
       return z; 
    }

    [Fact]
    public static int TestEntryPoint()
    {
        float y = FPAvg2(5f, 7f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-6f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
