// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Nested do lops

public class NestedDoLoops
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int F(int inner, int outer, int innerTo, int outerTo)
    {
        do {
            do {} while (inner++ < innerTo);
            inner = 0;
        } 
        while (outer++ < outerTo);

        return outer;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine($"starting sum");
        int result1 = F(0, 10, 0, 100_000);
        int result2 = F(0, 100_000, 0, 10);
        Console.WriteLine($"done, sum is {result1} and {result2}");
        return (result1 == result2) && (result1 == 100_001) ? 100 : -1;
    }  
}
