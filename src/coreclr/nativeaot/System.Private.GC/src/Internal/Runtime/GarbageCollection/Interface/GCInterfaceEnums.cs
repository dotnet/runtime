// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from src/coreclr/gc/gcinterface.h and src/coreclr/gc/gcinterface.ee.h.
//
// These enums are part of the GC/EE contract. Their values are baked into the EE, into managed
// code in System.GC, and in some cases into the cDAC contracts, so they must not be renumbered.

namespace Internal.Runtime.GarbageCollection
{
    /// <summary>
    /// The reason why the GC wishes to suspend the EE, used as an argument to
    /// <c>IGCToCLR::SuspendEE</c>.
    /// </summary>
    internal enum SUSPEND_REASON
    {
        SUSPEND_FOR_GC = 1,
        SUSPEND_FOR_GC_PREP = 6,
    }

    internal enum walk_surv_type
    {
        walk_for_gc = 1,
        walk_for_bgc = 2,
        walk_for_uoh = 3,
    }

    /// <summary>
    /// Different operations that can be done by <c>GCToEEInterface::StompWriteBarrier</c>.
    /// </summary>
    internal enum WriteBarrierOp
    {
        StompResize,
        StompEphemeral,
        Initialize,
        SwitchToWriteWatch,
        SwitchToNonWriteWatch,
    }

    // NOTE: keep in sync with the definition in System.GC.
    [System.Flags]
    internal enum collection_mode
    {
        collection_non_blocking = 0x00000001,
        collection_blocking = 0x00000002,
        collection_optimized = 0x00000004,
        collection_compacting = 0x00000008,
        collection_aggressive = 0x00000010,

        // Only defined when the GC is built with STRESS_HEAP.
        collection_gcstress = unchecked((int)0x80000000),
    }

    // NOTE: keep in sync with the definition in System.GC.
    internal enum wait_full_gc_status
    {
        wait_full_gc_success = 0,
        wait_full_gc_failed = 1,
        wait_full_gc_cancelled = 2,
        wait_full_gc_timeout = 3,
        wait_full_gc_na = 4,
    }

    // NOTE: keep in sync with the definition in System.GC.
    internal enum start_no_gc_region_status
    {
        start_no_gc_success = 0,
        start_no_gc_no_memory = 1,
        start_no_gc_too_large = 2,
        start_no_gc_in_progress = 3,
    }

    internal enum end_no_gc_region_status
    {
        end_no_gc_success = 0,
        end_no_gc_not_in_progress = 1,
        end_no_gc_induced = 2,
        end_no_gc_alloc_exceeded = 3,
    }

    // NOTE: keep in sync with the definition in System.GC.
    internal enum refresh_memory_limit_status
    {
        refresh_success = 0,
        refresh_hard_limit_too_low = 1,
        refresh_hard_limit_invalid = 2,
    }

    // NOTE: keep in sync with the definition in System.GC.
    internal enum enable_no_gc_region_callback_status
    {
        succeed,
        not_started,
        insufficient_budget,
        already_registered,
    }

    internal enum gc_kind
    {
        /// <summary>Any of the following kinds.</summary>
        gc_kind_any = 0,

        /// <summary>gen0 or gen1 GC.</summary>
        gc_kind_ephemeral = 1,

        /// <summary>Blocking gen2 GC.</summary>
        gc_kind_full_blocking = 2,

        /// <summary>Background GC (always gen2).</summary>
        gc_kind_background = 3,
    }

    /// <summary>
    /// The kind of a handle. Several of these values are depended upon by cDAC contracts and by
    /// the EE; do not renumber.
    /// </summary>
    internal enum HandleType
    {
        /// <summary>
        /// Short-lived weak handles track an object until the first time it is detected to be
        /// unreachable. At that point the handle is severed, even if the object will be visible
        /// from a pending finalization graph, so short weak handles do not track across object
        /// resurrections.
        /// </summary>
        HNDTYPE_WEAK_SHORT = 0,

        /// <summary>
        /// Long-lived weak handles track an object until the object is actually reclaimed. Unlike
        /// short weak handles they continue to track their referents through finalization and
        /// across any resurrections that may occur.
        /// </summary>
        HNDTYPE_WEAK_LONG = 1,
        HNDTYPE_WEAK_DEFAULT = 1,

