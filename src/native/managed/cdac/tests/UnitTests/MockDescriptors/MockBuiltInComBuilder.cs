// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class MockComMethodTable : TypedView
{
    private const string FlagsFieldName = "Flags";
    private const string MethodTableFieldName = "MethodTable";

    internal static Layout<MockComMethodTable> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("ComMethodTable", architecture)
            .AddPointerField(FlagsFieldName)
            .AddPointerField(MethodTableFieldName)
            .Build<MockComMethodTable>();

    internal ulong Flags
    {
        get => ReadPointerField(FlagsFieldName);
        set => WritePointerField(FlagsFieldName, value);
    }

    internal ulong MethodTable
    {
        get => ReadPointerField(MethodTableFieldName);
        set => WritePointerField(MethodTableFieldName, value);
    }

    internal ulong VTable
        => Address + (ulong)Layout.Size;

    internal ulong GetVTableSlot(int index)
        => ReadPointer(GetVTableSlotSpan(index));

    internal void SetVTableSlot(int index, ulong value)
        => WritePointer(GetVTableSlotSpan(index), value);

    private Span<byte> GetVTableSlotSpan(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int pointerSize = Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        int vtableOffset = Layout.Size + (index * pointerSize);
        int vtableBytes = Memory.Length - Layout.Size;
        int slotCount = vtableBytes / pointerSize;
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, slotCount);

        return Memory.Span.Slice(vtableOffset, pointerSize);
    }
}

internal sealed class MockStdInterfaceDesc : TypedView
{
    private const string StdInterfaceKindFieldName = "StdInterfaceKind";

    internal static Layout<MockStdInterfaceDesc> CreateLayout(MockTarget.Architecture architecture)
    {
        int pointerSize = architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        LayoutBuilder builder = new("StdInterfaceDesc", architecture)
        {
            Size = pointerSize,
        };

        builder.AddField(StdInterfaceKindFieldName, 0, sizeof(uint));
        return builder.Build<MockStdInterfaceDesc>();
    }

    internal uint StdInterfaceKind
    {
        get => ReadUInt32Field(StdInterfaceKindFieldName);
        set => WriteUInt32Field(StdInterfaceKindFieldName, value);
    }

    internal ulong VTable
        => Address + (ulong)Layout.Size;

    internal ulong GetVTableSlot(int index)
        => ReadPointer(GetVTableSlotSpan(index));

    internal void SetVTableSlot(int index, ulong value)
        => WritePointer(GetVTableSlotSpan(index), value);

    private Span<byte> GetVTableSlotSpan(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int pointerSize = Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        int vtableOffset = Layout.Size + (index * pointerSize);
        int vtableBytes = Memory.Length - Layout.Size;
        int slotCount = vtableBytes / pointerSize;
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, slotCount);

        return Memory.Span.Slice(vtableOffset, pointerSize);
    }
}

internal sealed class MockInterfaceEntry : TypedView
{
    internal static Layout<MockInterfaceEntry> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("InterfaceEntry", architecture)
            .AddPointerField("MethodTable")
            .AddPointerField("Unknown")
            .Build<MockInterfaceEntry>();

    internal ulong MethodTable
    {
        get => ReadPointerField("MethodTable");
        set => WritePointerField("MethodTable", value);
    }

    internal ulong Unknown
    {
        get => ReadPointerField("Unknown");
        set => WritePointerField("Unknown", value);
    }
}

internal sealed class MockCtxEntry : TypedView
{
    internal static Layout<MockCtxEntry> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("CtxEntry", architecture)
            .AddPointerField("STAThread")
            .AddPointerField("CtxCookie")
            .Build<MockCtxEntry>();

    internal ulong STAThread
    {
        get => ReadPointerField("STAThread");
        set => WritePointerField("STAThread", value);
    }

    internal ulong CtxCookie
    {
        get => ReadPointerField("CtxCookie");
        set => WritePointerField("CtxCookie", value);
    }
}

internal sealed class MockRCW : TypedView
{
    internal const uint InterfaceEntryCacheSize = 8;

    internal MockRCW()
    {
        InterfaceEntries = new InterfaceEntryCollection(this);
    }

