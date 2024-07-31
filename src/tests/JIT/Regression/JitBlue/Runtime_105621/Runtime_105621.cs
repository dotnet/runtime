// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class Runtime_105621
{
    [Fact]
    public static void TestShiftByZero()
    {
        if (AdvSimd.IsSupported)
        {
            var vr3 = Vector64.Create<byte>(0);
            var vr4 = AdvSimd.ShiftRightLogical(vr3, 0);
            Assert.Equal(vr3, vr4);
        }
    }

    [Fact]
    public static void TestShiftToZero()
    {
        if (AdvSimd.IsSupported)
        {
            var vr3 = Vector64.Create<byte>(128);
            var vr4 = AdvSimd.ShiftRightLogical(vr3, 9);
            Assert.Equal(vr4, Vector64<byte>.Zero);
        }
    }
}
