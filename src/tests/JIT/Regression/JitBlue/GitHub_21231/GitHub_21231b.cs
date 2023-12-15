// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// Test case for https://github.com/dotnet/coreclr/issues/21231
//
// 
// Debug: Outputs 2
// Release: Outputs 1
struct S0
{
    public sbyte F0;
    public S0(sbyte p0): this()
    {
        F0 = p0;
    }
}

struct S1
{
    public S0 F1;
    public ushort F2;
    public S1(S0 p2): this()
    {
        F1 = p2;
    }
}

struct S2
{
    public S1 F3;
    public S2(S0 p3): this()
    {
        F3 = new S1(p3);
    }
}

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        var vr22 = new S2[]{new S2(new S0(1))};
        S2 vr26;
        vr26.F3.F1 = vr22[0].F3.F1;
        vr22[0].F3.F1.F0 += vr22[0].F3.F1.F0;
        vr26.F3.F1 = vr22[0].F3.F1;

        if (vr26.F3.F1.F0 != 2)
        {
            System.Console.WriteLine("Failed");
	    return -1;
        }
        else
        {
            System.Console.WriteLine("Passed");
	    return 100;
        }
    }
}
