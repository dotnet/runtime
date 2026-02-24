// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    public class Thread
    {
        private const ulong DefaultAllocationRangeStart = 0x0003_0000;
        private const ulong DefaultAllocationRangeEnd = 0x0004_0000;

        internal Dictionary<DataType, Target.TypeInfo> Types { get; }
        internal (string Name, ulong Value)[] Globals { get; }

        internal TargetPointer FinalizerThreadAddress { get; }
        internal TargetPointer GCThreadAddress { get; }

        internal MockMemorySpace.Builder Builder { get; }

        private readonly MockMemorySpace.BumpAllocator _allocator;

        private readonly TargetPointer _threadStoreAddress;

        // Most recently added thread. We update its link to the next thread if another thread is added.
        private TargetPointer _previousThread = TargetPointer.Null;

        public Thread(MockMemorySpace.Builder builder)
            : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        { }

        public Thread(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
        {
            Builder = builder;
            _allocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);

            TargetTestHelpers helpers = builder.TargetTestHelpers;

            Types = GetTypes(helpers);

            // Add thread store and set global to point at it
            MockMemorySpace.HeapFragment threadStoreGlobal = _allocator.Allocate((ulong)helpers.PointerSize, "[global pointer] ThreadStore");
            MockMemorySpace.HeapFragment threadStore = _allocator.Allocate(Types[DataType.ThreadStore].Size.Value, "ThreadStore");
            helpers.WritePointer(threadStoreGlobal.Data, threadStore.Address);
            Builder.AddHeapFragments([threadStoreGlobal, threadStore]);
            _threadStoreAddress = threadStore.Address;

            // Add finalizer thread and set global to point at it
            MockMemorySpace.HeapFragment finalizerThreadGlobal = _allocator.Allocate((ulong)helpers.PointerSize, "[global pointer] Finalizer thread");
            MockMemorySpace.HeapFragment finalizerThread = _allocator.Allocate(Types[DataType.Thread].Size.Value, "Finalizer thread");
            helpers.WritePointer(finalizerThreadGlobal.Data, finalizerThread.Address);
            Builder.AddHeapFragments([finalizerThreadGlobal, finalizerThread]);
            FinalizerThreadAddress = finalizerThread.Address;

            // Add GC thread and set global to point at it
            MockMemorySpace.HeapFragment gcThreadGlobal = _allocator.Allocate((ulong)helpers.PointerSize, "[global pointer] GC thread");
            MockMemorySpace.HeapFragment gcThread = _allocator.Allocate(Types[DataType.Thread].Size.Value, "GC thread");
            helpers.WritePointer(gcThreadGlobal.Data, gcThread.Address);
            Builder.AddHeapFragments([gcThreadGlobal, gcThread]);
            GCThreadAddress = gcThread.Address;

            Globals =
            [
                (nameof(Constants.Globals.ThreadStore), threadStoreGlobal.Address),
                (nameof(Constants.Globals.FinalizerThread), finalizerThreadGlobal.Address),
                (nameof(Constants.Globals.GCThread), gcThreadGlobal.Address),
            ];
        }

        private static Dictionary<DataType, Target.TypeInfo> GetTypes(TargetTestHelpers helpers)
        {
            var types = GetTypesForTypeFields(
                helpers,
                [
                    ExceptionInfoFields,
                    ThreadFields,
                    ThreadStoreFields,
                ]);

            // Compute layouts for embedded struct types (GCAllocContext -> EEAllocContext -> RuntimeThreadLocals)
            var gcAllocContextLayout = helpers.LayoutFields(
            [
                new(nameof(Data.GCAllocContext.Pointer), DataType.pointer),
                new(nameof(Data.GCAllocContext.Limit), DataType.pointer),
                new(nameof(Data.GCAllocContext.AllocBytes), DataType.int64),
                new(nameof(Data.GCAllocContext.AllocBytesLoh), DataType.int64),
            ]);
            types[DataType.GCAllocContext] = new Target.TypeInfo()
            {
                Fields = gcAllocContextLayout.Fields,
                Size = gcAllocContextLayout.Stride,
            };

            var eeAllocContextLayout = helpers.LayoutFields(
            [
                new(nameof(Data.EEAllocContext.GCAllocationContext), DataType.GCAllocContext, gcAllocContextLayout.Stride),
            ]);
            types[DataType.EEAllocContext] = new Target.TypeInfo()
            {
                Fields = eeAllocContextLayout.Fields,
                Size = eeAllocContextLayout.Stride,
            };

            var runtimeThreadLocalsLayout = helpers.LayoutFields(
            [
                new(nameof(Data.RuntimeThreadLocals.AllocContext), DataType.EEAllocContext, eeAllocContextLayout.Stride),
            ]);
            types[DataType.RuntimeThreadLocals] = new Target.TypeInfo()
            {
                Fields = runtimeThreadLocalsLayout.Fields,
                Size = runtimeThreadLocalsLayout.Stride,
            };

            return types;
        }

        internal void SetThreadCounts(int threadCount, int unstartedCount, int backgroundCount, int pendingCount, int deadCount)
        {
            TargetTestHelpers helpers = Builder.TargetTestHelpers;
            Target.TypeInfo typeInfo = Types[DataType.ThreadStore];
            Span<byte> data = Builder.BorrowAddressRange(_threadStoreAddress, (int)typeInfo.Size.Value);
            helpers.Write(
                data.Slice(typeInfo.Fields[nameof(Data.ThreadStore.ThreadCount)].Offset),
                threadCount);
            helpers.Write(
                data.Slice(typeInfo.Fields[nameof(Data.ThreadStore.UnstartedCount)].Offset),
                unstartedCount);
            helpers.Write(
                data.Slice(typeInfo.Fields[nameof(Data.ThreadStore.BackgroundCount)].Offset),
                backgroundCount);
            helpers.Write(
                data.Slice(typeInfo.Fields[nameof(Data.ThreadStore.PendingCount)].Offset),
                pendingCount);
            helpers.Write(
                data.Slice(typeInfo.Fields[nameof(Data.ThreadStore.DeadCount)].Offset),
                deadCount);
        }

        internal TargetPointer AddThread(uint id, TargetNUInt osId)
            => AddThread(id, osId, allocBytes: 0, allocBytesLoh: 0);

        internal TargetPointer AddThread(uint id, TargetNUInt osId, long allocBytes, long allocBytesLoh)
        {
            TargetTestHelpers helpers = Builder.TargetTestHelpers;
            Target.TypeInfo threadType = Types[DataType.Thread];
            Target.TypeInfo exceptionInfoType = Types[DataType.ExceptionInfo];
            Target.TypeInfo runtimeThreadLocalsType = Types[DataType.RuntimeThreadLocals];
            Target.TypeInfo eeAllocContextType = Types[DataType.EEAllocContext];
            Target.TypeInfo gcAllocContextType = Types[DataType.GCAllocContext];

            MockMemorySpace.HeapFragment exceptionInfo = _allocator.Allocate(exceptionInfoType.Size.Value, "ExceptionInfo");
            MockMemorySpace.HeapFragment runtimeThreadLocals = _allocator.Allocate(runtimeThreadLocalsType.Size.Value, "RuntimeThreadLocals");
            MockMemorySpace.HeapFragment thread = _allocator.Allocate(threadType.Size.Value, "Thread");
            Span<byte> data = thread.Data.AsSpan();
            helpers.Write(
                data.Slice(threadType.Fields[nameof(Data.Thread.Id)].Offset),
                id);
            helpers.WriteNUInt(
                data.Slice(threadType.Fields[nameof(Data.Thread.OSId)].Offset),
                osId);
            helpers.WritePointer(
                data.Slice(threadType.Fields[nameof(Data.Thread.ExceptionTracker)].Offset),
                exceptionInfo.Address);
            helpers.WritePointer(
                data.Slice(threadType.Fields[nameof(Data.Thread.RuntimeThreadLocals)].Offset),
                runtimeThreadLocals.Address);

            // Write alloc bytes into the GCAllocContext embedded within RuntimeThreadLocals
            int allocContextOffset = runtimeThreadLocalsType.Fields[nameof(Data.RuntimeThreadLocals.AllocContext)].Offset;
            int gcAllocationContextOffset = eeAllocContextType.Fields[nameof(Data.EEAllocContext.GCAllocationContext)].Offset;
            int allocBytesOffset = gcAllocContextType.Fields[nameof(Data.GCAllocContext.AllocBytes)].Offset;
            int allocBytesLohOffset = gcAllocContextType.Fields[nameof(Data.GCAllocContext.AllocBytesLoh)].Offset;

            Span<byte> runtimeThreadLocalsData = runtimeThreadLocals.Data.AsSpan();
            int baseOffset = allocContextOffset + gcAllocationContextOffset;
            helpers.Write(
                runtimeThreadLocalsData.Slice(baseOffset + allocBytesOffset),
                (ulong)allocBytes);
            helpers.Write(
                runtimeThreadLocalsData.Slice(baseOffset + allocBytesLohOffset),
                (ulong)allocBytesLoh);

            Builder.AddHeapFragment(thread);
            Builder.AddHeapFragment(exceptionInfo);
            Builder.AddHeapFragment(runtimeThreadLocals);

            ulong threadLinkOffset = (ulong)threadType.Fields[nameof(Data.Thread.LinkNext)].Offset;
            if (_previousThread != TargetPointer.Null)
            {
                // Set the next link for the previously added thread to the newly added one
                helpers.WritePointer(
                    Builder.BorrowAddressRange(_previousThread + threadLinkOffset, helpers.PointerSize),
                    thread.Address + threadLinkOffset);
            }
            else
            {
                // Set the first thread link in the thread store
                ulong firstThreadLinkAddr = _threadStoreAddress + (ulong)Types[DataType.ThreadStore].Fields[nameof(Data.ThreadStore.FirstThreadLink)].Offset;
                helpers.WritePointer(
                    Builder.BorrowAddressRange(firstThreadLinkAddr, helpers.PointerSize),
                    thread.Address + threadLinkOffset);
            }

            _previousThread = thread.Address;
            return thread.Address;
        }
    }
}
