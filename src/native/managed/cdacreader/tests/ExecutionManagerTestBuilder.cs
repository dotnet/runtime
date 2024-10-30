// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

using InteriorMapValue = Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers.RangeSectionMap.InteriorMapValue;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

internal class ExecutionManagerTestBuilder
{
    public const ulong ExecutionManagerCodeRangeMapAddress = 0x000a_fff0;

    const int RealCodeHeaderSize = 0x08; // must be big enough for the offsets of RealCodeHeader size in ExecutionManagerTestTarget, below

    public struct AllocationRange
    {
        // elements of the range section map are allocated in this range
        public ulong RangeSectionMapStart;
        public ulong RangeSectionMapEnd;
        // nibble maps for various range section fragments are allocated in this range
        public ulong NibbleMapStart;
        public ulong NibbleMapEnd;
        // "RealCodeHeader" objects for jitted methods are allocated in this range
        public ulong CodeHeaderStart;
        public ulong CodeHeaderEnd;
    }

    public static readonly AllocationRange DefaultAllocationRange = new AllocationRange {
        RangeSectionMapStart = 0x00dd_0000,
        RangeSectionMapEnd = 0x00de_0000,
        NibbleMapStart = 0x00ee_0000,
        NibbleMapEnd = 0x00ef_0000,
        CodeHeaderStart = 0x0033_4000,
        CodeHeaderEnd = 0x0033_5000,
    };
    internal class NibbleMapTestBuilder
    {
        // This is the base address of the memory range that the map covers.
        // The map works on code pointers as offsets from this address
        // For testing we don't actually place anything into this space
        private readonly TargetPointer MapBase;

        internal readonly MockTarget.Architecture Arch;
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

        public NibbleMapTestBuilder(TargetPointer mapBase, ulong mapRangeSize, MockMemorySpace.BumpAllocator allocator, MockTarget.Architecture arch)
        {
            MapBase = mapBase;
            Arch = arch;
            int nibbleMapSize = (int)Addr2Pos(mapRangeSize);
            NibbleMapFragment = allocator.Allocate((ulong)nibbleMapSize, "Nibble Map");
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
    }


    internal static NibbleMapTestBuilder CreateNibbleMap(TargetPointer mapBase, ulong mapRangeSize, TargetPointer mapStart, MockTarget.Architecture arch)
    {
        return new NibbleMapTestBuilder(mapBase, mapRangeSize, mapStart, arch);
    }

   internal class RangeSectionMapTestBuilder
    {
        const ulong DefaultTopLevelAddress = 0x0000_1000u; // arbitrary
        const int EntriesPerMapLevel = 256; // for now its fixed at 256, see codeman.h RangeSectionMap::entriesPerMapLevel
        const int BitsPerLevel = 8;

        private readonly TargetPointer _topLevelAddress;
        private readonly MockMemorySpace.Builder _builder;
        private readonly TargetTestHelpers _targetTestHelpers;
        private readonly int _levels;
        private readonly int _maxSetBit;
        private ulong _nextMapAddress;
        public RangeSectionMapTestBuilder(MockTarget.Architecture arch) : this (DefaultTopLevelAddress, new MockMemorySpace.Builder (new TargetTestHelpers(arch)))
        {
        }

        public RangeSectionMapTestBuilder (TargetPointer topLevelAddress, MockMemorySpace.Builder builder)
        {
            _topLevelAddress = topLevelAddress;
            _builder = builder;
            _targetTestHelpers = builder.TargetTestHelpers;
            var arch = _targetTestHelpers.Arch;
            _levels = arch.Is64Bit ? 5 : 2;
            _maxSetBit = arch.Is64Bit ? 56 : 31; // 0 indexed
            MockMemorySpace.HeapFragment top = new MockMemorySpace.HeapFragment
            {
                Address = topLevelAddress,
                Data = new byte[EntriesPerMapLevel * _targetTestHelpers.PointerSize],
                Name = $"Map Level {_levels}"
            };
            _nextMapAddress = topLevelAddress + (ulong)top.Data.Length;
            _builder.AddHeapFragment(top);
        }

        public TargetPointer TopLevel => _topLevelAddress;

