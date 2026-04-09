// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class MockSyncBlockCache : TypedView
{
    private const string FreeSyncTableIndexFieldName = "FreeSyncTableIndex";
    private const string CleanupBlockListFieldName = "CleanupBlockList";

    public static Layout<MockSyncBlockCache> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("SyncBlockCache", architecture)
            .AddUInt32Field(FreeSyncTableIndexFieldName)
            .AddPointerField(CleanupBlockListFieldName)
            .Build<MockSyncBlockCache>();

    public uint FreeSyncTableIndex
    {
        get => ReadUInt32Field(FreeSyncTableIndexFieldName);
        set => WriteUInt32Field(FreeSyncTableIndexFieldName, value);
    }

    public ulong CleanupBlockList
    {
        get => ReadPointerField(CleanupBlockListFieldName);
        set => WritePointerField(CleanupBlockListFieldName, value);
    }
}

internal sealed class MockInteropSyncBlockInfo : TypedView
{
    private const string RCWFieldName = "RCW";
    private const string CCWFieldName = "CCW";
    private const string CCFFieldName = "CCF";

    public static Layout<MockInteropSyncBlockInfo> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("InteropSyncBlockInfo", architecture)
            .AddPointerField(RCWFieldName)
            .AddPointerField(CCWFieldName)
            .AddPointerField(CCFFieldName)
            .Build<MockInteropSyncBlockInfo>();

    public ulong RCW
    {
        get => ReadPointerField(RCWFieldName);
        set => WritePointerField(RCWFieldName, value);
    }

    public ulong CCW
    {
        get => ReadPointerField(CCWFieldName);
        set => WritePointerField(CCWFieldName, value);
    }

    public ulong CCF
    {
        get => ReadPointerField(CCFFieldName);
        set => WritePointerField(CCFFieldName, value);
    }
}

internal sealed class MockSyncBlock : TypedView
{
    private const string InteropInfoFieldName = "InteropInfo";
    private const string LinkNextFieldName = "LinkNext";

    public static Layout<MockSyncBlock> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("SyncBlock", architecture)
            .AddPointerField(InteropInfoFieldName)
            .AddPointerField("Lock")
            .AddUInt32Field("ThinLock")
            .AddPointerField(LinkNextFieldName)
            .AddUInt32Field("HashCode")
            .Build<MockSyncBlock>();

    public ulong InteropInfo
    {
        get => ReadPointerField(InteropInfoFieldName);
        set => WritePointerField(InteropInfoFieldName, value);
    }

    public ulong LinkNext
    {
        get => ReadPointerField(LinkNextFieldName);
        set => WritePointerField(LinkNextFieldName, value);
    }

    public ulong CleanupLinkAddress
        => GetFieldAddress(LinkNextFieldName);
}

internal sealed class MockSyncBlockBuilder
{
    private const ulong DefaultAllocationRangeStart = 0x0001_0000;
    private const ulong DefaultAllocationRangeEnd = 0x0002_0000;

    private const ulong TestSyncTableEntriesAddress = 0x0000_0300;

    internal MockMemorySpace.Builder Builder { get; }

    private readonly MockMemorySpace.BumpAllocator _allocator;
    private readonly MockSyncBlockCache? _syncBlockCache;
    private ulong _cleanupListHeadAddress;

    internal Layout<MockSyncBlockCache> SyncBlockCacheLayout { get; }

    internal Layout<MockSyncBlock> SyncBlockLayout { get; }

    internal Layout<MockInteropSyncBlockInfo> InteropSyncBlockInfoLayout { get; }

    internal ulong SyncBlockCacheGlobalAddress { get; }

    internal ulong SyncTableEntriesGlobalAddress { get; }

    public MockSyncBlockBuilder(MockMemorySpace.Builder builder)
        : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
    { }

