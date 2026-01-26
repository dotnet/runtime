// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;

public class RangeSectionMapTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TestLookupFail(MockTarget.Architecture arch)
    {
        var builder = MockDescriptors.ExecutionManager.CreateRangeSection(arch);
        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget);

        var rsla = RangeSectionMap.Create(target);

        var inputPC = new TargetCodePointer(0x007f_0000);
        var result = rsla.FindFragmentInternal(target, builder.TopLevel, inputPC);
        Assert.False(result.HasValue);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TestLookupOne(MockTarget.Architecture arch)
    {
        var builder = MockDescriptors.ExecutionManager.CreateRangeSection(arch);
        var inputPC = new TargetCodePointer(0x007f_0000);
        var length = 0x1000u;
        var value = 0x0a0a_0a0au;
        builder.InsertAddressRange(inputPC, length, value);
        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget);

        var rsla = RangeSectionMap.Create(target);

        var cursor = rsla.FindFragmentInternal(target, builder.TopLevel, inputPC);
        Assert.True(cursor.HasValue);
        var result = cursor.Value.LoadValue(target);
        Assert.Equal(value, result.Address.Value);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TestGetIndexForLevel(MockTarget.Architecture arch)
    {
        // Exhaustively test GetIndexForLevel for all possible values of the byte for each level
        var target = new TestPlaceholderTarget(arch, new MockMemorySpace.MemoryContext().ReadFromTarget);
        var rsla = RangeSectionMap.Create(target);
        int numLevels = arch.Is64Bit ? 5 : 2;
        // the bits 0..effectiveRange - 1 are not handled the map and are irrelevant
        // the bits from effectiveRange..maxSetBit are used by the map, the bits above maxSetBit are irrelevant
        int maxSetBit = arch.Is64Bit ? 56 : 31;
        int effectiveRange = maxSetBit + 1 - (numLevels * 8);
        ulong irrelevantLowBits = 0xcccccccc_ccccccccul & ((1ul << (effectiveRange - 1)) - 1);
        for (int i = 0; i < 256; i++) {
            for (int level = 1; level <= numLevels; level++) {
                ulong address;
                // Set address to 0xfffff_{i}_0000_cccc
                // where all the unused bits above the current level (including bits above maxSetBit) are
                // set to 1 and all the lower bits are set to 0
                // and the current level bits are set to i
                // and the bits that will be handled by the fragment linked list are set to 0xccc
                ulong upperBits = ~0ul << (effectiveRange + 8 * level);
                ulong payload = (ulong)i << (8 * (level - 1));
                ulong workingBits = payload << effectiveRange;
                address = upperBits | workingBits | irrelevantLowBits;
                int expected = i;
                int actual = rsla.GetIndexForLevel(new TargetCodePointer(address), level);
                Assert.Equal(expected, actual);
            }
        }
    }

}
