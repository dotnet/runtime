// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from src/coreclr/gc/gcinterface.h and src/coreclr/gc/gcinterface.ee.h.
//
// Every type in this file is shared by value or by pointer with the native EE, so its layout is
// part of the GC/EE contract. The layouts are pinned by GCInterfaceOffsets.h, which is asserted
// against the C++ headers by the native build and against these types by GCInterfaceLayout.
//
// Per the porting rules for this library, object references are represented as raw byte pointers:
// the GC must never hold a tracked reference to the heap it is collecting.

using System.Runtime.InteropServices;

namespace Internal.Runtime.GarbageCollection
{
    /// <summary>
    /// An opaque handle to an object, as handed out by <c>IGCHandleStore</c>. Corresponds to the
    /// native <c>OBJECTHANDLE</c>.
    /// </summary>
    internal readonly unsafe struct OBJECTHANDLE
    {
        private readonly void* _value;

        public OBJECTHANDLE(void* value) => _value = value;

        public void* Value => _value;

        public bool IsNull => _value == null;
    }

    /// <summary>
    /// An opaque handle to a heap segment. Corresponds to the native <c>segment_handle</c>.
    /// </summary>
    internal readonly unsafe struct segment_handle
    {
        private readonly void* _value;

        public segment_handle(void* value) => _value = value;

        public void* Value => _value;
    }

