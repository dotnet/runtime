// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Run on Arm64 Linux
// Seed: 10489942769529190437-vectort,vector64,vector128,armadvsimd,armadvsimdarm64,armaes,armarmbase,armarmbasearm64,armcrc32,armcrc32arm64,armdp,armrdm,armrdmarm64,armsha1,armsha256
// Reduced from 58.9 KiB to 0.4 KiB in 00:00:28
// Debug: Outputs [9223372036854775808, 9223372036854775808]
// Release: Outputs [0, 0]

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_105742
{
    [Fact]
    public static void TestEntyPoint()
    {
        if (!Avx512BW.VL.IsSupported)
        {
            return;
        }

        if (ShiftLeft().ToString() != "<2, 1, 1, 1, 1, 1, 1, 1>")
        {
            throw new Exception("ShiftLeft");
        }
        if (ShiftRight().ToString() != "<0, 32767, -1, -1, -1, -1, -1, -1>")
        {
            throw new Exception("ShiftRight");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<short> ShiftLeft()
    {
        var vr4 = Vector128.Create<short>(1);
        var vr5 = (ushort)1;
        var vr6 = Vector128.CreateScalar(vr5);
        var vr7 = Avx512BW.VL.ShiftLeftLogicalVariable(vr4, vr6);
        return vr7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<short> ShiftRight()
    {
        var vr2 = Vector128.Create<short>(-1);
        var vr3 = Vector128.Create(65534, 1, 0, 0, 0, 0, 0, 0);
        return Avx512BW.VL.ShiftRightLogicalVariable(vr2, vr3);
    }
}
