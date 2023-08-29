// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPConvI2F
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPConvI2F(int x) { return (float) x; }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double FPConvI2F(UInt32 x) { return (double) x; }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double FPConvI2F(long x) { return (double)x; }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double FPConvI2F(UInt64 x) { return (double)x; }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPConvI2F(byte x) { return (float)x; }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float FPConvI2F(Int16 x) { return (float)x; }

    [Fact]
    public static int TestEntryPoint()
    {
        int result = Fail;
        float x = FPConvI2F((int)3);
        Console.WriteLine(x);
        if (Math.Abs(x-3f) <= Single.Epsilon) result = Pass;
        
        int result2 = Fail;
        double y = FPConvI2F((UInt32)5);
        Console.WriteLine(y);
        if (Math.Abs(y-5d) <= Double.Epsilon) result2 = Pass;

        int result3 = Fail;
        y = FPConvI2F(12345L);
        Console.WriteLine(y);
        if (Math.Abs(y - 12345d) <= Double.Epsilon) result3 = Pass;

        int result4 = Fail;
        x = FPConvI2F((byte)3);
        Console.WriteLine(x);
        if (Math.Abs(x - 3f) <= Single.Epsilon) result4 = Pass;

        int result5 = Fail;
        x = FPConvI2F((Int16)3);
        Console.WriteLine(x);
        if (Math.Abs(x - 3f) <= Single.Epsilon) result5 = Pass;

        int result6 = Fail;
        y = FPConvI2F(12345UL);
        Console.WriteLine(y);
        if (Math.Abs(y - 12345d) <= Double.Epsilon) result6 = Pass;

        if (result == Pass && result2 == Pass && result3 == Pass && result4 == Pass && result5 == Pass && result6 == Pass) return Pass;
        return Fail;

    }
}
