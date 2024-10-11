// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using Microsoft.Diagnostics.DataContractReader.Contracts;
using System.Collections.Generic;
using System;
namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class ExecutionManagerTests
{
    private const ulong ExecutionManagerCodeRangeMapAddress = 0x000a_fff0;

    const int RealCodeHeaderSize = 0x08; // must be big enough for the offsets of RealCodeHeader size in ExecutionManagerTestTarget, below


    internal struct AllocationRange
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

    private static AllocationRange s_DefaultAllocationRange = new AllocationRange {
        RangeSectionMapStart = 0x00dd_0000,
        RangeSectionMapEnd = 0x00de_0000,
        NibbleMapStart = 0x00ee_0000,
        NibbleMapEnd = 0x00ef_0000,
        CodeHeaderStart = 0x0033_4000,
        CodeHeaderEnd = 0x0033_5000,
    };

    internal class ExecutionManagerTestBuilder
    {
        public MockMemorySpace.Builder Builder { get; }
        private readonly RangeSectionMapTests.Builder _rsmBuilder;

        private readonly MockMemorySpace.BumpAllocator _rangeSectionMapAllocator;
        private readonly MockMemorySpace.BumpAllocator _nibbleMapAllocator;
        private readonly MockMemorySpace.BumpAllocator _codeHeaderAllocator;

        public readonly Dictionary<DataType, Target.TypeInfo> TypeInfoCache = new();

        public ExecutionManagerTestBuilder(MockTarget.Architecture arch,  AllocationRange allocationRange) : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)), allocationRange)
        {}


        public ExecutionManagerTestBuilder(MockMemorySpace.Builder builder, AllocationRange allocationRange, Dictionary<DataType, Target.TypeInfo>? typeInfoCache = null)
        {
            Builder = builder;
            _rsmBuilder = new RangeSectionMapTests.Builder(ExecutionManagerCodeRangeMapAddress, builder);
            _rangeSectionMapAllocator = Builder.CreateAllocator(allocationRange.RangeSectionMapStart, allocationRange.RangeSectionMapEnd);
            _nibbleMapAllocator = Builder.CreateAllocator(allocationRange.NibbleMapStart, allocationRange.NibbleMapEnd);
            _codeHeaderAllocator = Builder.CreateAllocator(allocationRange.CodeHeaderStart, allocationRange.CodeHeaderEnd);
            TypeInfoCache = typeInfoCache ?? CreateTypeInfoCache(Builder.TargetTestHelpers);
        }

        public static Dictionary<DataType, Target.TypeInfo> CreateTypeInfoCache(TargetTestHelpers targetTestHelpers)
        {
            Dictionary<DataType, Target.TypeInfo> typeInfoCache = new();
            AddToTypeInfoCache(targetTestHelpers, typeInfoCache);
            return typeInfoCache;
        }

        public static void AddToTypeInfoCache(TargetTestHelpers targetTestHelpers, Dictionary<DataType, Target.TypeInfo> typeInfoCache)
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

        public NibbleMapTests.NibbleMapTestBuilder AddNibbleMap(ulong codeRangeStart, uint codeRangeSize)
        {

            NibbleMapTests.NibbleMapTestBuilder nibBuilder = new NibbleMapTests.NibbleMapTestBuilder(codeRangeStart, codeRangeSize, _nibbleMapAllocator, Builder.TargetTestHelpers.Arch);
            Builder.AddHeapFragment(nibBuilder.NibbleMapFragment);
            return nibBuilder;
        }

        public TargetPointer InsertRangeSection(ulong codeRangeStart, uint codeRangeSize, TargetPointer jitManagerAddress, TargetPointer codeHeapListNodeAddress)
        {
            var tyInfo = TypeInfoCache[DataType.RangeSection];
            uint rangeSectionSize = tyInfo.Size.Value;
            MockMemorySpace.HeapFragment rangeSection = _rangeSectionMapAllocator.Allocate(rangeSectionSize, "RangeSection");
            Builder.AddHeapFragment(rangeSection);
            int pointerSize = Builder.TargetTestHelpers.PointerSize;
            Span<byte> rs = Builder.BorrowAddressRange(rangeSection.Address, (int)rangeSectionSize);
            Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.RangeBegin)].Offset, pointerSize), codeRangeStart);
            Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.RangeEndOpen)].Offset, pointerSize), codeRangeStart + codeRangeSize);
            // 0x02 = RangeSectionFlags.CodeHeap
            Builder.TargetTestHelpers.Write(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.Flags)].Offset, sizeof(uint)), (uint)0x02);
            Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.HeapList)].Offset, pointerSize), codeHeapListNodeAddress);
            Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.JitManager)].Offset, pointerSize), jitManagerAddress);
            // FIXME: other fields

            return rangeSection.Address;
        }

        public TargetPointer InsertAddressRange(ulong codeRangeStart, uint codeRangeSize, TargetPointer rangeSectionAddress)
        {
            var tyInfo = TypeInfoCache[DataType.RangeSectionFragment];
            uint rangeSectionFragmentSize = tyInfo.Size.Value;
            MockMemorySpace.HeapFragment rangeSectionFragment = _rangeSectionMapAllocator.Allocate(rangeSectionFragmentSize, "RangeSectionFragment");
            // FIXME: this shouldn't really be called InsertAddressRange, but maybe InsertRangeSectionFragment?
            _rsmBuilder.InsertAddressRange(codeRangeStart, codeRangeSize, rangeSectionFragment.Address);
            Builder.AddHeapFragment(rangeSectionFragment);
            int pointerSize = Builder.TargetTestHelpers.PointerSize;
            Span<byte> rsf = Builder.BorrowAddressRange(rangeSectionFragment.Address, (int)rangeSectionFragmentSize);
            Builder.TargetTestHelpers.WritePointer(rsf.Slice(tyInfo.Fields[nameof(Data.RangeSectionFragment.RangeBegin)].Offset, pointerSize), codeRangeStart);
            Builder.TargetTestHelpers.WritePointer(rsf.Slice(tyInfo.Fields[nameof(Data.RangeSectionFragment.RangeEndOpen)].Offset, pointerSize), codeRangeStart + codeRangeSize);
            Builder.TargetTestHelpers.WritePointer(rsf.Slice(tyInfo.Fields[nameof(Data.RangeSectionFragment.RangeSection)].Offset, pointerSize), rangeSectionAddress);
            /* Next = nullptr */
            // nothing
            return rangeSectionFragment.Address;
        }

        public TargetPointer InsertCodeHeapListNode(TargetPointer next, TargetPointer startAddress, TargetPointer endAddress, TargetPointer mapBase, TargetPointer headerMap)
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

        public void AllocateMethod(TargetCodePointer codeStart, int codeSize, TargetPointer methodDescAddress)
        {
            int codeHeaderOffset = Builder.TargetTestHelpers.PointerSize;

            TargetPointer methodFragmentStart = codeStart.AsTargetPointer - (ulong)codeHeaderOffset;
            MockMemorySpace.HeapFragment methodFragment = new MockMemorySpace.HeapFragment
            {
                Address = methodFragmentStart,
                Data = new byte[codeSize + codeHeaderOffset],
                Name = "Method Header & Code"
            };
            Builder.AddHeapFragment(methodFragment);
            MockMemorySpace.HeapFragment codeHeaderFragment = _codeHeaderAllocator.Allocate(RealCodeHeaderSize, "RealCodeHeader");
            Builder.AddHeapFragment(codeHeaderFragment);
            Span<byte> mfPtr = Builder.BorrowAddressRange(methodFragmentStart, codeHeaderOffset);
            Builder.TargetTestHelpers.WritePointer(mfPtr.Slice(0, Builder.TargetTestHelpers.PointerSize), codeHeaderFragment.Address);
            Span<byte> chf = Builder.BorrowAddressRange(codeHeaderFragment.Address, RealCodeHeaderSize);
            var tyInfo = TypeInfoCache[DataType.RealCodeHeader];
            Builder.TargetTestHelpers.WritePointer(chf.Slice(tyInfo.Fields[nameof(Data.RealCodeHeader.MethodDesc)].Offset, Builder.TargetTestHelpers.PointerSize), methodDescAddress);
        }

        public void MarkCreated() => Builder.MarkCreated();

    }

    internal class ExecutionManagerTestTarget : TestPlaceholderTarget
    {
        private readonly ulong _executionManagerCodeRangeMapAddress;

        public ExecutionManagerTestTarget(MockTarget.Architecture arch, ReadFromTargetDelegate dataReader, Dictionary<DataType, TypeInfo> typeInfoCache) : base(arch)
        {
            _executionManagerCodeRangeMapAddress = ExecutionManagerCodeRangeMapAddress;
            SetDataReader(dataReader);
            SetTypeInfoCache(typeInfoCache);
            SetDataCache(new DefaultDataCache(this));
            IContractFactory<IExecutionManager> emfactory = new ExecutionManagerFactory();
            SetContracts(new TestRegistry() {
                ExecutionManagerContract = new (() => emfactory.CreateContract(this, 1)),
            });
        }

        public override TargetPointer ReadGlobalPointer(string global)
        {
            switch (global)
            {
            case Constants.Globals.ExecutionManagerCodeRangeMapAddress:
                return new TargetPointer(_executionManagerCodeRangeMapAddress);
            default:
                return base.ReadGlobalPointer(global);
            }
        }

        public override T ReadGlobal<T>(string name)
        {
            switch (name)
            {
            case Constants.Globals.StubCodeBlockLast:
                if (typeof(T) == typeof(byte))
                    return (T)(object)(byte)0x0Fu;
                break;
            default:
                break;
            }
            return base.ReadGlobal<T>(name);

        }

    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void LookupNull(MockTarget.Architecture arch)
    {
        ExecutionManagerTestBuilder emBuilder = new (arch, s_DefaultAllocationRange);
        emBuilder.MarkCreated();

        ExecutionManagerTestTarget target = new(arch, emBuilder.Builder.GetReadContext().ReadFromTarget, emBuilder.TypeInfoCache);
        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetEECodeInfoHandle(TargetCodePointer.Null);
        Assert.Null(eeInfo);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void LookupNonNullMissing(MockTarget.Architecture arch)
    {
        ExecutionManagerTestBuilder emBuilder = new (arch, s_DefaultAllocationRange);
        emBuilder.MarkCreated();
        ExecutionManagerTestTarget target = new(arch, emBuilder.Builder.GetReadContext().ReadFromTarget, emBuilder.TypeInfoCache);
        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetEECodeInfoHandle(new TargetCodePointer(0x0a0a_0000));
        Assert.Null(eeInfo);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void LookupNonNullOneRangeOneMethod(MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        TargetCodePointer methodStart = new (0x0a0a_0040); // arbitrary, inside [codeRangeStart,codeRangeStart+codeRangeSize]
        int methodSize = 0x100; // arbitrary

        TargetPointer jitManagerAddress = new (0x000b_ff00); // arbitrary

        TargetPointer expectedMethodDescAddress = new TargetPointer(0x0101_aaa0);

        ExecutionManagerTestBuilder emBuilder = new (arch, s_DefaultAllocationRange);

        NibbleMapTests.NibbleMapTestBuilder nibBuilder = emBuilder.AddNibbleMap(codeRangeStart, codeRangeSize);
        nibBuilder.AllocateCodeChunk(methodStart, methodSize);

        TargetPointer codeHeapListNodeAddress = emBuilder.InsertCodeHeapListNode(TargetPointer.Null, codeRangeStart, codeRangeStart + codeRangeSize, codeRangeStart, nibBuilder.NibbleMapFragment.Address);
        TargetPointer rangeSectionAddress = emBuilder.InsertRangeSection(codeRangeStart, codeRangeSize, jitManagerAddress: jitManagerAddress, codeHeapListNodeAddress: codeHeapListNodeAddress);
        TargetPointer rangeSectionFragmentAddress = emBuilder.InsertAddressRange(codeRangeStart, codeRangeSize, rangeSectionAddress);


        emBuilder.AllocateMethod(methodStart, methodSize, expectedMethodDescAddress);

        emBuilder.MarkCreated();

        ExecutionManagerTestTarget target = new(arch, emBuilder.Builder.GetReadContext().ReadFromTarget, emBuilder.TypeInfoCache);

        // test

        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetEECodeInfoHandle(methodStart);
        Assert.NotNull(eeInfo);
        TargetPointer actualMethodDesc = em.GetMethodDesc(eeInfo.Value);
        Assert.Equal(expectedMethodDescAddress, actualMethodDesc);
    }
}