        private int EffectiveBitsForLevel(ulong address, int level)
        {
            ulong addressBitsUsedInMap = address >> (_maxSetBit + 1 - (_levels * BitsPerLevel));
            ulong addressBitsShifted = addressBitsUsedInMap >> ((level - 1) * BitsPerLevel);
            int addressBitsUsedInLevel = checked((int)((EntriesPerMapLevel - 1) & addressBitsShifted));
            return addressBitsUsedInLevel;
        }

        // This is how much of the address space is covered by each entry in the last level of the map
        private int BytesAtLastLevel => checked (1 << BitsAtLastLevel);
        private int BitsAtLastLevel => _maxSetBit - (BitsPerLevel * _levels) + 1;

        private TargetPointer CursorAddress(RangeSectionMap.Cursor cursor)
        {
            return cursor.LevelMap + (ulong)(cursor.Index * _targetTestHelpers.PointerSize);
        }

        private void WritePointer(RangeSectionMap.Cursor cursor, InteriorMapValue value)
        {
            TargetPointer address = CursorAddress(cursor);
            Span<byte> dest = _builder.BorrowAddressRange(address, _targetTestHelpers.PointerSize);
            _targetTestHelpers.WritePointer(dest, value.RawValue);
        }

        private InteriorMapValue LoadCursorValue (RangeSectionMap.Cursor cursor)
        {
            TargetPointer address = CursorAddress(cursor);
            ReadOnlySpan<byte> src = _builder.BorrowAddressRange(address, _targetTestHelpers.PointerSize);
            return new InteriorMapValue(_targetTestHelpers.ReadPointer(src));
        }

        private MockMemorySpace.HeapFragment AllocateMapLevel(int level)
        {
            MockMemorySpace.HeapFragment mapLevel = new MockMemorySpace.HeapFragment
            {
                Address = new TargetPointer(_nextMapAddress),
                Data = new byte[EntriesPerMapLevel * _targetTestHelpers.PointerSize],
                Name = $"Map Level {level}"
            };
            _nextMapAddress += (ulong)mapLevel.Data.Length;
            _builder.AddHeapFragment(mapLevel);
            return mapLevel;
        }


        // computes the cursor for the next level down from the given cursor
        // if the slot for the next level does not exist, it is created
        private RangeSectionMap.Cursor GetOrAddLevelSlot(TargetCodePointer address, RangeSectionMap.Cursor cursor, bool collectible = false)
        {
            int nextLevel = cursor.Level - 1;
            int nextIndex = EffectiveBitsForLevel(address, nextLevel);
            InteriorMapValue nextLevelMap = LoadCursorValue(cursor);
            if (nextLevelMap.IsNull)
            {
                nextLevelMap = new (AllocateMapLevel(nextLevel).Address);
                if (collectible)
                {
                    nextLevelMap = new (nextLevelMap.RawValue | 1);
                }
                WritePointer(cursor, nextLevelMap);
            }
            return new RangeSectionMap.Cursor(nextLevelMap.Address, nextLevel, nextIndex);
        }

        // ensures that the maps for all the levels for the given address are allocated.
        // returns the address of the slot in the last level that corresponds to the given address
        RangeSectionMap.Cursor EnsureLevelsForAddress(TargetCodePointer address, bool collectible = false)
        {
            int topIndex = EffectiveBitsForLevel(address, _levels);
            RangeSectionMap.Cursor cursor = new RangeSectionMap.Cursor(TopLevel, _levels, topIndex);
            while (!cursor.IsLeaf)
            {
                cursor = GetOrAddLevelSlot(address, cursor, collectible);
            }
            return cursor;
        }
        public void InsertAddressRange(TargetCodePointer start, uint length, ulong value, bool collectible = false)
        {
            TargetCodePointer cur = start;
            ulong end = start.Value + length;
            do {
                RangeSectionMap.Cursor lastCursor = EnsureLevelsForAddress(cur, collectible);
                WritePointer(lastCursor, new InteriorMapValue(value));
                cur = new TargetCodePointer(cur.Value + (ulong)BytesAtLastLevel); // FIXME: round ?
            } while (cur.Value < end);
        }
        public void MarkCreated()
        {
            _builder.MarkCreated();
        }

        public MockMemorySpace.ReadContext GetReadContext()
        {
            return _builder.GetReadContext();
        }
    }

    public static RangeSectionMapTestBuilder CreateRangeSection(MockTarget.Architecture arch)
    {
        return new RangeSectionMapTestBuilder(arch);
    }

