// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPSmall
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPSmall(float x, float y) 
    { 
       float result;
       if (y < x)
           result = y;
       else
           result = x;
       return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        float y = FPSmall(3f, 2f);
        Console.WriteLine(y);
        if (System.Math.Abs(y-2f) <= Single.Epsilon) return Pass;
        else return Fail;
    }
}