    internal static Layout<MockRCW> CreateLayout(MockTarget.Architecture architecture)
    {
        int interfaceEntriesSize = checked(MockInterfaceEntry.CreateLayout(architecture).Size * (int)InterfaceEntryCacheSize);
        return new SequentialLayoutBuilder("RCW", architecture)
            .AddPointerField("NextCleanupBucket")
            .AddPointerField("NextRCW")
            .AddUInt32Field("Flags")
            .AddPointerField("CtxCookie")
            .AddPointerField("CtxEntry")
            .AddField("InterfaceEntries", interfaceEntriesSize)
            .AddPointerField("IdentityPointer")
            .AddUInt32Field("SyncBlockIndex")
            .AddPointerField("VTablePtr")
            .AddPointerField("CreatorThread")
            .AddUInt32Field("RefCount")
            .AddPointerField("UnknownPointer")
            .Build<MockRCW>();
    }

    internal InterfaceEntryCollection InterfaceEntries { get; }

    internal ulong IdentityPointer
    {
        get => ReadPointerField("IdentityPointer");
        set => WritePointerField("IdentityPointer", value);
    }

    internal ulong UnknownPointer
    {
        get => ReadPointerField("UnknownPointer");
        set => WritePointerField("UnknownPointer", value);
    }

    internal ulong VTablePtr
    {
        get => ReadPointerField("VTablePtr");
        set => WritePointerField("VTablePtr", value);
    }

    internal ulong CreatorThread
    {
        get => ReadPointerField("CreatorThread");
        set => WritePointerField("CreatorThread", value);
    }

    internal ulong CtxCookie
    {
        get => ReadPointerField("CtxCookie");
        set => WritePointerField("CtxCookie", value);
    }

    internal ulong CtxEntry
    {
        get => ReadPointerField("CtxEntry");
        set => WritePointerField("CtxEntry", value);
    }

    internal uint SyncBlockIndex
    {
        get => ReadUInt32Field("SyncBlockIndex");
        set => WriteUInt32Field("SyncBlockIndex", value);
    }

    internal uint RefCount
    {
        get => ReadUInt32Field("RefCount");
        set => WriteUInt32Field("RefCount", value);
    }

    internal uint Flags
    {
        get => ReadUInt32Field("Flags");
        set => WriteUInt32Field("Flags", value);
    }

    internal sealed class InterfaceEntryCollection
    {
        private readonly MockRCW _rcw;

        internal InterfaceEntryCollection(MockRCW rcw)
        {
            _rcw = rcw;
        }

        internal MockInterfaceEntry this[int index]
            => _rcw.GetInterfaceEntry(index);
    }

    private MockInterfaceEntry GetInterfaceEntry(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, (int)InterfaceEntryCacheSize);

        int interfaceEntrySize = 2 * (Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint));
        int offset = Layout.GetField("InterfaceEntries").Offset + (index * interfaceEntrySize);
        Layout<MockInterfaceEntry> interfaceEntryLayout = MockInterfaceEntry.CreateLayout(Architecture);
        return interfaceEntryLayout.Create(
            Memory.Slice(offset, interfaceEntryLayout.Size),
            Address + (ulong)offset);
    }
}

internal sealed class MockSimpleComCallWrapper : TypedView
{
    private const int InlineVTablePointerCount = 2;

    internal MockSimpleComCallWrapper()
    {
        VTablePointers = new VTablePointerCollection(this);
    }

    internal static Layout<MockSimpleComCallWrapper> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("SimpleComCallWrapper", architecture)
            .AddPointerField("OuterIUnknown")
            .AddUInt64Field("RefCount")
            .AddUInt32Field("Flags")
            .AddPointerField("MainWrapper")
            .AddField("VTablePtr", InlineVTablePointerCount * (architecture.Is64Bit ? sizeof(ulong) : sizeof(uint)))
            .Build<MockSimpleComCallWrapper>();

    internal ulong VTablePointerAddress => GetFieldAddress("VTablePtr");

    internal VTablePointerCollection VTablePointers { get; }

    internal ulong OuterIUnknown
    {
        get => ReadPointerField("OuterIUnknown");
        set => WritePointerField("OuterIUnknown", value);
    }

    internal ulong RefCount
    {
        get => ReadUInt64Field("RefCount");
        set => WriteUInt64Field("RefCount", value);
    }

    internal uint Flags
    {
        get => ReadUInt32Field("Flags");
        set => WriteUInt32Field("Flags", value);
    }

    internal ulong MainWrapper
    {
        get => ReadPointerField("MainWrapper");
        set => WritePointerField("MainWrapper", value);
    }

