// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class NibbleMapTests
{
    internal class NibbleMapTestTarget : TestPlaceholderTarget
    {
        private readonly MockMemorySpace.ReadContext _readContext;
        public NibbleMapTestTarget(MockTarget.Architecture arch, MockMemorySpace.ReadContext readContext) : base(arch)
        {
            _readContext = readContext;
            SetDataReader(_readContext.ReadFromTarget);
        }

    }

    internal class NibbleMapTestBuilder
    {
        // This is the base address of the memory range that the map covers.
        // The map works on code pointers as offsets from this address
        // For testing we don't actually place anything into this space
        private readonly TargetPointer MapBase;


        private readonly MockTarget.Architecture Arch;
        // this is the target memory representation of the nibble map itself
        public readonly MockMemorySpace.HeapFragment NibbleMapFragment;

        public NibbleMapTestBuilder(TargetPointer mapBase, ulong mapRangeSize, TargetPointer mapStart,MockTarget.Architecture arch)
        {
            MapBase = mapBase;
            Arch = arch;
            int nibbleMapSize = (int)Addr2Pos(mapRangeSize);
            NibbleMapFragment = new MockMemorySpace.HeapFragment {
                Address = mapStart,
                Data = new byte[nibbleMapSize],
                Name = "Nibble Map",
            };
        }

        const int Log2CodeAlign = 2; // N.B. this might be different on 64-bit in the future
        const int Log2NibblesPerDword = 3;
        const int Log2BytesPerBucket = Log2CodeAlign + Log2NibblesPerDword;
        const int Log2NibbleSize = 2;
        const int NibbleSize = 1 << Log2NibbleSize;
        const uint NibblesPerDword = (8 * sizeof(uint)) >> Log2NibbleSize;
        const uint NibblesPerDwordMask = NibblesPerDword - 1;
        const uint BytesPerBucket = NibblesPerDword * (1 << Log2CodeAlign);

        const uint MaskBytesPerBucket = BytesPerBucket - 1;

        const uint NibbleMask = 0xf;
        const int HighestNibbleBit = 32 - NibbleSize;

        const uint HighestNibbleMask = NibbleMask << HighestNibbleBit;

        private ulong Addr2Pos(ulong addr)
        {
            return addr >> Log2BytesPerBucket;
        }

        private uint Addr2Offs(ulong addr)
        {
            return (uint)  (((addr & MaskBytesPerBucket) >> Log2CodeAlign) + 1);
        }

        private int Pos2ShiftCount (ulong addr)
        {
            return HighestNibbleBit - (int)((addr & NibblesPerDwordMask) << Log2NibbleSize);
        }
        public void AllocateCodeChunk(TargetCodePointer codeStart, int codeSize)
        {
            // paraphrased from EEJitManager::NibbleMapSetUnlocked
            if (codeStart.Value < MapBase.Value)
            {
                throw new ArgumentException("Code start address is below the map base");
            }
            ulong delta = codeStart.Value - MapBase.Value;
            ulong pos = Addr2Pos(delta);
            bool bSet = true;
            uint value = bSet?Addr2Offs(delta):0;

            uint index = (uint) (pos >> Log2NibblesPerDword);
            uint mask = ~(HighestNibbleMask >> (int)((pos & NibblesPerDwordMask) << Log2NibbleSize));

            value = value << Pos2ShiftCount(pos);

            Span<byte> entry = NibbleMapFragment.Data.AsSpan((int)(index * sizeof(uint)), sizeof(uint));
            uint oldValue = TestPlaceholderTarget.ReadFromSpan<uint>(entry, Arch.IsLittleEndian);

            if (value != 0 && (oldValue & ~mask) != 0)
            {
                throw new InvalidOperationException("Overwriting existing offset");
            }

            uint newValue = (oldValue & mask) | value;
            TestPlaceholderTarget.WriteToSpan(newValue, Arch.IsLittleEndian, entry);
        }

        public NibbleMapTestTarget Create()
        {
            return new NibbleMapTestTarget(Arch, new MockMemorySpace.ReadContext() {
                HeapFragments = new[] { NibbleMapFragment }
            });
        }
    }

    [Fact]
    public void RoundTripAddressTest()
    {
        TargetPointer mapBase = 0;
        uint delta = 0x10u;
        for (TargetPointer p = mapBase; p < mapBase + 0x1000; p += delta)
        {
            TargetPointer actual = NibbleMap.RoundTripAddress(mapBase, p);
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
            NibbleMap.MapKey input = new (irrelevant + (ulong)i);
            int actualShift = NibbleMap.ComputeNibbleShift(input);
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
        NibbleMapTestBuilder builder = new(mapBase, MapRangeSize, mapStart, arch);

        // don't put the code too close to the start - the NibbleMap bails if the code is too close to the start of the range
        TargetCodePointer inputPC = new(mapBase + 0x0200u);
        int codeSize = 0x80; // doesn't matter
        builder.AllocateCodeChunk (inputPC, codeSize);
        NibbleMapTestTarget target = builder.Create();

        // TESTCASE:

        NibbleMap map = NibbleMap.Create(target);
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
