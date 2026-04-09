// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPCall2
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPAvg2(float x, float y) 
    { 
       float z = (x+y)/2.0f;
       return z; 
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPCall2(float a, float b, float c, float d)
    {
        //float e = FPAvg2(a, b);
        //float f = FPAvg2(c, d);
        //float g = FPAvg2(e, f);
        //return g;
        return FPAvg2(FPAvg2(a, b), FPAvg2(c, d));
    }

    [Fact]
    public static int TestEntryPoint()
    {
        float y = FPCall2(1f, 2f, 3f, 4f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-2.5f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
