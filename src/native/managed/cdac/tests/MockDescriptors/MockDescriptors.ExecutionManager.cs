// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class MockRangeSectionMap : TypedView
{
    private const string TopLevelDataFieldName = "TopLevelData";

    public static Layout<MockRangeSectionMap> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("RangeSectionMap", architecture)
            .AddPointerField(TopLevelDataFieldName)
            .Build<MockRangeSectionMap>();
}

internal sealed class MockRangeSectionMapLevel : TypedView
{
    public const int EntriesPerMapLevel = 256;

    public static Layout<MockRangeSectionMapLevel> CreateLayout(MockTarget.Architecture architecture)
    {
        // Map levels are fixed-size arrays of pointers rather than named struct fields, so use
        // LayoutBuilder directly and access entries by index through the typed view helpers below.
        LayoutBuilder layoutBuilder = new("RangeSectionMapLevel", architecture)
        {
            Size = checked(EntriesPerMapLevel * (architecture.Is64Bit ? sizeof(ulong) : sizeof(uint))),
        };

        return layoutBuilder.Build<MockRangeSectionMapLevel>();
    }

    public ulong GetPointer(int index)
        => ReadPointer(GetEntrySlice(index));

    public void SetPointer(int index, ulong value)
        => WritePointer(GetEntrySlice(index), value);

    private Span<byte> GetEntrySlice(int index)
        => Memory.Span.Slice(GetEntryOffset(index), Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint));

    private int GetEntryOffset(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, EntriesPerMapLevel);
        return checked(index * (Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint)));
    }
}

internal sealed class MockRangeSectionFragment : TypedView
{
    private const string RangeBeginFieldName = "RangeBegin";
    private const string RangeEndOpenFieldName = "RangeEndOpen";
    private const string RangeSectionFieldName = "RangeSection";
    private const string NextFieldName = "Next";

    public static Layout<MockRangeSectionFragment> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("RangeSectionFragment", architecture)
            .AddPointerField(RangeBeginFieldName)
            .AddPointerField(RangeEndOpenFieldName)
            .AddPointerField(RangeSectionFieldName)
            .AddPointerField(NextFieldName)
            .Build<MockRangeSectionFragment>();

    public ulong RangeBegin
    {
        get => ReadPointerField(RangeBeginFieldName);
        set => WritePointerField(RangeBeginFieldName, value);
    }

    public ulong RangeEndOpen
    {
        get => ReadPointerField(RangeEndOpenFieldName);
        set => WritePointerField(RangeEndOpenFieldName, value);
    }

    public ulong RangeSection
    {
        get => ReadPointerField(RangeSectionFieldName);
        set => WritePointerField(RangeSectionFieldName, value);
    }

    public ulong Next
    {
        get => ReadPointerField(NextFieldName);
        set => WritePointerField(NextFieldName, value);
    }
}

internal sealed class MockRangeSection : TypedView
{
    private const string RangeBeginFieldName = "RangeBegin";
    private const string RangeEndOpenFieldName = "RangeEndOpen";
    private const string NextForDeleteFieldName = "NextForDelete";
    private const string JitManagerFieldName = "JitManager";
    private const string FlagsFieldName = "Flags";
    private const string HeapListFieldName = "HeapList";
    private const string R2RModuleFieldName = "R2RModule";

    public static Layout<MockRangeSection> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("RangeSection", architecture)
            .AddPointerField(RangeBeginFieldName)
            .AddPointerField(RangeEndOpenFieldName)
            .AddPointerField(NextForDeleteFieldName)
            .AddPointerField(JitManagerFieldName)
            .AddUInt32Field(FlagsFieldName)
            .AddPointerField(HeapListFieldName)
            .AddPointerField(R2RModuleFieldName)
            .Build<MockRangeSection>();

    public ulong RangeBegin
    {
        get => ReadPointerField(RangeBeginFieldName);
        set => WritePointerField(RangeBeginFieldName, value);
    }

    public ulong RangeEndOpen
    {
        get => ReadPointerField(RangeEndOpenFieldName);
        set => WritePointerField(RangeEndOpenFieldName, value);
    }

    public ulong JitManager
    {
        get => ReadPointerField(JitManagerFieldName);
        set => WritePointerField(JitManagerFieldName, value);
    }

    public uint Flags
    {
        get => ReadUInt32Field(FlagsFieldName);
        set => WriteUInt32Field(FlagsFieldName, value);
    }

    public ulong HeapList
    {
        get => ReadPointerField(HeapListFieldName);
        set => WritePointerField(HeapListFieldName, value);
    }

    public ulong R2RModule
    {
        get => ReadPointerField(R2RModuleFieldName);
        set => WritePointerField(R2RModuleFieldName, value);
    }
}

