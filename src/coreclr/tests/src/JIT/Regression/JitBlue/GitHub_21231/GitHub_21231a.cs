// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

// Test case for issue 21231
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
    public S0 F2;
    public ushort F1;
    public S1(S0 p2): this()
    {
        F2 = p2;
    }
}

public class Program
{
    public static int Main()
    {
        var vr22 = new S1[]{new S1(new S0(1))};
        S1 vr26;
        vr26.F2 = vr22[0].F2;
        vr22[0].F2.F0 += vr22[0].F2.F0;
        vr26.F2 = vr22[0].F2;

        if (vr26.F2.F0 != 2)
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
