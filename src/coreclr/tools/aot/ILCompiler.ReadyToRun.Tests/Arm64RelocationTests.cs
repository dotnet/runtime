// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;

using crossgen2::ILCompiler.DependencyAnalysis;

using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class Arm64RelocationTests
{
    private const uint BranchWithLink = 0x94000000;
    private const uint OpcodeMask = 0xFC000000;

    [Theory]
    [InlineData(0L)]
    [InlineData(4L)]
    [InlineData(-4L)]
    [InlineData(0x07FFFFFCL)]
    [InlineData(-0x08000000L)]
    public unsafe void Branch26RoundTripsDisplacementInRange(long displacement)
    {
        uint instruction = BranchWithLink;

        Relocation.WriteValue(RelocType.IMAGE_REL_BASED_ARM64_BRANCH26, &instruction, displacement);

        Assert.Equal(displacement, Relocation.ReadValue(RelocType.IMAGE_REL_BASED_ARM64_BRANCH26, &instruction));
        Assert.Equal(BranchWithLink, instruction & OpcodeMask);
    }

    [Theory]
    [InlineData(0x08000000L)]
    [InlineData(-0x08000004L)]
    [InlineData(0x10000000L)]
    public void Branch26RejectsDisplacementOutOfRange(long displacement)
    {
        uint[] instruction = [BranchWithLink];

        Assert.Throws<ArgumentOutOfRangeException>(() => WriteBranch26(instruction, displacement));
        Assert.Equal(BranchWithLink, instruction[0]);
    }

    private static unsafe void WriteBranch26(uint[] instruction, long displacement)
    {
        fixed (uint* location = instruction)
        {
            Relocation.WriteValue(RelocType.IMAGE_REL_BASED_ARM64_BRANCH26, location, displacement);
        }
    }
}