internal sealed class MockCodeHeapListNode : TypedView
{
    private const string NextFieldName = "Next";
    private const string StartAddressFieldName = "StartAddress";
    private const string EndAddressFieldName = "EndAddress";
    private const string MapBaseFieldName = "MapBase";
    private const string HeaderMapFieldName = "HeaderMap";
    private const string HeapFieldName = "Heap";

    public static Layout<MockCodeHeapListNode> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("CodeHeapListNode", architecture)
            .AddPointerField(NextFieldName)
            .AddPointerField(StartAddressFieldName)
            .AddPointerField(EndAddressFieldName)
            .AddPointerField(MapBaseFieldName)
            .AddPointerField(HeaderMapFieldName)
            .AddPointerField(HeapFieldName)
            .Build<MockCodeHeapListNode>();

    public ulong Next
    {
        get => ReadPointerField(NextFieldName);
        set => WritePointerField(NextFieldName, value);
    }

    public ulong StartAddress
    {
        get => ReadPointerField(StartAddressFieldName);
        set => WritePointerField(StartAddressFieldName, value);
    }

    public ulong EndAddress
    {
        get => ReadPointerField(EndAddressFieldName);
        set => WritePointerField(EndAddressFieldName, value);
    }

    public ulong MapBase
    {
        get => ReadPointerField(MapBaseFieldName);
        set => WritePointerField(MapBaseFieldName, value);
    }

    public ulong HeaderMap
    {
        get => ReadPointerField(HeaderMapFieldName);
        set => WritePointerField(HeaderMapFieldName, value);
    }

    public ulong Heap
    {
        get => ReadPointerField(HeapFieldName);
        set => WritePointerField(HeapFieldName, value);
    }
}

internal sealed class MockCodeHeap : TypedView
{
    private const string VtablePtrFieldName = "VtablePtr";
    private const string HeapTypePaddingFieldName = "HeapTypePadding";
    private const string HeapTypeFieldName = "HeapType";

    public static Layout<MockCodeHeap> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("CodeHeap", architecture)
            .AddPointerField(VtablePtrFieldName)
            .AddPointerField(HeapTypePaddingFieldName)
            .AddField(HeapTypeFieldName, sizeof(byte))
            .Build<MockCodeHeap>();

    public byte HeapType
    {
        get => ReadByteField(HeapTypeFieldName);
        set => WriteByteField(HeapTypeFieldName, value);
    }
}

internal sealed class MockLoaderCodeHeap : TypedView
{
    private const string LoaderHeapFieldName = "LoaderHeap";

    public static Layout<MockLoaderCodeHeap> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("LoaderCodeHeap", architecture)
            .AddPointerField(LoaderHeapFieldName)
            .Build<MockLoaderCodeHeap>();

    public ulong LoaderHeapAddress => GetFieldAddress(LoaderHeapFieldName);
}

internal sealed class MockHostCodeHeap : TypedView
{
    private const string BaseAddressFieldName = "BaseAddress";
    private const string CurrentAddressFieldName = "CurrentAddress";

    public static Layout<MockHostCodeHeap> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("HostCodeHeap", architecture)
            .AddPointerField(BaseAddressFieldName)
            .AddPointerField(CurrentAddressFieldName)
            .Build<MockHostCodeHeap>();

    public ulong BaseAddress
    {
        get => ReadPointerField(BaseAddressFieldName);
        set => WritePointerField(BaseAddressFieldName, value);
    }

    public ulong CurrentAddress
    {
        get => ReadPointerField(CurrentAddressFieldName);
        set => WritePointerField(CurrentAddressFieldName, value);
    }
}

internal sealed class MockRealCodeHeader : TypedView
{
    private const string MethodDescFieldName = "MethodDesc";
    private const string DebugInfoFieldName = "DebugInfo";
    private const string EHInfoFieldName = "EHInfo";
    private const string GCInfoFieldName = "GCInfo";
    private const string NumUnwindInfosFieldName = "NumUnwindInfos";
    private const string UnwindInfosFieldName = "UnwindInfos";

    public static Layout<MockRealCodeHeader> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("RealCodeHeader", architecture)
            .AddPointerField(MethodDescFieldName)
            .AddPointerField(DebugInfoFieldName)
            .AddPointerField(EHInfoFieldName)
            .AddPointerField(GCInfoFieldName)
            .AddUInt32Field(NumUnwindInfosFieldName)
            .AddPointerField(UnwindInfosFieldName)
            .Build<MockRealCodeHeader>();

    public ulong MethodDesc
    {
        get => ReadPointerField(MethodDescFieldName);
        set => WritePointerField(MethodDescFieldName, value);
    }

