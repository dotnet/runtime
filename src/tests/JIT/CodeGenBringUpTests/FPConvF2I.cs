// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_FPConvF2I
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int FPConvF2I(float x) { return (int) x; }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static byte FPConvF2I(double x) { return (byte) x; }


    [Fact]
    public static int TestEntryPoint()
    {
        int result = Fail;
        int x = FPConvF2I(3.14f);
        Console.WriteLine(x);
        if (x == 3) result = Pass;
        
        int result2 = Fail;
        byte y = FPConvF2I(3.14d);
        Console.WriteLine(y);
        if (y == 3) result2 = Pass;

        if (result == Pass && result2 == Pass) return Pass;
        return Fail;

    }
}