    internal sealed class VTablePointerCollection
    {
        private readonly MockSimpleComCallWrapper _wrapper;

        internal VTablePointerCollection(MockSimpleComCallWrapper wrapper)
        {
            _wrapper = wrapper;
        }

        internal ulong this[int index]
        {
            get => _wrapper.ReadPointer(_wrapper.GetVTablePointerSpan(index));
            set => _wrapper.WritePointer(_wrapper.GetVTablePointerSpan(index), value);
        }
    }

    private Span<byte> GetVTablePointerSpan(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int pointerSize = Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        int vtableOffset = Layout.GetField("VTablePtr").Offset;
        int slotCount = (Memory.Length - vtableOffset) / pointerSize;
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, slotCount);

        return Memory.Span.Slice(vtableOffset + (index * pointerSize), pointerSize);
    }
}

internal sealed class MockComCallWrapper : TypedView
{
    internal MockComCallWrapper()
    {
        InterfacePointers = new InterfacePointerCollection(this);
    }

    internal static Layout<MockComCallWrapper> CreateLayout(MockTarget.Architecture architecture)
    {
        int pointerSize = architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        LayoutBuilder builder = new("ComCallWrapper", architecture)
        {
            Size = checked(8 * pointerSize),
        };

        builder.AddField("Handle", 0, pointerSize);
        builder.AddField("SimpleWrapper", pointerSize, pointerSize);
        builder.AddField("IPtr", 2 * pointerSize, pointerSize);
        builder.AddField("Next", 7 * pointerSize, pointerSize);
        return builder.Build<MockComCallWrapper>();
    }

    internal ulong InterfacePointerAddress => GetFieldAddress("IPtr");

    internal InterfacePointerCollection InterfacePointers { get; }

    internal ulong Handle
    {
        get => ReadPointerField("Handle");
        set => WritePointerField("Handle", value);
    }

    internal ulong SimpleWrapper
    {
        get => ReadPointerField("SimpleWrapper");
        set => WritePointerField("SimpleWrapper", value);
    }

    internal ulong Next
    {
        get => ReadPointerField("Next");
        set => WritePointerField("Next", value);
    }

    internal sealed class InterfacePointerCollection
    {
        private readonly MockComCallWrapper _wrapper;

        internal InterfacePointerCollection(MockComCallWrapper wrapper)
        {
            _wrapper = wrapper;
        }

        internal ulong this[int index]
        {
            get => _wrapper.ReadPointer(_wrapper.GetInterfacePointerSpan(index));
            set => _wrapper.WritePointer(_wrapper.GetInterfacePointerSpan(index), value);
        }
    }

    private Span<byte> GetInterfacePointerSpan(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int pointerSize = Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        int interfacePointerOffset = Layout.GetField("IPtr").Offset;
        int nextOffset = Layout.GetField("Next").Offset;
        int slotCount = (nextOffset - interfacePointerOffset) / pointerSize;
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, slotCount);

        return Memory.Span.Slice(interfacePointerOffset + (index * pointerSize), pointerSize);
    }
}

internal sealed class MockBuiltInComBuilder
{
    private readonly MockMemorySpace.Builder _builder;
    private readonly MockMemorySpace.BumpAllocator _allocator;
    private readonly MockTarget.Architecture _architecture;

    internal const ulong DefaultComRefcountMask = 0x000000007FFFFFFF;
    internal const uint DefaultNumVtablePtrs = 5;
    internal const ulong DefaultTearOffAddRefAddress = 0xF000_0001;
    internal const ulong DefaultTearOffAddRefSimpleAddress = 0xF000_0002;
    internal const ulong DefaultTearOffAddRefSimpleInnerAddress = 0xF000_0003;