    public ulong DebugInfo
    {
        get => ReadPointerField(DebugInfoFieldName);
        set => WritePointerField(DebugInfoFieldName, value);
    }

    public ulong EHInfo
    {
        get => ReadPointerField(EHInfoFieldName);
        set => WritePointerField(EHInfoFieldName, value);
    }

    public ulong GCInfo
    {
        get => ReadPointerField(GCInfoFieldName);
        set => WritePointerField(GCInfoFieldName, value);
    }

    public uint NumUnwindInfos
    {
        get => ReadUInt32Field(NumUnwindInfosFieldName);
        set => WriteUInt32Field(NumUnwindInfosFieldName, value);
    }

    public ulong UnwindInfos
    {
        get => ReadPointerField(UnwindInfosFieldName);
        set => WritePointerField(UnwindInfosFieldName, value);
    }
}

internal sealed class MockReadyToRunInfo : TypedView
{
    private const string ReadyToRunHeaderFieldName = "ReadyToRunHeader";
    private const string CompositeInfoFieldName = "CompositeInfo";
    private const string NumRuntimeFunctionsFieldName = "NumRuntimeFunctions";
    private const string RuntimeFunctionsFieldName = "RuntimeFunctions";
    private const string NumHotColdMapFieldName = "NumHotColdMap";
    private const string HotColdMapFieldName = "HotColdMap";
    private const string DelayLoadMethodCallThunksFieldName = "DelayLoadMethodCallThunks";
    private const string DebugInfoSectionFieldName = "DebugInfoSection";
    private const string ExceptionInfoSectionFieldName = "ExceptionInfoSection";
    private const string EntryPointToMethodDescMapFieldName = "EntryPointToMethodDescMap";
    private const string LoadedImageBaseFieldName = "LoadedImageBase";
    private const string CompositeFieldName = "Composite";

    public static Layout<MockReadyToRunInfo> CreateLayout(MockTarget.Architecture architecture, int hashMapStride)
        => new SequentialLayoutBuilder("ReadyToRunInfo", architecture)
            .AddPointerField(ReadyToRunHeaderFieldName)
            .AddPointerField(CompositeInfoFieldName)
            .AddUInt32Field(NumRuntimeFunctionsFieldName)
            .AddPointerField(RuntimeFunctionsFieldName)
            .AddUInt32Field(NumHotColdMapFieldName)
            .AddPointerField(HotColdMapFieldName)
            .AddPointerField(DelayLoadMethodCallThunksFieldName)
            .AddPointerField(DebugInfoSectionFieldName)
            .AddPointerField(ExceptionInfoSectionFieldName)
            .AddField(EntryPointToMethodDescMapFieldName, hashMapStride)
            .AddPointerField(LoadedImageBaseFieldName)
            .AddPointerField(CompositeFieldName)
            .Build<MockReadyToRunInfo>();

    public ulong CompositeInfo
    {
        get => ReadPointerField(CompositeInfoFieldName);
        set => WritePointerField(CompositeInfoFieldName, value);
    }

    public uint NumRuntimeFunctions
    {
        get => ReadUInt32Field(NumRuntimeFunctionsFieldName);
        set => WriteUInt32Field(NumRuntimeFunctionsFieldName, value);
    }

    public ulong RuntimeFunctions
    {
        get => ReadPointerField(RuntimeFunctionsFieldName);
        set => WritePointerField(RuntimeFunctionsFieldName, value);
    }

    public uint NumHotColdMap
    {
        get => ReadUInt32Field(NumHotColdMapFieldName);
        set => WriteUInt32Field(NumHotColdMapFieldName, value);
    }

    public ulong HotColdMap
    {
        get => ReadPointerField(HotColdMapFieldName);
        set => WritePointerField(HotColdMapFieldName, value);
    }

    public ulong ExceptionInfoSection
    {
        get => ReadPointerField(ExceptionInfoSectionFieldName);
        set => WritePointerField(ExceptionInfoSectionFieldName, value);
    }

    public ulong EntryPointToMethodDescMapAddress => GetFieldAddress(EntryPointToMethodDescMapFieldName);
}

internal sealed class MockEEJitManager : TypedView
{
    private const string StoreRichDebugInfoFieldName = "StoreRichDebugInfo";
    private const string AllCodeHeapsFieldName = "AllCodeHeaps";

    public static Layout<MockEEJitManager> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("EEJitManager", architecture)
            .AddField(StoreRichDebugInfoFieldName, sizeof(byte))
            .AddPointerField(AllCodeHeapsFieldName)
            .Build<MockEEJitManager>();

    public byte StoreRichDebugInfo
    {
        get => ReadByteField(StoreRichDebugInfoFieldName);
        set => WriteByteField(StoreRichDebugInfoFieldName, value);
    }

