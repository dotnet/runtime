// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;

internal abstract class NibbleMapTestBuilderBase
{
    // This is the base address of the memory range that the map covers.
    // The map works on code pointers as offsets from this address
    // For testing we don't actually place anything into this space
    protected TargetPointer MapBase { get; init; }

    public MockTarget.Architecture Arch { get; init; }

    // this is the target memory representation of the nibble map itself
    public MockMemorySpace.HeapFragment NibbleMapFragment { get; init; }

    protected const int Log2CodeAlign = 2; // This might be different on 64-bit in the future
    protected const int Log2NibblesPerDword = 3;
    protected const int Log2BytesPerBucket = Log2CodeAlign + Log2NibblesPerDword;
    protected const int Log2NibbleSize = 2;
    protected const int NibbleSize = 1 << Log2NibbleSize;
    protected const uint NibblesPerDword = (8 * sizeof(uint)) >>> Log2NibbleSize;
    protected const uint NibblesPerDwordMask = NibblesPerDword - 1;
    protected const uint BytesPerBucket = NibblesPerDword * (1 << Log2CodeAlign);

    protected const uint MaskBytesPerBucket = BytesPerBucket - 1;

    protected const uint NibbleMask = 0xf;
    protected const int HighestNibbleBit = 32 - NibbleSize;

    protected const uint HighestNibbleMask = NibbleMask << HighestNibbleBit;

    protected ulong Addr2Pos(ulong addr)
    {
        return addr >>> Log2BytesPerBucket;
    }

    protected uint Addr2Offs(ulong addr)
    {
        return (uint)  (((addr & MaskBytesPerBucket) >>> Log2CodeAlign) + 1);
    }

    protected int Pos2ShiftCount (ulong addr)
    {
        return HighestNibbleBit - (int)((addr & NibblesPerDwordMask) << Log2NibbleSize);
    }

    public NibbleMapTestBuilderBase(TargetPointer mapBase, ulong mapRangeSize, TargetPointer mapStart, MockTarget.Architecture arch)
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

    public NibbleMapTestBuilderBase(TargetPointer mapBase, ulong mapRangeSize, MockMemorySpace.BumpAllocator allocator, MockTarget.Architecture arch)
    {
        MapBase = mapBase;
        Arch = arch;
        int nibbleMapSize = (int)Addr2Pos(mapRangeSize);
        NibbleMapFragment = allocator.Allocate((ulong)nibbleMapSize, "Nibble Map");
    }

    public abstract void AllocateCodeChunk(TargetCodePointer codeStart, uint codeSize);
}

internal class NibbleMapTestBuilder_1 : NibbleMapTestBuilderBase
{
    public NibbleMapTestBuilder_1(TargetPointer mapBase, ulong mapRangeSize, TargetPointer mapStart, MockTarget.Architecture arch)
        : base(mapBase, mapRangeSize, mapStart, arch)
    {
    }

    public NibbleMapTestBuilder_1(TargetPointer mapBase, ulong mapRangeSize, MockMemorySpace.BumpAllocator allocator, MockTarget.Architecture arch)
        : base(mapBase, mapRangeSize, allocator, arch)
    {
    }

    public override void AllocateCodeChunk(TargetCodePointer codeStart, uint codeSize)
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

        uint index = (uint) (pos >>> Log2NibblesPerDword);
        uint mask = ~(HighestNibbleMask >>> (int)((pos & NibblesPerDwordMask) << Log2NibbleSize));

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
}

internal class NibbleMapTestBuilder_2 : NibbleMapTestBuilderBase
{
    public NibbleMapTestBuilder_2(TargetPointer mapBase, ulong mapRangeSize, TargetPointer mapStart, MockTarget.Architecture arch)
        : base(mapBase, mapRangeSize, mapStart, arch)
    {
    }

    public NibbleMapTestBuilder_2(TargetPointer mapBase, ulong mapRangeSize, MockMemorySpace.BumpAllocator allocator, MockTarget.Architecture arch)
        : base(mapBase, mapRangeSize, allocator, arch)
    {
    }

    public override void AllocateCodeChunk(TargetCodePointer codeStart, uint codeSize)
    {
        // paraphrased from EEJitManager::NibbleMapSetUnlocked
        if (codeStart.Value < MapBase.Value)
        {
            throw new ArgumentException("Code start address is below the map base");
        }
        ulong delta = codeStart.Value - MapBase.Value;

        ulong pos = Addr2Pos(delta);
        uint value = Addr2Offs(delta);

        uint index = (uint) (pos >>> Log2NibblesPerDword);
        uint mask = ~(HighestNibbleMask >>> (int)((pos & NibblesPerDwordMask) << Log2NibbleSize));

        value = value << Pos2ShiftCount(pos);

        Span<byte> entry = NibbleMapFragment.Data.AsSpan((int)(index * sizeof(uint)), sizeof(uint));
        uint oldValue = TestPlaceholderTarget.ReadFromSpan<uint>(entry, Arch.IsLittleEndian);

        if (value != 0 && (oldValue & ~mask) != 0)
        {
            throw new InvalidOperationException("Overwriting existing offset");
        }

        uint newValue = (oldValue & mask) | value;
        TestPlaceholderTarget.WriteToSpan(newValue, Arch.IsLittleEndian, entry);

        ulong firstByteAfterMethod = delta + (uint)codeSize;
        uint encodedPointer = NibbleMapConstantLookup.EncodePointer((uint)delta);
        index++;
        while((index + 1) * 256 <= firstByteAfterMethod)
        {
            entry = NibbleMapFragment.Data.AsSpan((int)(index * sizeof(uint)), sizeof(uint));
            oldValue = TestPlaceholderTarget.ReadFromSpan<uint>(entry, Arch.IsLittleEndian);
            if(oldValue != 0)
            {
                throw new InvalidOperationException("Overwriting existing offset");
            }
            TestPlaceholderTarget.WriteToSpan(encodedPointer, Arch.IsLittleEndian, entry);
            index++;
        }
    }
}
