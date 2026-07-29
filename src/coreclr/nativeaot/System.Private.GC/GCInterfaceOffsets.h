// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This file pins the layout of the types that are shared across the GC/EE interface, so that the
// C# port of the GC in System.Private.GC stays binary compatible with the C++ definitions in
// src/coreclr/gc/gcinterface.h and src/coreclr/gc/gcinterface.ee.h.
//
// It is consumed twice:
//
//   * GCInterfaceOffsetsVerify.cpp turns each entry into a static_assert against the real C++
//     header, so the native build fails if the C++ layout ever drifts from this table.
//   * GCInterfaceOffsets.cspp turns each entry into a C# constant, which GCInterfaceLayout
//     checks against the managed struct definitions.
//
// You must #define PLAT_GC_OFFSET, PLAT_GC_SIZEOF and PLAT_GC_CONST before you #include this file.
//

#ifdef HOST_64BIT
#define GC_OFFSET(offset32, offset64, cls, member) PLAT_GC_OFFSET(offset64, cls, member)
#define GC_SIZEOF(sizeof32, sizeof64, cls        ) PLAT_GC_SIZEOF(sizeof64, cls)
#define GC_CONST(const32, const64, expr)           PLAT_GC_CONST(const64, expr)
#else
#define GC_OFFSET(offset32, offset64, cls, member) PLAT_GC_OFFSET(offset32, cls, member)
#define GC_SIZEOF(sizeof32, sizeof64, cls        ) PLAT_GC_SIZEOF(sizeof32, cls)
#define GC_CONST(const32, const64, expr)           PLAT_GC_CONST(const32, expr)
#endif

// NOTE: the values MUST be in hex notation WITHOUT the 0x prefix.

//        32-bit,64-bit, class, member
GC_OFFSET(     0,     0, gc_alloc_context, alloc_ptr)
GC_OFFSET(     4,     8, gc_alloc_context, alloc_limit)
GC_OFFSET(     8,    10, gc_alloc_context, alloc_bytes)
GC_OFFSET(    10,    18, gc_alloc_context, alloc_bytes_uoh)
GC_OFFSET(    18,    20, gc_alloc_context, gc_reserved_1)
GC_OFFSET(    1c,    28, gc_alloc_context, gc_reserved_2)
GC_OFFSET(    20,    30, gc_alloc_context, alloc_count)
GC_SIZEOF(    28,    38, gc_alloc_context)

GC_OFFSET(     0,     0, segment_info, pvMem)
GC_OFFSET(     4,     8, segment_info, ibFirstObject)
GC_OFFSET(     8,    10, segment_info, ibAllocated)
GC_OFFSET(     c,    18, segment_info, ibCommit)
GC_OFFSET(    10,    20, segment_info, ibReserved)
GC_SIZEOF(    14,    28, segment_info)

GC_OFFSET(     0,     0, WriteBarrierParameters, operation)
GC_OFFSET(     4,     4, WriteBarrierParameters, is_runtime_suspended)
GC_OFFSET(     5,     5, WriteBarrierParameters, requires_upper_bounds_check)
GC_OFFSET(     8,     8, WriteBarrierParameters, card_table)
GC_OFFSET(     c,    10, WriteBarrierParameters, card_bundle_table)
GC_OFFSET(    10,    18, WriteBarrierParameters, lowest_address)
GC_OFFSET(    14,    20, WriteBarrierParameters, highest_address)
GC_OFFSET(    18,    28, WriteBarrierParameters, ephemeral_low)
GC_OFFSET(    1c,    30, WriteBarrierParameters, ephemeral_high)
GC_OFFSET(    20,    38, WriteBarrierParameters, write_watch_table)
GC_OFFSET(    24,    40, WriteBarrierParameters, region_to_generation_table)
GC_OFFSET(    28,    48, WriteBarrierParameters, region_shr)
GC_OFFSET(    29,    49, WriteBarrierParameters, region_use_bitwise_write_barrier)
GC_SIZEOF(    2c,    50, WriteBarrierParameters)