    public ulong AllCodeHeaps
    {
        get => ReadPointerField(AllCodeHeapsFieldName);
        set => WritePointerField(AllCodeHeapsFieldName, value);
    }
}

internal sealed class MockJittedMethod : TypedView
{
    private const string CodeHeaderFieldName = "CodeHeader";
    private const string CodeBytesFieldName = "CodeBytes";

    public static Layout<MockJittedMethod> CreateLayout(MockTarget.Architecture architecture, int codeSize)
        => new SequentialLayoutBuilder("JittedMethod", architecture)
            .AddPointerField(CodeHeaderFieldName)
            .AddField(CodeBytesFieldName, codeSize)
            .Build<MockJittedMethod>();

    public ulong CodeHeader
    {
        get => ReadPointerField(CodeHeaderFieldName);
        set => WritePointerField(CodeHeaderFieldName, value);
    }

    public ulong CodeAddress => GetFieldAddress(CodeBytesFieldName);

    public Memory<byte> CodeBytes
    {
        get
        {
            LayoutField codeBytesField = Layout.GetField(CodeBytesFieldName);
            return Memory.Slice(codeBytesField.Offset, codeBytesField.Size);
        }
    }
}

internal sealed class MockExecutionManagerBuilder
{
    private const uint CodeHeapRangeSectionFlag = 0x02;
    private const string EEJitManagerGlobalName = "EEJitManagerGlobalPointer";
    private const int RangeSectionMapBitsPerLevel = 8;

    public readonly struct AllocationRange
    {
        public ulong RangeSectionMapStart { get; init; }
        public ulong RangeSectionMapEnd { get; init; }
        public ulong NibbleMapStart { get; init; }
        public ulong NibbleMapEnd { get; init; }
        public ulong ExecutionManagerStart { get; init; }
        public ulong ExecutionManagerEnd { get; init; }
    }

    public static readonly AllocationRange DefaultAllocationRange = new()
    {
        RangeSectionMapStart = 0x00dd_0000,
        RangeSectionMapEnd = 0x00de_0000,
        NibbleMapStart = 0x00ee_0000,
        NibbleMapEnd = 0x00ef_0000,
        ExecutionManagerStart = 0x0033_4000,
        ExecutionManagerEnd = 0x0033_5000,
    };

    private readonly struct RangeSectionMapCursor
    {
        public RangeSectionMapCursor(MockRangeSectionMapLevel levelMap, int level, int index)
        {
            LevelMap = levelMap;
            Level = level;
            Index = index;
        }

        public MockRangeSectionMapLevel LevelMap { get; }
        public int Level { get; }
        public int Index { get; }
        public bool IsLeaf => Level == 1;
    }

    internal readonly struct JittedCodeRange
    {
        public MockMemorySpace.BumpAllocator Allocator { get; init; }
        public ulong RangeStart => Allocator.RangeStart;
        public ulong RangeEnd => Allocator.RangeEnd;
        public ulong RangeSize => RangeEnd - RangeStart;
    }

    internal int Version { get; }
    internal MockMemorySpace.Builder Builder { get; }
    internal Layout<MockRangeSectionMap> RangeSectionMapLayout { get; }
    internal Layout<MockRangeSectionFragment> RangeSectionFragmentLayout { get; }
    internal Layout<MockRangeSection> RangeSectionLayout { get; }
    internal Layout<MockCodeHeapListNode> CodeHeapListNodeLayout { get; }
    internal Layout<MockCodeHeap> CodeHeapLayout { get; }
    internal Layout<MockLoaderCodeHeap> LoaderCodeHeapLayout { get; }
    internal Layout<MockHostCodeHeap> HostCodeHeapLayout { get; }
    internal Layout<MockRealCodeHeader> RealCodeHeaderLayout { get; }
    internal Layout<MockReadyToRunInfo> ReadyToRunInfoLayout { get; }
    internal Layout<MockEEJitManager> EEJitManagerLayout { get; }
    internal Layout<MockLoaderModule> ModuleLayout { get; }
    internal Layout<MockRuntimeFunction> RuntimeFunctionLayout => _runtimeFunctions.RuntimeFunctionLayout;
    internal Layout<MockUnwindInfo> UnwindInfoLayout => _runtimeFunctions.UnwindInfoLayout;
    internal (string Name, ulong Value)[] Globals { get; }
    internal ulong EEJitManagerAddress { get; }
    internal ulong RangeSectionMapTopLevelAddress => _rangeSectionMapTopLevelAddress;