        /// <summary>
        /// Strong handles function like a normal object reference: their existence causes the
        /// object to remain alive through a garbage collection cycle.
        /// </summary>
        HNDTYPE_STRONG = 2,
        HNDTYPE_DEFAULT = 2,

        /// <summary>
        /// Pinned handles are strong handles that additionally prevent an object from moving
        /// during a garbage collection cycle. Pinning is expensive; use sparingly.
        /// </summary>
        HNDTYPE_PINNED = 3,

        /// <summary>
        /// Variable handles are handles whose type can be changed dynamically. Not used currently.
        /// </summary>
        HNDTYPE_VARIABLE = 4,

        /// <summary>
        /// Refcounted handles behave as strong handles while their refcount is greater than 0 and
        /// as weak handles otherwise.
        /// </summary>
        HNDTYPE_REFCOUNTED = 5,

        /// <summary>
        /// Dependent handles are two handles that need to have the same lifetime: as long as the
        /// primary object is alive, so is the secondary, but the secondary does not keep the
        /// primary alive. Used to implement <c>ConditionalWeakTable</c>.
        /// </summary>
        HNDTYPE_DEPENDENT = 6,

        /// <summary>
        /// No longer used in the VM starting with .NET 8; kept for backward compatibility.
        /// </summary>
        HNDTYPE_ASYNCPINNED = 7,

        /// <summary>
        /// No longer used in the VM starting with .NET 9; kept for backward compatibility.
        /// </summary>
        HNDTYPE_SIZEDREF = 8,

        /// <summary>
        /// No longer used in the VM starting with .NET 8; kept for backward compatibility.
        /// </summary>
        HNDTYPE_WEAK_NATIVE_COM = 9,

        /// <summary>
        /// Interior pointer handles keep an interior pointer into an object updated so that it
        /// keeps pointing at the same location within that object.
        /// </summary>
        HNDTYPE_WEAK_INTERIOR_POINTER = 10,

        /// <summary>
        /// Crossreference handles track the lifetime of an object in another VM heap.
        /// </summary>
        HNDTYPE_CROSSREFERENCE = 11,
    }

    internal enum GCHeapType
    {
        GC_HEAP_INVALID = 0,
        GC_HEAP_WKS = 1,
        GC_HEAP_SVR = 2,
    }

    /// <summary>
    /// The type passed to <c>GC.CoreCLR.cs</c>, used to deduce the type of a configuration value.
    /// </summary>
    internal enum GCConfigurationType
    {
        Int64,
        StringUtf8,
        Boolean,
    }

    // NOTE: keep in sync with GC_ALLOC_FLAGS in System.GC.
    [System.Flags]
    internal enum GC_ALLOC_FLAGS
    {
        GC_ALLOC_NO_FLAGS = 0,
        GC_ALLOC_FINALIZE = 1,
        GC_ALLOC_CONTAINS_REF = 2,
        GC_ALLOC_ALIGN8_BIAS = 4,

        /// <summary>
        /// Only implies the initial allocation is 8 byte aligned. Preserving the alignment across
        /// relocation depends on RESPECT_LARGE_ALIGNMENT also being defined.
        /// </summary>
        GC_ALLOC_ALIGN8 = 8,

        GC_ALLOC_ZEROING_OPTIONAL = 16,
        GC_ALLOC_LARGE_OBJECT_HEAP = 32,
        GC_ALLOC_PINNED_OBJECT_HEAP = 64,
        GC_ALLOC_USER_OLD_HEAP = GC_ALLOC_LARGE_OBJECT_HEAP | GC_ALLOC_PINNED_OBJECT_HEAP,
    }

    /// <summary>
    /// Flags passed to the GC scanning callbacks.
    /// </summary>
    [System.Flags]
    internal enum GCCallFlags
    {
        GC_CALL_INTERIOR = 0x1,
        GC_CALL_PINNED = 0x2,
    }

    internal enum EtwGCRootKind
    {
        kEtwGCRootKindStack = 0,
        kEtwGCRootKindFinalizer = 1,
        kEtwGCRootKindHandle = 2,
        kEtwGCRootKindOther = 3,
    }

    [System.Flags]
    internal enum EtwGCRootFlags
    {
        kEtwGCRootFlagsPinning = 0x1,
        kEtwGCRootFlagsWeakRef = 0x2,
        kEtwGCRootFlagsInterior = 0x4,
        kEtwGCRootFlagsRefCounted = 0x8,
    }
}
