// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPAvg6
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPAvg6(float a, float b, float c, float d, float e, float f) 
    { 
       float z = (a+b+c+d+e+f)/6.0f;
       return z; 
    }

    [Fact]
    public static int TestEntryPoint()
    {
        float y = FPAvg6(1f, 2f, 3f, 4f, 5f, 6f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-3.5f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
