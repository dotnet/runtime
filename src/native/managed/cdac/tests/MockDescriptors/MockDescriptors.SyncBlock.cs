// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    public class SyncBlock
    {
        private const ulong DefaultAllocationRangeStart = 0x0001_0000;
        private const ulong DefaultAllocationRangeEnd = 0x0002_0000;

        private const ulong TestSyncBlockCacheGlobalAddress = 0x0000_0100;
        private const ulong TestSyncTableEntriesGlobalAddress = 0x0000_0200;
        private const ulong TestSyncTableEntriesAddress = 0x0000_0300;

        internal Dictionary<DataType, Target.TypeInfo> Types { get; }
        internal (string Name, ulong Value)[] Globals { get; }
        internal MockMemorySpace.Builder Builder { get; }

        private readonly MockMemorySpace.BumpAllocator _allocator;
        private readonly TargetPointer _syncBlockCacheAddress;
        private TargetPointer _cleanupListHead = TargetPointer.Null;

        public SyncBlock(MockMemorySpace.Builder builder)
            : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        { }

        public SyncBlock(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
        {
            Builder = builder;
            _allocator = builder.CreateAllocator(allocationRange.Start, allocationRange.End);

            TargetTestHelpers helpers = builder.TargetTestHelpers;
            Types = GetTypes(helpers);

            // Allocate SyncBlockCache and a global pointer to it
            MockMemorySpace.HeapFragment syncBlockCacheGlobal = _allocator.Allocate((ulong)helpers.PointerSize, "[global pointer] SyncBlockCache");
            MockMemorySpace.HeapFragment syncBlockCache = _allocator.Allocate(Types[DataType.SyncBlockCache].Size.Value, "SyncBlockCache");
            helpers.WritePointer(syncBlockCacheGlobal.Data, syncBlockCache.Address);
            Builder.AddHeapFragments([syncBlockCacheGlobal, syncBlockCache]);
            _syncBlockCacheAddress = syncBlockCache.Address;

            // Set FreeSyncTableIndex = 1 (signals empty table)
            Span<byte> cacheData = Builder.BorrowAddressRange(_syncBlockCacheAddress, (int)Types[DataType.SyncBlockCache].Size.Value);
            helpers.Write(
                cacheData.Slice(Types[DataType.SyncBlockCache].Fields[nameof(Data.SyncBlockCache.FreeSyncTableIndex)].Offset),
                (uint)1);

            // Set up a dummy SyncTableEntries global (not used by cleanup path)
            MockMemorySpace.HeapFragment syncTableEntriesGlobal = _allocator.Allocate((ulong)helpers.PointerSize, "[global pointer] SyncTableEntries");
            helpers.WritePointer(syncTableEntriesGlobal.Data, new TargetPointer(TestSyncTableEntriesAddress));
            Builder.AddHeapFragment(syncTableEntriesGlobal);

            Globals =
            [
                (nameof(Constants.Globals.SyncBlockCache), syncBlockCacheGlobal.Address),
                (nameof(Constants.Globals.SyncTableEntries), syncTableEntriesGlobal.Address),
            ];
        }

        private static Dictionary<DataType, Target.TypeInfo> GetTypes(TargetTestHelpers helpers)
            => GetTypesForTypeFields(helpers, [SyncBlockCacheFields, SyncBlockFields, InteropSyncBlockFields]);

        /// <summary>
        /// Prepends a new SyncBlock to the cleanup list.
        /// </summary>
        /// <param name="rcw">RCW pointer to store (pass <see cref="TargetPointer.Null"/> for none).</param>
        /// <param name="ccw">CCW pointer to store (pass <see cref="TargetPointer.Null"/> for none).</param>
        /// <param name="ccf">CCF pointer to store (pass <see cref="TargetPointer.Null"/> for none).</param>
        /// <param name="hasInteropInfo">When false, the InteropInfo pointer in the SyncBlock is left null.</param>
        /// <returns>The address of the newly allocated SyncBlock.</returns>
        internal TargetPointer AddSyncBlockToCleanupList(
            TargetPointer rcw, TargetPointer ccw, TargetPointer ccf, bool hasInteropInfo = true)
        {
            TargetTestHelpers helpers = Builder.TargetTestHelpers;
            Target.TypeInfo syncBlockTypeInfo = Types[DataType.SyncBlock];
            Target.TypeInfo interopTypeInfo = Types[DataType.InteropSyncBlockInfo];

            uint syncBlockSize = syncBlockTypeInfo.Size.Value;
            uint totalSize = syncBlockSize + (hasInteropInfo ? interopTypeInfo.Size.Value : 0u);

            MockMemorySpace.HeapFragment fragment = _allocator.Allocate(totalSize, "SyncBlock (cleanup)");
            TargetPointer syncBlockAddr = fragment.Address;

            Span<byte> syncBlockData = fragment.Data.AsSpan().Slice(0, (int)syncBlockSize);

            if (hasInteropInfo)
            {
                TargetPointer interopAddr = syncBlockAddr + syncBlockSize;
                Span<byte> interopData = fragment.Data.AsSpan().Slice((int)syncBlockSize);
                helpers.WritePointer(interopData.Slice(interopTypeInfo.Fields[nameof(Data.InteropSyncBlockInfo.RCW)].Offset), rcw);
                helpers.WritePointer(interopData.Slice(interopTypeInfo.Fields[nameof(Data.InteropSyncBlockInfo.CCW)].Offset), ccw);
                helpers.WritePointer(interopData.Slice(interopTypeInfo.Fields[nameof(Data.InteropSyncBlockInfo.CCF)].Offset), ccf);
                helpers.WritePointer(syncBlockData.Slice(syncBlockTypeInfo.Fields[nameof(Data.SyncBlock.InteropInfo)].Offset), interopAddr);
            }

            // LinkNext = m_Link.m_pNext -> points to the current head's m_Link (prepend to list)
            helpers.WritePointer(
                syncBlockData.Slice(syncBlockTypeInfo.Fields[nameof(Data.SyncBlock.LinkNext)].Offset),
                _cleanupListHead);

            Builder.AddHeapFragment(fragment);

            // The CleanupBlockList pointer points to the m_Link field of this SyncBlock.
            // m_Link is at the same offset as LinkNext (since SLink.m_pNext is the first field).
            ulong linkOffset = (ulong)syncBlockTypeInfo.Fields[nameof(Data.SyncBlock.LinkNext)].Offset;
            _cleanupListHead = new TargetPointer(syncBlockAddr.Value + linkOffset);
            UpdateCleanupBlockList(_cleanupListHead);

            return syncBlockAddr;
        }

        private void UpdateCleanupBlockList(TargetPointer newHead)
        {
            TargetTestHelpers helpers = Builder.TargetTestHelpers;
            Target.TypeInfo cacheTypeInfo = Types[DataType.SyncBlockCache];
            Span<byte> cacheData = Builder.BorrowAddressRange(_syncBlockCacheAddress, (int)cacheTypeInfo.Size.Value);
            helpers.WritePointer(
                cacheData.Slice(cacheTypeInfo.Fields[nameof(Data.SyncBlockCache.CleanupBlockList)].Offset),
                newHead);
        }
    }
}