    private readonly MockRuntimeFunctionsBuilder _runtimeFunctions;
    private readonly MockMemorySpace.BumpAllocator _rangeSectionMapAllocator;
    private readonly MockMemorySpace.BumpAllocator _nibbleMapAllocator;
    private readonly MockMemorySpace.BumpAllocator _allocator;
    private readonly MockEEJitManager _eeJitManager;
    private readonly Layout<MockRangeSectionMapLevel> _rangeSectionMapLevelLayout;
    private readonly Dictionary<ulong, MockRangeSectionMapLevel> _rangeSectionMapLevels;
    private readonly ulong _rangeSectionMapTopLevelAddress;
    private readonly int _rangeSectionMapLevelsCount;
    private readonly int _rangeSectionMapMaxSetBit;

    internal MockExecutionManagerBuilder(int version, MockTarget.Architecture arch, AllocationRange allocationRange, ulong allCodeHeaps = 0)
        : this(version, new MockMemorySpace.Builder(new TargetTestHelpers(arch)), allocationRange, allCodeHeaps)
    {
    }

    internal MockExecutionManagerBuilder(int version, MockMemorySpace.Builder builder, AllocationRange allocationRange, ulong allCodeHeaps = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Version = version;
        Builder = builder;
        _runtimeFunctions = new MockRuntimeFunctionsBuilder(builder);
        _rangeSectionMapAllocator = Builder.CreateAllocator(allocationRange.RangeSectionMapStart, allocationRange.RangeSectionMapEnd);
        _nibbleMapAllocator = Builder.CreateAllocator(allocationRange.NibbleMapStart, allocationRange.NibbleMapEnd);
        _allocator = Builder.CreateAllocator(allocationRange.ExecutionManagerStart, allocationRange.ExecutionManagerEnd);

        MockTarget.Architecture architecture = Builder.TargetTestHelpers.Arch;
        _rangeSectionMapLevelLayout = MockRangeSectionMapLevel.CreateLayout(architecture);
        _rangeSectionMapLevels = [];
        _rangeSectionMapLevelsCount = architecture.Is64Bit ? 5 : 2;
        _rangeSectionMapMaxSetBit = architecture.Is64Bit ? 56 : 31;
        MockMemorySpace.HeapFragment topRangeSectionMapLevel = _rangeSectionMapAllocator.Allocate((ulong)_rangeSectionMapLevelLayout.Size, $"Map Level {_rangeSectionMapLevelsCount}");
        _rangeSectionMapTopLevelAddress = topRangeSectionMapLevel.Address;
        _rangeSectionMapLevels[_rangeSectionMapTopLevelAddress] = _rangeSectionMapLevelLayout.Create(topRangeSectionMapLevel);

        int hashMapStride = MockHashMap.CreateLayout(architecture).Size;
        RangeSectionMapLayout = MockRangeSectionMap.CreateLayout(architecture);
        RangeSectionFragmentLayout = MockRangeSectionFragment.CreateLayout(architecture);
        RangeSectionLayout = MockRangeSection.CreateLayout(architecture);
        CodeHeapListNodeLayout = MockCodeHeapListNode.CreateLayout(architecture);
        CodeHeapLayout = MockCodeHeap.CreateLayout(architecture);
        LoaderCodeHeapLayout = MockLoaderCodeHeap.CreateLayout(architecture);
        HostCodeHeapLayout = MockHostCodeHeap.CreateLayout(architecture);
        RealCodeHeaderLayout = MockRealCodeHeader.CreateLayout(architecture);
        ReadyToRunInfoLayout = MockReadyToRunInfo.CreateLayout(architecture, hashMapStride);
        EEJitManagerLayout = MockEEJitManager.CreateLayout(architecture);
        ModuleLayout = MockLoaderModule.CreateLayout(architecture);

        _eeJitManager = AllocateAndCreate(EEJitManagerLayout, "EEJitManager");
        _eeJitManager.AllCodeHeaps = allCodeHeaps;
        EEJitManagerAddress = _eeJitManager.Address;

        ulong eeJitManagerGlobalAddress = AddPointerGlobal(EEJitManagerAddress, EEJitManagerGlobalName);
        var globals = new List<(string Name, ulong Value)>
        {
            (nameof(Constants.Globals.ExecutionManagerCodeRangeMapAddress), _rangeSectionMapTopLevelAddress),
            (nameof(Constants.Globals.StubCodeBlockLast), 0x0F),
            (nameof(Constants.Globals.EEJitManagerAddress), eeJitManagerGlobalAddress),
        };
        globals.Add((nameof(Constants.Globals.HashMapSlotsPerBucket), MockHashMapBucket.SlotsPerBucket));
        globals.Add((nameof(Constants.Globals.HashMapValueMask), Builder.TargetTestHelpers.MaxSignedTargetAddress));
        Globals = [.. globals];
    }

