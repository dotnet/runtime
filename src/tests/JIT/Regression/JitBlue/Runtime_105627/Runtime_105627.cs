// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

// Run on Arm64 Linux
// Seed: 10489942769529190437-vectort,vector64,vector128,armadvsimd,armadvsimdarm64,armaes,armarmbase,armarmbasearm64,armcrc32,armcrc32arm64,armdp,armrdm,armrdmarm64,armsha1,armsha256
// Reduced from 58.9 KiB to 0.4 KiB in 00:00:28
// Debug: Outputs [9223372036854775808, 9223372036854775808]
// Release: Outputs [0, 0]

public class Runtime_105627
{
    public static Vector128<double> s_9;

    [Fact]
    public static void TestEntyPoint()
    {
        var vr1 = Vector128.CreateScalar(0d);

        if (AdvSimd.IsSupported)
        {
            s_9 = AdvSimd.Arm64.Negate(vr1);
        }
        else
        {
            s_9 = -vr1;
        }
        Assert.Equal(Vector128.Create<ulong>(0x80000000_00000000UL), s_9.AsUInt64());
    }
}