    public MockSyncBlockBuilder(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
        : this(builder, builder.CreateAllocator(allocationRange.Start, allocationRange.End))
    {
    }

    public MockSyncBlockBuilder(MockMemorySpace.Builder builder, MockMemorySpace.BumpAllocator allocator)
        : this(builder, allocator, initializeCacheAndGlobals: true)
    {
    }

    internal MockSyncBlockBuilder(MockMemorySpace.Builder builder, MockMemorySpace.BumpAllocator allocator, bool initializeCacheAndGlobals)
    {
        Builder = builder;
        _allocator = allocator;

        TargetTestHelpers helpers = builder.TargetTestHelpers;
        SyncBlockCacheLayout = MockSyncBlockCache.CreateLayout(helpers.Arch);
        SyncBlockLayout = MockSyncBlock.CreateLayout(helpers.Arch);
        InteropSyncBlockInfoLayout = MockInteropSyncBlockInfo.CreateLayout(helpers.Arch);

        if (initializeCacheAndGlobals)
        {
            MockMemorySpace.HeapFragment syncBlockCacheFragment = AllocateAndAdd((ulong)SyncBlockCacheLayout.Size, "SyncBlockCache");
            _syncBlockCache = SyncBlockCacheLayout.Create(syncBlockCacheFragment);
            _syncBlockCache.FreeSyncTableIndex = 1;

            SyncBlockCacheGlobalAddress = AddPointerGlobal("SyncBlockCache", _syncBlockCache.Address);
            SyncTableEntriesGlobalAddress = AddPointerGlobal("SyncTableEntries", TestSyncTableEntriesAddress);
        }
    }

    internal MockSyncBlock AddSyncBlock(
        ulong rcw,
        ulong ccw,
        ulong ccf,
        bool hasInteropInfo = true,
        string name = "SyncBlock")
    {
        int totalSize = SyncBlockLayout.Size + (hasInteropInfo ? InteropSyncBlockInfoLayout.Size : 0);
        MockMemorySpace.HeapFragment fragment = AllocateAndAdd((ulong)totalSize, name);
        MockSyncBlock syncBlock = SyncBlockLayout.Create(
            fragment.Data.AsMemory(0, SyncBlockLayout.Size),
            fragment.Address);

        if (hasInteropInfo)
        {
            ulong interopAddress = syncBlock.Address + (ulong)SyncBlockLayout.Size;
            MockInteropSyncBlockInfo interopInfo = InteropSyncBlockInfoLayout.Create(
                fragment.Data.AsMemory(SyncBlockLayout.Size, InteropSyncBlockInfoLayout.Size),
                interopAddress);
            interopInfo.RCW = rcw;
            interopInfo.CCW = ccw;
            interopInfo.CCF = ccf;
            syncBlock.InteropInfo = interopAddress;
        }

        return syncBlock;
    }

    /// <summary>
    /// Prepends a new SyncBlock to the cleanup list.
    /// </summary>
    /// <param name="rcw">RCW pointer to store (pass 0 for none).</param>
    /// <param name="ccw">CCW pointer to store (pass 0 for none).</param>
    /// <param name="ccf">CCF pointer to store (pass 0 for none).</param>
    /// <param name="hasInteropInfo">When false, the InteropInfo pointer in the SyncBlock is left null.</param>
    internal MockSyncBlock AddSyncBlockToCleanupList(
        ulong rcw, ulong ccw, ulong ccf, bool hasInteropInfo = true)
    {
        if (_syncBlockCache is null)
        {
            throw new InvalidOperationException("Cleanup-list support requires the cache/global initialization path.");
        }

        MockSyncBlock syncBlock = AddSyncBlock(rcw, ccw, ccf, hasInteropInfo, "SyncBlock (cleanup)");
        syncBlock.LinkNext = _cleanupListHeadAddress;
        _cleanupListHeadAddress = syncBlock.CleanupLinkAddress;
        _syncBlockCache.CleanupBlockList = _cleanupListHeadAddress;
        return syncBlock;
    }

    private ulong AddPointerGlobal(string name, ulong value)
    {
        TargetTestHelpers helpers = Builder.TargetTestHelpers;
        MockMemorySpace.HeapFragment global = AllocateAndAdd((ulong)helpers.PointerSize, $"[global pointer] {name}");
        helpers.WritePointer(global.Data, value);
        return global.Address;
    }

    private MockMemorySpace.HeapFragment AllocateAndAdd(ulong size, string name)
    {
        MockMemorySpace.HeapFragment fragment = _allocator.Allocate(size, name);
        Builder.AddHeapFragment(fragment);
        return fragment;
    }
}
