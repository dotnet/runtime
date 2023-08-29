// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPMath
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int FPMath() 
    {
        double r = Math.Cos(0);
        if (Math.Abs(r - 1d) <= Double.Epsilon) return Pass;      
        return Fail;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double FPMath(double x) 
    {
        return Math.Round(x);

    }

    [Fact]
    public static int TestEntryPoint()
    {
        int result = FPMath();

        if (result != Pass) return result;

        double r = FPMath(3.999d);
        Console.WriteLine(r);
        if (Math.Abs(r - 4d) <= Double.Epsilon) return Pass;      
        return Fail;
    }
}
