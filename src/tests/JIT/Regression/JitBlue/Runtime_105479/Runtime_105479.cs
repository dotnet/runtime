// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Runtime_105479;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Xunit;

#nullable disable

public class Runtime_105479_A
{
    private void Method0()
    {
        Vector128<ulong> vr0 = Vector128.CreateScalar(1698800584428641629UL);
        AdvSimd.ShiftLeftLogicalSaturate(vr0, 229);
    }

    private void Method1()
    {
        Vector128<float> vr1 = default;
        Avx.Compare(vr1, vr1, (FloatComparisonMode)255);
    }

    [ConditionalFact(typeof(AdvSimd), nameof(AdvSimd.IsSupported))]
    public static void TestEntryPointArm()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Runtime_105479_A().Method0());
    }

    [ConditionalFact(typeof(Avx), nameof(Avx.IsSupported))]
    public static void TestEntryPoint()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Runtime_105479_A().Method1());
    }
}