    /// <summary>
    /// The allocation context must be known to the VM for use in the allocation fast path and
    /// known to the GC for performing the allocation. Every thread has its own allocation context
    /// that it hands to the GC when allocating.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct gc_alloc_context
    {
        public byte* alloc_ptr;
        public byte* alloc_limit;

        /// <summary>Number of bytes allocated on SOH by this context.</summary>
        public long alloc_bytes;

        /// <summary>Number of bytes allocated not on SOH by this context.</summary>
        public long alloc_bytes_uoh;

        // These two fields are deliberately not exposed past the EE-GC interface.
        public void* gc_reserved_1;
        public void* gc_reserved_2;

        public int alloc_count;

        public void init()
        {
            alloc_ptr = null;
            alloc_limit = null;
            alloc_bytes = 0;
            alloc_bytes_uoh = 0;
            gc_reserved_1 = null;
            gc_reserved_2 = null;
            alloc_count = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct segment_info
    {
        /// <summary>Base of the allocation, not the first object (must add ibFirstObject).</summary>
        public void* pvMem;

        /// <summary>Offset to the base of the first object in the segment.</summary>
        public nuint ibFirstObject;

        /// <summary>Limit of allocated memory in the segment (&gt;= ibFirstObject).</summary>
        public nuint ibAllocated;

        /// <summary>Limit of committed memory in the segment (&gt;= ibAllocated).</summary>
        public nuint ibCommit;

        /// <summary>Limit of reserved memory in the segment (&gt;= ibCommit).</summary>
        public nuint ibReserved;
    }

    /// <summary>
    /// Arguments to <c>GCToEEInterface::StompWriteBarrier</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct WriteBarrierParameters
    {
        /// <summary>The operation that StompWriteBarrier will perform.</summary>
        public WriteBarrierOp operation;

        /// <summary>
        /// Whether or not the runtime is currently suspended. If it is not, the EE will need to
        /// suspend it before bashing the write barrier. Used for all operations.
        /// </summary>
        public byte is_runtime_suspended;

        /// <summary>
        /// Whether or not the GC has moved the ephemeral generation to no longer be at the top of
        /// the heap. When the ephemeral generation is at the top of the heap, and the write
        /// barrier observes that a pointer is greater than g_ephemeral_low, it does not need to
        /// check that the pointer is less than g_ephemeral_high because there is nothing in the GC
        /// heap above the ephemeral generation. When this is not the case, the GC must inform the
        /// EE so that it can switch to a write barrier that checks both bounds.
        /// Used for <see cref="WriteBarrierOp.StompResize"/>.
        /// </summary>
        public byte requires_upper_bounds_check;

        /// <summary>
        /// The new card table location. May or may not be the same as the previous card table.
        /// Used for <see cref="WriteBarrierOp.Initialize"/> and <see cref="WriteBarrierOp.StompResize"/>.
        /// </summary>
        public uint* card_table;

        /// <summary>
        /// The new card bundle table location. May or may not be the same as the previous card
        /// bundle table. Used for <see cref="WriteBarrierOp.Initialize"/> and
        /// <see cref="WriteBarrierOp.StompResize"/>.
        /// </summary>
        public uint* card_bundle_table;

        /// <summary>The heap's new low boundary.</summary>
        public byte* lowest_address;

        /// <summary>The heap's new high boundary.</summary>
        public byte* highest_address;

        /// <summary>
        /// The new start of the ephemeral generation. Used for
        /// <see cref="WriteBarrierOp.StompEphemeral"/>.
        /// </summary>
        public byte* ephemeral_low;

        /// <summary>
        /// The new end of the ephemeral generation. Used for
        /// <see cref="WriteBarrierOp.StompEphemeral"/>.
        /// </summary>
        public byte* ephemeral_high;

        /// <summary>
        /// The new write watch table, if we are using our own write watch implementation. Used for
        /// <see cref="WriteBarrierOp.SwitchToWriteWatch"/> only.
        /// </summary>
        public byte* write_watch_table;

        /// <summary>Mapping table from region index to generation.</summary>
        public byte* region_to_generation_table;

        /// <summary>How many bits to shift right to obtain the region index from an address.</summary>
        public byte region_shr;

        /// <summary>Whether to use the more precise but slower write barrier.</summary>
        public byte region_use_bitwise_write_barrier;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct FinalizerWorkItem
    {
        public FinalizerWorkItem* next;
        public delegate* unmanaged<FinalizerWorkItem*, void> callback;
    }

    /// <summary>
    /// Derives from <see cref="FinalizerWorkItem"/> in the native header; the base fields are
    /// spelled out here because C# has no struct inheritance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NoGCRegionCallbackFinalizerWorkItem
    {
        public FinalizerWorkItem* next;
        public delegate* unmanaged<FinalizerWorkItem*, void> callback;
        public byte scheduled;
        public byte abandoned;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EtwGCSettingsInfo
    {
        public nuint heap_hard_limit;
        public nuint loh_threshold;
        public nuint physical_memory_from_config;
        public nuint gen0_min_budget_from_config;
        public nuint gen0_max_budget_from_config;
        public uint high_mem_percent_from_config;
        public byte concurrent_gc_p;
        public byte use_large_pages_p;
        public byte use_frozen_segments_p;

        /// <summary>If this is false, the hard limit was set implicitly by the container.</summary>
        public byte hard_limit_config_p;

        public byte no_affinitize_p;
    }

    // These definitions are also in managed code (see the cross-reference bridge support).
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct StronglyConnectedComponent
    {
        public nuint Count;
        public nuint* Contexts;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ComponentCrossReference
    {
        public nuint SourceGroupIndex;
        public nuint DestinationGroupIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct MarkCrossReferencesArgs
    {
        public nuint ComponentCount;
        public StronglyConnectedComponent* Components;
        public nuint CrossReferenceCount;
        public ComponentCrossReference* CrossReferences;
    }

    /// <summary>
    /// The context handed to the EE's root-scanning callbacks.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ScanContext
    {
        public void* thread_under_crawl;
        public int thread_number;
        public int thread_count;

        /// <summary>
        /// Lowest point on the thread stack that the scanning logic is permitted to read.
        /// </summary>
        public nuint stack_limit;

        /// <summary>TRUE: promotion, FALSE: relocation.</summary>
        public byte promotion;

        /// <summary>TRUE: concurrent scanning.</summary>
        public byte concurrent;

        private void* _unused1;

        public void* pMD;

        /// <summary>
        /// Only meaningful when the runtime is built with GC_PROFILING or FEATURE_EVENT_TRACE;
        /// otherwise this slot is unused but still present.
        /// </summary>
        public EtwGCRootKind dwEtwRootKind;

        public void init()
        {
            thread_under_crawl = null;
            thread_number = -1;
            thread_count = -1;
            stack_limit = 0;
            promotion = 0;
            concurrent = 0;
            _unused1 = null;
            pMD = null;
            dwEtwRootKind = EtwGCRootKind.kEtwGCRootKindOther;
        }
    }

    /// <summary>
    /// Part of the loader protocol between the EE and the GC.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct VersionInfo
    {
        public uint MajorVersion;
        public uint MinorVersion;
        public uint BuildVersion;
        public byte* Name;
    }
}