    public void SetAllCodeHeaps(ulong headNodeAddress)
        => _eeJitManager.AllCodeHeaps = headNodeAddress;

    internal NibbleMapTestBuilderBase CreateNibbleMap(ulong codeRangeStart, uint codeRangeSize)
    {
        NibbleMapTestBuilderBase nibBuilder = Version switch
        {
            1 => new NibbleMapTestBuilder_1(codeRangeStart, codeRangeSize, _nibbleMapAllocator, Builder.TargetTestHelpers.Arch),
            2 => new NibbleMapTestBuilder_2(codeRangeStart, codeRangeSize, _nibbleMapAllocator, Builder.TargetTestHelpers.Arch),
            _ => throw new InvalidOperationException("Unknown version"),
        };

        return nibBuilder;
    }

    public JittedCodeRange AllocateJittedCodeRange(ulong codeRangeStart, uint codeRangeSize)
    {
        MockMemorySpace.BumpAllocator allocator = Builder.CreateAllocator(codeRangeStart, codeRangeStart + codeRangeSize, minAlign: 1);
        return new JittedCodeRange { Allocator = allocator };
    }

    public MockRangeSection AddRangeSection(JittedCodeRange jittedCodeRange, ulong jitManagerAddress, ulong codeHeapListNodeAddress)
    {
        MockRangeSection rangeSection = AllocateAndCreate(RangeSectionLayout, "RangeSection", _rangeSectionMapAllocator);
        rangeSection.RangeBegin = jittedCodeRange.RangeStart;
        rangeSection.RangeEndOpen = jittedCodeRange.RangeEnd;
        rangeSection.Flags = CodeHeapRangeSectionFlag;
        rangeSection.HeapList = codeHeapListNodeAddress;
        rangeSection.JitManager = jitManagerAddress;
        return rangeSection;
    }

    public MockRangeSection AddReadyToRunRangeSection(JittedCodeRange jittedCodeRange, ulong jitManagerAddress, ulong r2rModuleAddress)
    {
        MockRangeSection rangeSection = AllocateAndCreate(RangeSectionLayout, "RangeSection", _rangeSectionMapAllocator);
        rangeSection.RangeBegin = jittedCodeRange.RangeStart;
        rangeSection.RangeEndOpen = jittedCodeRange.RangeEnd;
        rangeSection.R2RModule = r2rModuleAddress;
        rangeSection.JitManager = jitManagerAddress;
        return rangeSection;
    }

    public MockRangeSectionFragment AddRangeSectionFragment(JittedCodeRange jittedCodeRange, ulong rangeSectionAddress)
        => AddRangeSectionFragment(jittedCodeRange, rangeSectionAddress, insertIntoMap: true);

    public MockRangeSectionFragment AddUnmappedRangeSectionFragment(JittedCodeRange jittedCodeRange, ulong rangeSectionAddress)
        => AddRangeSectionFragment(jittedCodeRange, rangeSectionAddress, insertIntoMap: false);

    public MockRangeSectionFragment AddRangeSectionFragmentWithCollectibleNext(JittedCodeRange mapCodeRange, ulong rangeSectionAddress, ulong nextFragmentAddress)
    {
        MockRangeSectionFragment rangeSectionFragment = AllocateAndCreate(
            RangeSectionFragmentLayout,
            "RangeSectionFragment (collectible head)",
            _rangeSectionMapAllocator);
        InsertMappedRange(mapCodeRange.RangeStart, (uint)mapCodeRange.RangeSize, rangeSectionFragment.Address);
        rangeSectionFragment.RangeSection = rangeSectionAddress;
        rangeSectionFragment.Next = nextFragmentAddress | 1;
        return rangeSectionFragment;
    }

    public MockCodeHeapListNode AddCodeHeapListNode(ulong next, ulong startAddress, ulong endAddress, ulong mapBase, ulong headerMap, ulong heap = 0)
    {
        MockCodeHeapListNode codeHeapListNode = AllocateAndCreate(CodeHeapListNodeLayout, "CodeHeapListNode", _rangeSectionMapAllocator);
        codeHeapListNode.Next = next;
        codeHeapListNode.StartAddress = startAddress;
        codeHeapListNode.EndAddress = endAddress;
        codeHeapListNode.MapBase = mapBase;
        codeHeapListNode.HeaderMap = headerMap;
        codeHeapListNode.Heap = heap;
        return codeHeapListNode;
    }

