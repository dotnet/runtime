// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

#nullable disable

public class Runtime_105474_A
{
    private void Method0()
    {
        Vector128<ulong> vr0 = Vector128.CreateScalar(1698800584428641629UL);
        AdvSimd.ShiftLeftLogicalSaturate(vr0, 229);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        if (AdvSimd.IsSupported)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Runtime_105474_A().Method0());`
        }
    }
}
