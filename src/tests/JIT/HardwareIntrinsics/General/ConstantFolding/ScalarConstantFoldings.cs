// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class ScalarConstantFoldings
{
    [Fact]
    public static void LeadingZeroCountTests()
    {
        if (Lzcnt.IsSupported)
        {
            Assert.Equal(32U, Lzcnt.LeadingZeroCount(0));
            Assert.Equal(31U, Lzcnt.LeadingZeroCount(1));
            Assert.Equal(17U, Lzcnt.LeadingZeroCount(31400));
            Assert.Equal(0U, Lzcnt.LeadingZeroCount(1U << 31));
            Assert.Equal(0U, Lzcnt.LeadingZeroCount(uint.MaxValue));
            Assert.Equal(0U, Lzcnt.LeadingZeroCount(uint.MaxValue - 1));
        }
        if (Lzcnt.X64.IsSupported)
        {
            Assert.Equal(64UL, Lzcnt.X64.LeadingZeroCount(0));
            Assert.Equal(63UL, Lzcnt.X64.LeadingZeroCount(1));
            Assert.Equal(49UL, Lzcnt.X64.LeadingZeroCount(31400));
            Assert.Equal(32UL, Lzcnt.X64.LeadingZeroCount(1UL << 31));
            Assert.Equal(0UL, Lzcnt.X64.LeadingZeroCount(1UL << 63));
            Assert.Equal(0UL, Lzcnt.X64.LeadingZeroCount(ulong.MaxValue));
            Assert.Equal(0UL, Lzcnt.X64.LeadingZeroCount(ulong.MaxValue - 1));
        }
        if (ArmBase.IsSupported)
        {
            Assert.Equal(32, ArmBase.LeadingZeroCount(0));
            Assert.Equal(31, ArmBase.LeadingZeroCount(1));
            Assert.Equal(17, ArmBase.LeadingZeroCount(31400));
            Assert.Equal(0, ArmBase.LeadingZeroCount(1U << 31));
            Assert.Equal(0, ArmBase.LeadingZeroCount(uint.MaxValue));
            Assert.Equal(0, ArmBase.LeadingZeroCount(uint.MaxValue - 1));
        }
        if (ArmBase.Arm64.IsSupported)
        {
            Assert.Equal(64, ArmBase.Arm64.LeadingZeroCount(0));
            Assert.Equal(63, ArmBase.Arm64.LeadingZeroCount(1));
            Assert.Equal(49, ArmBase.Arm64.LeadingZeroCount(31400));
            Assert.Equal(32, ArmBase.Arm64.LeadingZeroCount(1UL << 31));
            Assert.Equal(0, ArmBase.Arm64.LeadingZeroCount(1UL << 63));
            Assert.Equal(0, ArmBase.Arm64.LeadingZeroCount(ulong.MaxValue));
            Assert.Equal(0, ArmBase.Arm64.LeadingZeroCount(ulong.MaxValue - 1));
        }
    }
}