    public MockLoaderCodeHeap AddLoaderCodeHeap()
    {
        ulong allocationSize = (ulong)Math.Max(CodeHeapLayout.Size, LoaderCodeHeapLayout.Size);
        MockMemorySpace.HeapFragment heapFragment = _allocator.Allocate(allocationSize, "LoaderCodeHeap");
        MockLoaderCodeHeap loaderCodeHeap = LoaderCodeHeapLayout.Create(heapFragment);
        MockCodeHeap codeHeap = CodeHeapLayout.Create(
            heapFragment.Data.AsMemory(0, CodeHeapLayout.Size),
            heapFragment.Address);
        codeHeap.HeapType = 0;
        return loaderCodeHeap;
    }

    public MockHostCodeHeap AddHostCodeHeap(ulong baseAddress, ulong currentAddress)
    {
        ulong allocationSize = (ulong)Math.Max(CodeHeapLayout.Size, HostCodeHeapLayout.Size);
        MockMemorySpace.HeapFragment heapFragment = _allocator.Allocate(allocationSize, "HostCodeHeap");
        MockHostCodeHeap hostCodeHeap = HostCodeHeapLayout.Create(heapFragment);
        MockCodeHeap codeHeap = CodeHeapLayout.Create(
            heapFragment.Data.AsMemory(0, CodeHeapLayout.Size),
            heapFragment.Address);
        codeHeap.HeapType = 1;
        hostCodeHeap.BaseAddress = baseAddress;
        hostCodeHeap.CurrentAddress = currentAddress;
        return hostCodeHeap;
    }

    public MockJittedMethod AddJittedMethod(JittedCodeRange jittedCodeRange, uint codeSize, ulong methodDescAddress)
    {
        MockJittedMethod jittedMethod = AllocateJittedMethod(jittedCodeRange, codeSize);
        MockRealCodeHeader codeHeader = AllocateAndCreate(RealCodeHeaderLayout, "RealCodeHeader");
        jittedMethod.CodeHeader = codeHeader.Address;

        codeHeader.MethodDesc = methodDescAddress;
        codeHeader.DebugInfo = 0;
        codeHeader.EHInfo = 0;
        codeHeader.GCInfo = 0;
        codeHeader.NumUnwindInfos = 0;
        codeHeader.UnwindInfos = 0;

        return jittedMethod;
    }

    public MockReadyToRunInfo AddReadyToRunInfo(uint[] runtimeFunctions, uint[] hotColdMap)
    {
        ulong runtimeFunctionsAddress = _runtimeFunctions.AddRuntimeFunctions(runtimeFunctions);
        ulong hotColdMapAddress = 0;
        if (hotColdMap.Length > 0)
        {
            MockMemorySpace.HeapFragment hotColdMapFragment = _allocator.Allocate((ulong)hotColdMap.Length * sizeof(uint), $"HotColdMap[{hotColdMap.Length}]");
            hotColdMapAddress = hotColdMapFragment.Address;
            for (uint i = 0; i < hotColdMap.Length; i++)
            {
                Span<byte> span = Builder.BorrowAddressRange(hotColdMapFragment.Address + i * sizeof(uint), sizeof(uint));
                Builder.TargetTestHelpers.Write(span, hotColdMap[i]);
            }
        }

        MockReadyToRunInfo readyToRunInfo = AllocateAndCreate(ReadyToRunInfoLayout, "ReadyToRunInfo");
        readyToRunInfo.CompositeInfo = readyToRunInfo.Address;
        readyToRunInfo.NumRuntimeFunctions = checked((uint)runtimeFunctions.Length);
        readyToRunInfo.RuntimeFunctions = runtimeFunctionsAddress;
        readyToRunInfo.NumHotColdMap = checked((uint)hotColdMap.Length);
        readyToRunInfo.HotColdMap = hotColdMapAddress;
        return readyToRunInfo;
    }

    public MockLoaderModule AddReadyToRunModule(ulong readyToRunInfoAddress)
    {
        MockLoaderModule module = AllocateAndCreate(ModuleLayout, "R2R Module");
        module.ReadyToRunInfo = readyToRunInfoAddress;
        return module;
    }

    private MockRangeSectionFragment AddRangeSectionFragment(JittedCodeRange jittedCodeRange, ulong rangeSectionAddress, bool insertIntoMap)
    {
        MockRangeSectionFragment rangeSectionFragment = AllocateAndCreate(RangeSectionFragmentLayout, "RangeSectionFragment", _rangeSectionMapAllocator);
        if (insertIntoMap)
        {
            InsertMappedRange(jittedCodeRange.RangeStart, (uint)jittedCodeRange.RangeSize, rangeSectionFragment.Address);
        }

        rangeSectionFragment.RangeBegin = jittedCodeRange.RangeStart;
        rangeSectionFragment.RangeEndOpen = jittedCodeRange.RangeEnd;
        rangeSectionFragment.RangeSection = rangeSectionAddress;
        return rangeSectionFragment;
    }

