// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

// fgOptimizeHWIntrinsic transforms "(-v1) + v2" to "v2 - v1;"
// This triggered a use before def assert during Rationalization because v1
// stores to the local read by v2

public class Runtime_127747
{
    public static uint s_3;
    public static uint[] s_9 = new uint[] { 0 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void M0()
    {
        var vr2 = Vector128.Create((sbyte)1);
        var vr6 = Vector128.Create((sbyte)1);
        var vr8 = (sbyte)0;
        var vr7 = Vector128.CreateScalar(vr8);
        var vr5 = AdvSimd.ShiftLogicalSaturate(vr6, vr7);
        var vr0 = AdvSimd.Subtract(vr2, vr5);
        var vr9 = s_9[0];
        var vr10 = 0 % Crc32.ComputeCrc32(vr9, s_3);
        M1(vr0, vr10);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void M1(Vector128<sbyte> arg2, ulong arg4)
    {
    }

    [Fact]
    public static void TestEntryPoint()
    {
        if (!AdvSimd.IsSupported || !Crc32.IsSupported)
        {
            return;
        }

        try
        {
            M0();
        }
        catch (DivideByZeroException)
        {
            // Expected from "0 % Crc32.ComputeCrc32(0, 0)".
        }
    }
}