GC_OFFSET(     0,     0, FinalizerWorkItem, next)
GC_OFFSET(     4,     8, FinalizerWorkItem, callback)
GC_SIZEOF(     8,    10, FinalizerWorkItem)

GC_OFFSET(     8,    10, NoGCRegionCallbackFinalizerWorkItem, scheduled)
GC_OFFSET(     9,    11, NoGCRegionCallbackFinalizerWorkItem, abandoned)
GC_SIZEOF(     c,    18, NoGCRegionCallbackFinalizerWorkItem)

GC_OFFSET(     0,     0, EtwGCSettingsInfo, heap_hard_limit)
GC_OFFSET(     4,     8, EtwGCSettingsInfo, loh_threshold)
GC_OFFSET(     8,    10, EtwGCSettingsInfo, physical_memory_from_config)
GC_OFFSET(     c,    18, EtwGCSettingsInfo, gen0_min_budget_from_config)
GC_OFFSET(    10,    20, EtwGCSettingsInfo, gen0_max_budget_from_config)
GC_OFFSET(    14,    28, EtwGCSettingsInfo, high_mem_percent_from_config)
GC_OFFSET(    18,    2c, EtwGCSettingsInfo, concurrent_gc_p)
GC_OFFSET(    19,    2d, EtwGCSettingsInfo, use_large_pages_p)
GC_OFFSET(    1a,    2e, EtwGCSettingsInfo, use_frozen_segments_p)
GC_OFFSET(    1b,    2f, EtwGCSettingsInfo, hard_limit_config_p)
GC_OFFSET(    1c,    30, EtwGCSettingsInfo, no_affinitize_p)
GC_SIZEOF(    20,    38, EtwGCSettingsInfo)

GC_OFFSET(     0,     0, StronglyConnectedComponent, Count)
GC_OFFSET(     4,     8, StronglyConnectedComponent, Contexts)
GC_SIZEOF(     8,    10, StronglyConnectedComponent)

GC_OFFSET(     0,     0, ComponentCrossReference, SourceGroupIndex)
GC_OFFSET(     4,     8, ComponentCrossReference, DestinationGroupIndex)
GC_SIZEOF(     8,    10, ComponentCrossReference)

GC_OFFSET(     0,     0, MarkCrossReferencesArgs, ComponentCount)
GC_OFFSET(     4,     8, MarkCrossReferencesArgs, Components)
GC_OFFSET(     8,    10, MarkCrossReferencesArgs, CrossReferenceCount)
GC_OFFSET(     c,    18, MarkCrossReferencesArgs, CrossReferences)
GC_SIZEOF(    10,    20, MarkCrossReferencesArgs)

GC_OFFSET(     0,     0, ScanContext, thread_under_crawl)
GC_OFFSET(     4,     8, ScanContext, thread_number)
GC_OFFSET(     8,     c, ScanContext, thread_count)
GC_OFFSET(     c,    10, ScanContext, stack_limit)
GC_OFFSET(    10,    18, ScanContext, promotion)
GC_OFFSET(    11,    19, ScanContext, concurrent)
GC_OFFSET(    14,    20, ScanContext, _unused1)
GC_OFFSET(    18,    28, ScanContext, pMD)
GC_SIZEOF(    20,    38, ScanContext)

GC_OFFSET(     0,     0, VersionInfo, MajorVersion)
GC_OFFSET(     4,     4, VersionInfo, MinorVersion)
GC_OFFSET(     8,     8, VersionInfo, BuildVersion)
GC_OFFSET(     c,    10, VersionInfo, Name)
GC_SIZEOF(    10,    18, VersionInfo)

//        32-bit,64-bit, constant symbol
GC_CONST(     5,     5, GC_INTERFACE_MAJOR_VERSION)
GC_CONST(     8,     8, GC_INTERFACE_MINOR_VERSION)
GC_CONST(     4,     4, EE_INTERFACE_MAJOR_VERSION)
GC_CONST( 14C08, 14C08, LARGE_OBJECT_SIZE)
GC_CONST(     c,    18, min_obj_size)
GC_CONST(     c,     c, SOFTWARE_WRITE_WATCH_AddressToTableByteIndexShift)
