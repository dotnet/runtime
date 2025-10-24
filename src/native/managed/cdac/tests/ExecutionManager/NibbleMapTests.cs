// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;

public class NibbleMapTestsBase
{
    internal static Target CreateTarget(NibbleMapTestBuilderBase nibbleMapTestBuilder)
    {
        MockMemorySpace.MemoryContext memoryContext = new()
        {
            HeapFragments = new[] { nibbleMapTestBuilder.NibbleMapFragment }
        };
        return new TestPlaceholderTarget(nibbleMapTestBuilder.Arch, memoryContext.ReadFromTarget);
    }
}

public class NibbleMapLinearLookupTests : NibbleMapTestsBase
{
    [Fact]
    public void RoundTripAddressTest()
    {
        TargetPointer mapBase = 0;
        uint delta = 0x10u;
        for (TargetPointer p = mapBase; p < mapBase + 0x1000; p += delta)
        {
            TargetPointer actual = NibbleMapHelpers.RoundTripAddress(mapBase, p);
            Assert.Equal(p, actual);
        }
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(0x100u)]
    [InlineData(0xab00u)]
    // we don't really expct nibble maps to be this huge...
    [InlineData(0xabcd_abcd_7fff_ff00u)]
    public void ExhaustiveNibbbleShifts(ulong irrelevant)
    {
        // Try all possible inputs to ComputeNibbleShift.
        // Given the index of a nibble in the map, compute how much we have to shift a MapUnit to put that
        // nibble in the least significant position.
        // Actually we could just go up to 31 (since a map unit is 32 bits), but we'll go up to 255 for good measure
        int expectedShift = 28;
        for (int i = 0; i < 255; i++)
        {
            NibbleMapHelpers.MapKey input = new (irrelevant + (ulong)i);
            uint actualShift = NibbleMapHelpers.ComputeNibbleShift(input);
            Assert.True(expectedShift == actualShift, $"Expected {expectedShift}, got {actualShift} for input {input}");
            expectedShift -= 4;
            if (expectedShift == -4)
            {
                expectedShift = 28;
            }
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void NibbleMapOneItemLookupOk(MockTarget.Architecture arch)
    {
        // SETUP:

        // this is the beginning of the address range where code pointers might point
        TargetPointer mapBase = new(0x5f5f_0000u);
        // this is the beginning of the nibble map itself
        TargetPointer mapStart = new(0x0456_1000u);
        /// this is how big the address space is that the map covers
        const uint MapRangeSize = 0x1000;
        TargetPointer MapEnd = mapBase + MapRangeSize;
        var builder = new NibbleMapTestBuilder_1(mapBase, MapRangeSize, mapStart, arch);

        // don't put the code too close to the start - the NibbleMap bails if the code is too close to the start of the range
        TargetCodePointer inputPC = new(mapBase + 0x0200u);
        uint codeSize = 0x80; // doesn't matter
        builder.AllocateCodeChunk (inputPC, codeSize);
        Target target = CreateTarget(builder);

        // TESTCASE:

        NibbleMapLinearLookup map = (NibbleMapLinearLookup)NibbleMapLinearLookup.Create(target);
        Assert.NotNull(map);

        TargetPointer methodCode = map.FindMethodCode(mapBase, mapStart, inputPC);
        Assert.Equal(inputPC.Value, methodCode.Value);

        // All addresses in the code chunk should map to the same method
        for (int i = 0; i < codeSize; i++)
        {
            methodCode = map.FindMethodCode(mapBase, mapStart, inputPC.Value + (uint)i);
            // we should always find the beginning of the method
            Assert.Equal(inputPC.Value, methodCode.Value);
        }

        // All addresses before the code chunk should return null
        for (ulong i = mapBase; i < inputPC; i++)
        {
            methodCode = map.FindMethodCode(mapBase, mapStart, i);
            Assert.Equal(0u, methodCode.Value);
        }

        methodCode = map.FindMethodCode(mapBase, mapStart, inputPC.Value + 0x100u);
        Assert.Equal<TargetPointer>(inputPC.Value, methodCode.Value);

        // interestingly, all addresses after the code chunk should also return the beginning of the method
        // we don't track how long the method is, so we can't tell if we're past the end
        for (TargetCodePointer ptr = inputPC + (uint)codeSize; ptr < MapEnd; ptr++)
        {
            methodCode = map.FindMethodCode(mapBase, mapStart, ptr);
            Assert.Equal<TargetPointer>(inputPC.Value, methodCode);
        }

    }
}

public class NibbleMapConstantLookupTests : NibbleMapTestsBase
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void NibbleMapOneItemLookupOk(MockTarget.Architecture arch)
    {
        // SETUP:

        // this is the beginning of the address range where code pointers might point
        TargetPointer mapBase = new(0x5f5f_0000u);
        // this is the beginning of the nibble map itself
        TargetPointer mapStart = new(0x0456_1000u);
        /// this is how big the address space is that the map covers
        const uint MapRangeSize = 0x1000;
        TargetPointer MapEnd = mapBase + MapRangeSize;
        var builder = new NibbleMapTestBuilder_2(mapBase, MapRangeSize, mapStart, arch);

        // don't put the code too close to the start - the NibbleMap bails if the code is too close to the start of the range
        TargetCodePointer inputPC = new(mapBase + 0x0200u);
        uint codeSize = 0x400;
        builder.AllocateCodeChunk (inputPC, codeSize);
        Target target = CreateTarget(builder);

        // TESTCASE:

        NibbleMapConstantLookup map = (NibbleMapConstantLookup)NibbleMapConstantLookup.Create(target);
        Assert.NotNull(map);

        TargetPointer methodCode = map.FindMethodCode(mapBase, mapStart, inputPC);
        Assert.Equal(inputPC.Value, methodCode.Value);

        // All addresses in the code chunk should map to the same method
        for (int i = 0; i < codeSize; i++)
        {
            methodCode = map.FindMethodCode(mapBase, mapStart, inputPC.Value + (uint)i);
            // we should always find the beginning of the method
            Assert.Equal(inputPC.Value, methodCode.Value);
        }

        // All addresses before the code chunk should return null
        for (ulong i = mapBase; i < inputPC; i++)
        {
            methodCode = map.FindMethodCode(mapBase, mapStart, i);
            Assert.Equal(0u, methodCode.Value);
        }

        // All addresses more than 512 bytes after the code chunk should return null
        for (ulong i = inputPC.Value + (uint)codeSize + 512; i < MapEnd; i++)
        {
            methodCode = map.FindMethodCode(mapBase, mapStart, i);
            Assert.Equal(0u, methodCode.Value);
        }
    }
}
