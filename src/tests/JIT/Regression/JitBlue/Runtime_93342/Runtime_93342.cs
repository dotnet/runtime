// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_93342
{
    private int foo;
    private int bar;
    private int baz;
    
    [Fact]
    public static void TestEntryPoint()
    {
        new Runtime_93342().Run();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void Run()
    {
        if (foo == 1)
        {
            bar += 11;
            baz += 11;
        }
        if (foo == 2)
            bar += 12;
        if (foo == 3)
            bar += 13;
        if (foo == 4)
            bar += 14;
        if (foo == 5)
            bar += 15;
        if (foo == 6)
            bar += 16;
        if (foo == 7)
            bar += 17;
        if (foo == 8)
            bar += 18;
        if (foo == 9)
            bar += 19;
        if (foo == 10)
            bar += 20;
        if (foo == 11)
            bar += 21;
        if (foo == 12)
            bar += 22;
        if (foo == 13)
            bar += 23;
        if (foo == 14)
            bar += 24;
        if (foo == 15)
            bar += 25;
        if (foo == 16)
            bar += 26;
        if (foo == 17)
            bar += 27;
        if (foo == 18)
            bar += 28;
        if (foo == 19)
            bar += 29;
        if (foo == 20)
            bar += 30;
        if (foo == 21)
            bar += 31;
        if (foo == 22)
            bar += 32;
        if (foo == 23)
            bar += 33;
        if (foo == 24)
            bar += 34;
        if (foo == 25)
            bar += 35;
        if (foo == 26)
            bar += 36;
        if (foo == 27)
            bar += 37;
        if (foo == 28)
            bar += 38;
        if (foo == 29)
            bar += 39;
        if (foo == 30)
            bar += 40;
        if (foo == 31)
            bar += 41;
        if (foo == 32)
            bar += 42;
        if (foo == 33)
            bar += 43;
        if (foo == 34)
            bar += 44;
        if (foo == 35)
            bar += 45;
        if (foo == 36)
            bar += 46;
        if (foo == 37)
            bar += 47;

        bar = baz;
    }
}
