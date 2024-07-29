// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPConvF2F
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double FPConvF2F(float x) { return (double) x; }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPConvF2F(double x) { return (float) x; }

    [Fact]
    public static int TestEntryPoint()
    {
        int result = Fail;
        double x = FPConvF2F(3f);
        Console.WriteLine(x);

        if (Math.Abs(x-3d) <= Double.Epsilon)
            result = Pass;
        
        int result2 = Fail;
        float z = FPConvF2F(3.2d);
        Console.WriteLine(z);
        if (Math.Abs(z-3.2f) <= Single.Epsilon) result2 = Pass;

        if(result==Pass && result2==Pass) return Pass;
        return Fail;

    }
}