    private void InsertMappedRange(ulong rangeStart, uint rangeSize, ulong value)
        => InsertRangeSectionMapAddressRange(rangeStart, rangeSize, value);

    internal void InsertRangeSectionMapAddressRange(ulong start, uint length, ulong value, bool collectible = false)
    {
        if (length == 0)
        {
            return;
        }

        ulong current = start;
        ulong end = start + length;
        do
        {
            RangeSectionMapCursor lastCursor = EnsureRangeSectionMapLevelsForAddress(current, collectible);
            lastCursor.LevelMap.SetPointer(lastCursor.Index, value);
            current += (ulong)RangeSectionMapBytesAtLastLevel;
        }
        while (current < end);
    }

    private MockJittedMethod AllocateJittedMethod(JittedCodeRange jittedCodeRange, uint codeSize, string name = "Method Header & Code")
    {
        Layout<MockJittedMethod> jittedMethodLayout = MockJittedMethod.CreateLayout(Builder.TargetTestHelpers.Arch, checked((int)codeSize));
        MockMemorySpace.HeapFragment methodFragment = jittedCodeRange.Allocator.Allocate((ulong)jittedMethodLayout.Size, name);
        return jittedMethodLayout.Create(methodFragment);
    }

    private ulong AddPointerGlobal(ulong value, string name)
    {
        MockMemorySpace.HeapFragment fragment = _allocator.Allocate((ulong)Builder.TargetTestHelpers.PointerSize, name);
        Builder.TargetTestHelpers.WritePointer(fragment.Data, value);
        return fragment.Address;
    }

    private TView AllocateAndCreate<TView>(Layout<TView> layout, string name, MockMemorySpace.BumpAllocator? allocator = null)
        where TView : TypedView, new()
        => layout.Create((allocator ?? _allocator).Allocate((ulong)layout.Size, name));

    private int RangeSectionMapBitsAtLastLevel => _rangeSectionMapMaxSetBit - RangeSectionMapBitsPerLevel * _rangeSectionMapLevelsCount + 1;

    private int RangeSectionMapBytesAtLastLevel => checked(1 << RangeSectionMapBitsAtLastLevel);

    private int GetRangeSectionMapIndexForLevel(ulong address, int level)
    {
        ulong addressBitsUsedInMap = address >> _rangeSectionMapMaxSetBit + 1 - _rangeSectionMapLevelsCount * RangeSectionMapBitsPerLevel;
        ulong addressBitsShifted = addressBitsUsedInMap >> (level - 1) * RangeSectionMapBitsPerLevel;
        return checked((int)(MockRangeSectionMapLevel.EntriesPerMapLevel - 1 & addressBitsShifted));
    }

    private MockMemorySpace.HeapFragment CreateRangeSectionMapLevelAtAddress(ulong address, string name)
    {
        MockMemorySpace.HeapFragment mapLevel = new()
        {
            Address = address,
            Data = new byte[_rangeSectionMapLevelLayout.Size],
            Name = name
        };
        Builder.AddHeapFragment(mapLevel);
        _rangeSectionMapLevels[address] = _rangeSectionMapLevelLayout.Create(mapLevel);
        return mapLevel;
    }

    private MockRangeSectionMapLevel AllocateRangeSectionMapLevel(int level)
    {
        MockMemorySpace.HeapFragment mapLevel = _rangeSectionMapAllocator.Allocate((ulong)_rangeSectionMapLevelLayout.Size, $"Map Level {level}");
        _rangeSectionMapLevels[mapLevel.Address] = _rangeSectionMapLevelLayout.Create(mapLevel);
        return _rangeSectionMapLevels[mapLevel.Address];
    }

    private RangeSectionMapCursor EnsureRangeSectionMapLevelsForAddress(ulong address, bool collectible)
    {
        int topIndex = GetRangeSectionMapIndexForLevel(address, _rangeSectionMapLevelsCount);
        RangeSectionMapCursor cursor = new(_rangeSectionMapLevels[_rangeSectionMapTopLevelAddress], _rangeSectionMapLevelsCount, topIndex);
        while (!cursor.IsLeaf)
        {
            int nextLevel = cursor.Level - 1;
            int nextIndex = GetRangeSectionMapIndexForLevel(address, nextLevel);
            ulong nextLevelMap = cursor.LevelMap.GetPointer(cursor.Index);
            if (nextLevelMap == 0)
            {
                nextLevelMap = AllocateRangeSectionMapLevel(nextLevel).Address;
                if (collectible)
                {
                    nextLevelMap |= 1;
                }

                cursor.LevelMap.SetPointer(cursor.Index, nextLevelMap);
            }

            cursor = new(_rangeSectionMapLevels[nextLevelMap & ~1UL], nextLevel, nextIndex);
        }

        return cursor;
    }
}