    internal MockMemorySpace.Builder Builder { get; }
    private readonly RangeSectionMapTestBuilder _rsmBuilder;

    private readonly MockMemorySpace.BumpAllocator _rangeSectionMapAllocator;
    private readonly MockMemorySpace.BumpAllocator _nibbleMapAllocator;
    private readonly MockMemorySpace.BumpAllocator _codeHeaderAllocator;

    internal readonly Dictionary<DataType, Target.TypeInfo> TypeInfoCache = new();

    internal ExecutionManagerTestBuilder(MockTarget.Architecture arch,  AllocationRange allocationRange) : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)), allocationRange)
    {}


    internal ExecutionManagerTestBuilder(MockMemorySpace.Builder builder, AllocationRange allocationRange, Dictionary<DataType, Target.TypeInfo>? typeInfoCache = null)
    {
        Builder = builder;
        _rsmBuilder = new RangeSectionMapTestBuilder(ExecutionManagerCodeRangeMapAddress, builder);
        _rangeSectionMapAllocator = Builder.CreateAllocator(allocationRange.RangeSectionMapStart, allocationRange.RangeSectionMapEnd);
        _nibbleMapAllocator = Builder.CreateAllocator(allocationRange.NibbleMapStart, allocationRange.NibbleMapEnd);
        _codeHeaderAllocator = Builder.CreateAllocator(allocationRange.CodeHeaderStart, allocationRange.CodeHeaderEnd);
        TypeInfoCache = typeInfoCache ?? CreateTypeInfoCache(Builder.TargetTestHelpers);
    }

    internal static Dictionary<DataType, Target.TypeInfo> CreateTypeInfoCache(TargetTestHelpers targetTestHelpers)
    {
        Dictionary<DataType, Target.TypeInfo> typeInfoCache = new();
        AddToTypeInfoCache(targetTestHelpers, typeInfoCache);
        return typeInfoCache;
    }

    internal static void AddToTypeInfoCache(TargetTestHelpers targetTestHelpers, Dictionary<DataType, Target.TypeInfo> typeInfoCache)
    {
        var layout = targetTestHelpers.LayoutFields([
            (nameof(Data.RangeSectionMap.TopLevelData), DataType.pointer),
        ]);
        typeInfoCache[DataType.RangeSectionMap] = new Target.TypeInfo() {
                Fields = layout.Fields,
                Size = layout.Stride,
        };
        layout = targetTestHelpers.LayoutFields([
            (nameof(Data.RangeSectionFragment.RangeBegin), DataType.pointer),
            (nameof(Data.RangeSectionFragment.RangeEndOpen), DataType.pointer),
            (nameof(Data.RangeSectionFragment.RangeSection), DataType.pointer),
            (nameof(Data.RangeSectionFragment.Next), DataType.pointer)
        ]);
        typeInfoCache[DataType.RangeSectionFragment] = new Target.TypeInfo() {
                Fields = layout.Fields,
                Size = layout.Stride,
        };
        layout = targetTestHelpers.LayoutFields([
            (nameof(Data.RangeSection.RangeBegin), DataType.pointer),
            (nameof(Data.RangeSection.RangeEndOpen), DataType.pointer),
            (nameof(Data.RangeSection.NextForDelete), DataType.pointer),
            (nameof(Data.RangeSection.JitManager), DataType.pointer),
            (nameof(Data.RangeSection.Flags), DataType.int32),
            (nameof(Data.RangeSection.HeapList), DataType.pointer),
            (nameof(Data.RangeSection.R2RModule), DataType.pointer),
        ]);
        typeInfoCache[DataType.RangeSection] = new Target.TypeInfo() {
                Fields = layout.Fields,
                Size = layout.Stride,
        };
        layout = targetTestHelpers.LayoutFields([
            (nameof(Data.CodeHeapListNode.Next), DataType.pointer),
            (nameof(Data.CodeHeapListNode.StartAddress), DataType.pointer),
            (nameof(Data.CodeHeapListNode.EndAddress), DataType.pointer),
            (nameof(Data.CodeHeapListNode.MapBase), DataType.pointer),
            (nameof(Data.CodeHeapListNode.HeaderMap), DataType.pointer),
        ]);
        typeInfoCache[DataType.CodeHeapListNode] = new Target.TypeInfo() {
                Fields = layout.Fields,
                Size = layout.Stride,
        };
        layout = targetTestHelpers.LayoutFields([
            (nameof(Data.RealCodeHeader.MethodDesc), DataType.pointer),
        ]);
        typeInfoCache[DataType.RealCodeHeader] = new Target.TypeInfo() {
                Fields = layout.Fields,
                Size = layout.Stride,
        };
    }

    internal NibbleMapTestBuilder CreateNibbleMap(ulong codeRangeStart, uint codeRangeSize)
    {

        NibbleMapTestBuilder nibBuilder = new NibbleMapTestBuilder(codeRangeStart, codeRangeSize, _nibbleMapAllocator, Builder.TargetTestHelpers.Arch);
        Builder.AddHeapFragment(nibBuilder.NibbleMapFragment);
        return nibBuilder;
    }

    internal readonly struct JittedCodeRange
    {
        public  MockMemorySpace.BumpAllocator Allocator {get ; init;}
        public ulong RangeStart => Allocator.RangeStart;
        public ulong RangeEnd => Allocator.RangeEnd;
        public ulong RangeSize => RangeEnd - RangeStart;
    }

    public JittedCodeRange AllocateJittedCodeRange(ulong codeRangeStart, uint codeRangeSize)
    {
        MockMemorySpace.BumpAllocator allocator = Builder.CreateAllocator(codeRangeStart, codeRangeStart + codeRangeSize);
        return new JittedCodeRange { Allocator = allocator };
    }

    public TargetPointer AddRangeSection(JittedCodeRange jittedCodeRange, TargetPointer jitManagerAddress, TargetPointer codeHeapListNodeAddress)
    {
        var tyInfo = TypeInfoCache[DataType.RangeSection];
        uint rangeSectionSize = tyInfo.Size.Value;
        MockMemorySpace.HeapFragment rangeSection = _rangeSectionMapAllocator.Allocate(rangeSectionSize, "RangeSection");
        Builder.AddHeapFragment(rangeSection);
        int pointerSize = Builder.TargetTestHelpers.PointerSize;
        Span<byte> rs = Builder.BorrowAddressRange(rangeSection.Address, (int)rangeSectionSize);
        Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.RangeBegin)].Offset, pointerSize), jittedCodeRange.RangeStart);
        Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.RangeEndOpen)].Offset, pointerSize), jittedCodeRange.RangeEnd);
        // 0x02 = RangeSectionFlags.CodeHeap
        Builder.TargetTestHelpers.Write(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.Flags)].Offset, sizeof(uint)), (uint)0x02);
        Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.HeapList)].Offset, pointerSize), codeHeapListNodeAddress);
        Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.JitManager)].Offset, pointerSize), jitManagerAddress);
        // FIXME: other fields

        return rangeSection.Address;
    }

    public TargetPointer AddRangeSectionFragment(JittedCodeRange jittedCodeRange, TargetPointer rangeSectionAddress)
    {
        var tyInfo = TypeInfoCache[DataType.RangeSectionFragment];
        uint rangeSectionFragmentSize = tyInfo.Size.Value;
        MockMemorySpace.HeapFragment rangeSectionFragment = _rangeSectionMapAllocator.Allocate(rangeSectionFragmentSize, "RangeSectionFragment");
        // FIXME: this shouldn't really be called InsertAddressRange, but maybe InsertRangeSectionFragment?
        _rsmBuilder.InsertAddressRange(jittedCodeRange.RangeStart, (uint)jittedCodeRange.RangeSize, rangeSectionFragment.Address);
        Builder.AddHeapFragment(rangeSectionFragment);
        int pointerSize = Builder.TargetTestHelpers.PointerSize;
        Span<byte> rsf = Builder.BorrowAddressRange(rangeSectionFragment.Address, (int)rangeSectionFragmentSize);
        Builder.TargetTestHelpers.WritePointer(rsf.Slice(tyInfo.Fields[nameof(Data.RangeSectionFragment.RangeBegin)].Offset, pointerSize), jittedCodeRange.RangeStart);
        Builder.TargetTestHelpers.WritePointer(rsf.Slice(tyInfo.Fields[nameof(Data.RangeSectionFragment.RangeEndOpen)].Offset, pointerSize), jittedCodeRange.RangeEnd);
        Builder.TargetTestHelpers.WritePointer(rsf.Slice(tyInfo.Fields[nameof(Data.RangeSectionFragment.RangeSection)].Offset, pointerSize), rangeSectionAddress);
        /* Next = nullptr */
        // nothing
        return rangeSectionFragment.Address;
    }

    public TargetPointer AddCodeHeapListNode(TargetPointer next, TargetPointer startAddress, TargetPointer endAddress, TargetPointer mapBase, TargetPointer headerMap)
    {
        var tyInfo = TypeInfoCache[DataType.CodeHeapListNode];
        uint codeHeapListNodeSize = tyInfo.Size.Value;
        MockMemorySpace.HeapFragment codeHeapListNode = _rangeSectionMapAllocator.Allocate (codeHeapListNodeSize, "CodeHeapListNode");
        Builder.AddHeapFragment(codeHeapListNode);
        int pointerSize = Builder.TargetTestHelpers.PointerSize;
        Span<byte> chln = Builder.BorrowAddressRange(codeHeapListNode.Address, (int)codeHeapListNodeSize);
        Builder.TargetTestHelpers.WritePointer(chln.Slice(tyInfo.Fields[nameof(Data.CodeHeapListNode.Next)].Offset, pointerSize), next);
        Builder.TargetTestHelpers.WritePointer(chln.Slice(tyInfo.Fields[nameof(Data.CodeHeapListNode.StartAddress)].Offset, pointerSize), startAddress);
        Builder.TargetTestHelpers.WritePointer(chln.Slice(tyInfo.Fields[nameof(Data.CodeHeapListNode.EndAddress)].Offset, pointerSize), endAddress);
        Builder.TargetTestHelpers.WritePointer(chln.Slice(tyInfo.Fields[nameof(Data.CodeHeapListNode.MapBase)].Offset, pointerSize), mapBase);
        Builder.TargetTestHelpers.WritePointer(chln.Slice(tyInfo.Fields[nameof(Data.CodeHeapListNode.HeaderMap)].Offset, pointerSize), headerMap);
        return codeHeapListNode.Address;
    }

    private uint CodeHeaderSize => (uint)Builder.TargetTestHelpers.PointerSize;

    // offset from the start of the code
    private uint CodeHeaderOffset => CodeHeaderSize;

    private (MockMemorySpace.HeapFragment fragment, TargetCodePointer codeStart) AllocateJittedMethod(JittedCodeRange jittedCodeRange, int codeSize, string name = "Method Header & Code")
    {
        ulong size = (ulong)(codeSize + CodeHeaderOffset);
        MockMemorySpace.HeapFragment methodFragment = jittedCodeRange.Allocator.Allocate(size, name);
        Builder.AddHeapFragment(methodFragment);
        TargetCodePointer codeStart = methodFragment.Address + CodeHeaderOffset;
        return (methodFragment, codeStart);
    }

    public TargetCodePointer AddJittedMethod(JittedCodeRange jittedCodeRange, int codeSize, TargetPointer methodDescAddress)
    {
        (MockMemorySpace.HeapFragment methodFragment, TargetCodePointer codeStart) = AllocateJittedMethod(jittedCodeRange, codeSize);

        MockMemorySpace.HeapFragment codeHeaderFragment = _codeHeaderAllocator.Allocate(RealCodeHeaderSize, "RealCodeHeader");
        Builder.AddHeapFragment(codeHeaderFragment);

        Span<byte> mfPtr = Builder.BorrowAddressRange(methodFragment.Address, (int)CodeHeaderSize);
        Builder.TargetTestHelpers.WritePointer(mfPtr.Slice(0, Builder.TargetTestHelpers.PointerSize), codeHeaderFragment.Address);

        Span<byte> chf = Builder.BorrowAddressRange(codeHeaderFragment.Address, RealCodeHeaderSize);
        var tyInfo = TypeInfoCache[DataType.RealCodeHeader];
        Builder.TargetTestHelpers.WritePointer(chf.Slice(tyInfo.Fields[nameof(Data.RealCodeHeader.MethodDesc)].Offset, Builder.TargetTestHelpers.PointerSize), methodDescAddress);

        return codeStart;
    }

    public void MarkCreated() => Builder.MarkCreated();
}