    internal MockBuiltInComBuilder(
        MockMemorySpace.Builder builder,
        MockMemorySpace.BumpAllocator allocator,
        MockTarget.Architecture architecture)
    {
        _builder = builder;
        _allocator = allocator;
        _architecture = architecture;

        SimpleComCallWrapperLayout = MockSimpleComCallWrapper.CreateLayout(architecture);
        ComCallWrapperLayout = MockComCallWrapper.CreateLayout(architecture);
        ComMethodTableLayout = MockComMethodTable.CreateLayout(architecture);
        StdInterfaceDescLayout = MockStdInterfaceDesc.CreateLayout(architecture);
        InterfaceEntryLayout = MockInterfaceEntry.CreateLayout(architecture);
        CtxEntryLayout = MockCtxEntry.CreateLayout(architecture);
        RCWLayout = MockRCW.CreateLayout(architecture);

        TearOffAddRefGlobalAddress = AddPointerGlobal("TearOffAddRef", TearOffAddRefAddress);
        TearOffAddRefSimpleGlobalAddress = AddPointerGlobal("TearOffAddRefSimple", TearOffAddRefSimpleAddress);
        TearOffAddRefSimpleInnerGlobalAddress = AddPointerGlobal("TearOffAddRefSimpleInner", TearOffAddRefSimpleInnerAddress);
    }

    internal ulong ComRefcountMask { get; } = DefaultComRefcountMask;

    internal uint CCWNumInterfaces { get; set; } = DefaultNumVtablePtrs;

    internal ulong CCWThisMask => GetCCWThisMask(_architecture);

    internal ulong TearOffAddRefAddress { get; } = DefaultTearOffAddRefAddress;

    internal ulong TearOffAddRefSimpleAddress { get; } = DefaultTearOffAddRefSimpleAddress;

    internal ulong TearOffAddRefSimpleInnerAddress { get; } = DefaultTearOffAddRefSimpleInnerAddress;

    internal ulong TearOffAddRefGlobalAddress { get; }

    internal ulong TearOffAddRefSimpleGlobalAddress { get; }

    internal ulong TearOffAddRefSimpleInnerGlobalAddress { get; }

    internal Layout<MockSimpleComCallWrapper> SimpleComCallWrapperLayout { get; }

    internal Layout<MockComCallWrapper> ComCallWrapperLayout { get; }

    internal Layout<MockComMethodTable> ComMethodTableLayout { get; }

    internal Layout<MockStdInterfaceDesc> StdInterfaceDescLayout { get; }

    internal Layout<MockInterfaceEntry> InterfaceEntryLayout { get; }

    internal Layout<MockCtxEntry> CtxEntryLayout { get; }

    internal Layout<MockRCW> RCWLayout { get; }

    internal MockSimpleComCallWrapper AddSimpleComCallWrapper()
        => SimpleComCallWrapperLayout.Create(_allocator.Allocate((ulong)SimpleComCallWrapperLayout.Size, "SimpleComCallWrapper"));

    internal MockComCallWrapper AddComCallWrapper()
        => ComCallWrapperLayout.Create(_allocator.Allocate((ulong)ComCallWrapperLayout.Size, "ComCallWrapper"));

    internal MockComMethodTable AddComMethodTable(int vtableSlots = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(vtableSlots);

        int pointerSize = ComMethodTableLayout.Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        int totalSize = checked(ComMethodTableLayout.Size + (vtableSlots * pointerSize));
        return ComMethodTableLayout.Create(_allocator.Allocate((ulong)totalSize, "ComMethodTable"));
    }

    internal MockStdInterfaceDesc AddStdInterfaceDesc(int vtableSlots = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(vtableSlots);

        int pointerSize = StdInterfaceDescLayout.Architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        int totalSize = checked(StdInterfaceDescLayout.Size + (vtableSlots * pointerSize));
        return StdInterfaceDescLayout.Create(_allocator.Allocate((ulong)totalSize, "StdInterfaceDesc"));
    }

    internal MockRCW AddRCW()
        => RCWLayout.Create(_allocator.Allocate((ulong)RCWLayout.Size, "Full RCW"));

    internal ulong AddCtxEntry(ulong staThread = 0, ulong ctxCookie = 0)
    {
        MockCtxEntry entry = CtxEntryLayout.Create(_allocator.Allocate((ulong)CtxEntryLayout.Size, "CtxEntry"));
        entry.STAThread = staThread;
        entry.CtxCookie = ctxCookie;
        return entry.Address;
    }

    internal static ulong GetCCWThisMask(MockTarget.Architecture architecture)
        => architecture.Is64Bit ? ~0x3FUL : ~0x1FUL;

    private ulong AddPointerGlobal(string name, ulong value)
    {
        int pointerSize = _architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        MockMemorySpace.HeapFragment global = _allocator.Allocate((ulong)pointerSize, $"[global pointer] {name}");
        _builder.TargetTestHelpers.WritePointer(global.Data.AsSpan(), value);
        return global.Address;
    }
}
