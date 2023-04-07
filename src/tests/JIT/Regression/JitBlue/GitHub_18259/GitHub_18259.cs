// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// Test case for https://github.com/dotnet/coreclr/issues/18259
//
// We were a missing check for ZeroOffsetFldSeq values on LclVar reads
// 
// Debug: Outputs 0
// Release: Outputs 1234

struct S1
{
    public uint F0;
    public S1(uint f0): this() { F0 = f0; }
}

struct S2
{
    public S1  F1;
    public int F2;
    public S2(S1 f1): this() { F1 = f1; F2 = 1; }
}

public class Program
{
    static S2[] s_11 = new S2[]{new S2(new S1(1234u))};   // Assigns 1234 to F1.F0
    [Fact]
    public static int TestEntryPoint()
    {
        ref S1 vr7 = ref s_11[0].F1;
        vr7.F0 = vr7.F0;

        vr7.F0 = 0;                // Bug: We fail to update the Map with the proper ValueNum here.

	if (vr7.F0 != 0)           // Bug: We continue to return the old value for vr7.F0
        {
            System.Console.WriteLine(vr7.F0);
            System.Console.WriteLine("Failed");
            return 101;
        }
        else
        {
            System.Console.WriteLine("Passed");
            return 100;
        }
    }
}
