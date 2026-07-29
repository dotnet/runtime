// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Checks that the managed definitions of the GC/EE interface types agree with the layout table in
// GCInterfaceOffsets.h. The other half of that table is turned into static_asserts against the
// real C++ headers by nativeaot/Runtime/GCInterfaceOffsetsVerify.cpp, so agreement here means the
// managed structs are laid out exactly like their native counterparts.
//
// The check is a runtime one because C# has no compile-time offsetof, but it is cheap, has no
// dependencies, and is expected to be called once during GC startup in checked builds.

using System.Runtime.CompilerServices;

namespace Internal.Runtime.GarbageCollection
{
    internal static unsafe class GCInterfaceLayout
    {
        /// <summary>
        /// Returns true if every managed GC/EE interface type matches the pinned native layout.
        /// </summary>
        public static bool Verify()
        {
            gc_alloc_context allocContext;
            if (sizeof(gc_alloc_context) != GCInterfaceOffsets.SIZEOF__gc_alloc_context
                || OffsetOf(&allocContext, &allocContext.alloc_ptr) != GCInterfaceOffsets.OFFSETOF__gc_alloc_context__alloc_ptr
                || OffsetOf(&allocContext, &allocContext.alloc_limit) != GCInterfaceOffsets.OFFSETOF__gc_alloc_context__alloc_limit
                || OffsetOf(&allocContext, &allocContext.alloc_bytes) != GCInterfaceOffsets.OFFSETOF__gc_alloc_context__alloc_bytes
                || OffsetOf(&allocContext, &allocContext.alloc_bytes_uoh) != GCInterfaceOffsets.OFFSETOF__gc_alloc_context__alloc_bytes_uoh
                || OffsetOf(&allocContext, &allocContext.gc_reserved_1) != GCInterfaceOffsets.OFFSETOF__gc_alloc_context__gc_reserved_1
                || OffsetOf(&allocContext, &allocContext.gc_reserved_2) != GCInterfaceOffsets.OFFSETOF__gc_alloc_context__gc_reserved_2
                || OffsetOf(&allocContext, &allocContext.alloc_count) != GCInterfaceOffsets.OFFSETOF__gc_alloc_context__alloc_count)
            {
                return false;
            }

            segment_info segmentInfo;
            if (sizeof(segment_info) != GCInterfaceOffsets.SIZEOF__segment_info
                || OffsetOf(&segmentInfo, &segmentInfo.pvMem) != GCInterfaceOffsets.OFFSETOF__segment_info__pvMem
                || OffsetOf(&segmentInfo, &segmentInfo.ibFirstObject) != GCInterfaceOffsets.OFFSETOF__segment_info__ibFirstObject
                || OffsetOf(&segmentInfo, &segmentInfo.ibAllocated) != GCInterfaceOffsets.OFFSETOF__segment_info__ibAllocated
                || OffsetOf(&segmentInfo, &segmentInfo.ibCommit) != GCInterfaceOffsets.OFFSETOF__segment_info__ibCommit
                || OffsetOf(&segmentInfo, &segmentInfo.ibReserved) != GCInterfaceOffsets.OFFSETOF__segment_info__ibReserved)
            {
                return false;
            }

            WriteBarrierParameters args;
            if (sizeof(WriteBarrierParameters) != GCInterfaceOffsets.SIZEOF__WriteBarrierParameters
                || OffsetOf(&args, &args.operation) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__operation
                || OffsetOf(&args, &args.is_runtime_suspended) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__is_runtime_suspended
                || OffsetOf(&args, &args.requires_upper_bounds_check) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__requires_upper_bounds_check
                || OffsetOf(&args, &args.card_table) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__card_table
                || OffsetOf(&args, &args.card_bundle_table) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__card_bundle_table
                || OffsetOf(&args, &args.lowest_address) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__lowest_address
                || OffsetOf(&args, &args.highest_address) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__highest_address
                || OffsetOf(&args, &args.ephemeral_low) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__ephemeral_low
                || OffsetOf(&args, &args.ephemeral_high) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__ephemeral_high
                || OffsetOf(&args, &args.write_watch_table) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__write_watch_table
                || OffsetOf(&args, &args.region_to_generation_table) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__region_to_generation_table
                || OffsetOf(&args, &args.region_shr) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__region_shr
                || OffsetOf(&args, &args.region_use_bitwise_write_barrier) != GCInterfaceOffsets.OFFSETOF__WriteBarrierParameters__region_use_bitwise_write_barrier)
            {
                return false;
            }

            FinalizerWorkItem workItem;
            if (sizeof(FinalizerWorkItem) != GCInterfaceOffsets.SIZEOF__FinalizerWorkItem
                || OffsetOf(&workItem, &workItem.next) != GCInterfaceOffsets.OFFSETOF__FinalizerWorkItem__next
                || OffsetOf(&workItem, &workItem.callback) != GCInterfaceOffsets.OFFSETOF__FinalizerWorkItem__callback)
            {
                return false;
            }

            NoGCRegionCallbackFinalizerWorkItem noGCWorkItem;
            if (sizeof(NoGCRegionCallbackFinalizerWorkItem) != GCInterfaceOffsets.SIZEOF__NoGCRegionCallbackFinalizerWorkItem
                || OffsetOf(&noGCWorkItem, &noGCWorkItem.scheduled) != GCInterfaceOffsets.OFFSETOF__NoGCRegionCallbackFinalizerWorkItem__scheduled
                || OffsetOf(&noGCWorkItem, &noGCWorkItem.abandoned) != GCInterfaceOffsets.OFFSETOF__NoGCRegionCallbackFinalizerWorkItem__abandoned)
            {
                return false;
            }

            EtwGCSettingsInfo settings;
            if (sizeof(EtwGCSettingsInfo) != GCInterfaceOffsets.SIZEOF__EtwGCSettingsInfo
                || OffsetOf(&settings, &settings.heap_hard_limit) != GCInterfaceOffsets.OFFSETOF__EtwGCSettingsInfo__heap_hard_limit
                || OffsetOf(&settings, &settings.loh_threshold) != GCInterfaceOffsets.OFFSETOF__EtwGCSettingsInfo__loh_threshold
                || OffsetOf(&settings, &settings.physical_memory_from_config) != GCInterfaceOffsets.OFFSETOF__EtwGCSettingsInfo__physical_memory_from_config
                || OffsetOf(&settings, &settings.gen0_min_budget_from_config) != GCInterfaceOffsets.OFFSETOF__EtwGCSettingsInfo__gen0_min_budget_from_config
                || OffsetOf(&settings, &settings.gen0_max_budget_from_config) != GCInterfaceOffsets.OFFSETOF__EtwGCSettingsInfo__gen0_max_budget_from_config
                || OffsetOf(&settings, &settings.high_mem_percent_from_config) != GCInterfaceOffsets.OFFSETOF__EtwGCSettingsInfo__high_mem_percent_from_config
                || OffsetOf(&settings, &settings.concurrent_gc_p) != GCInterfaceOffsets.OFFSETOF__EtwGCSettingsInfo__concurrent_gc_p
                || OffsetOf(&settings, &settings.use_large_pages_p) != GCInterfaceOffsets.OFFSETOF__EtwGCSettingsInfo__use_large_pages_p
                || OffsetOf(&settings, &settings.use_frozen_segments_p) != GCInterfaceOffsets.OFFSETOF__EtwGCSettingsInfo__use_frozen_segments_p
                || OffsetOf(&settings, &settings.hard_limit_config_p) != GCInterfaceOffsets.OFFSETOF__EtwGCSettingsInfo__hard_limit_config_p
                || OffsetOf(&settings, &settings.no_affinitize_p) != GCInterfaceOffsets.OFFSETOF__EtwGCSettingsInfo__no_affinitize_p)
            {
                return false;
            }

            StronglyConnectedComponent component;
            if (sizeof(StronglyConnectedComponent) != GCInterfaceOffsets.SIZEOF__StronglyConnectedComponent
                || OffsetOf(&component, &component.Count) != GCInterfaceOffsets.OFFSETOF__StronglyConnectedComponent__Count
                || OffsetOf(&component, &component.Contexts) != GCInterfaceOffsets.OFFSETOF__StronglyConnectedComponent__Contexts)
            {
                return false;
            }

            ComponentCrossReference crossReference;
            if (sizeof(ComponentCrossReference) != GCInterfaceOffsets.SIZEOF__ComponentCrossReference
                || OffsetOf(&crossReference, &crossReference.SourceGroupIndex) != GCInterfaceOffsets.OFFSETOF__ComponentCrossReference__SourceGroupIndex
                || OffsetOf(&crossReference, &crossReference.DestinationGroupIndex) != GCInterfaceOffsets.OFFSETOF__ComponentCrossReference__DestinationGroupIndex)
            {
                return false;
            }

            MarkCrossReferencesArgs crossReferences;
            if (sizeof(MarkCrossReferencesArgs) != GCInterfaceOffsets.SIZEOF__MarkCrossReferencesArgs
                || OffsetOf(&crossReferences, &crossReferences.ComponentCount) != GCInterfaceOffsets.OFFSETOF__MarkCrossReferencesArgs__ComponentCount
                || OffsetOf(&crossReferences, &crossReferences.Components) != GCInterfaceOffsets.OFFSETOF__MarkCrossReferencesArgs__Components
                || OffsetOf(&crossReferences, &crossReferences.CrossReferenceCount) != GCInterfaceOffsets.OFFSETOF__MarkCrossReferencesArgs__CrossReferenceCount
                || OffsetOf(&crossReferences, &crossReferences.CrossReferences) != GCInterfaceOffsets.OFFSETOF__MarkCrossReferencesArgs__CrossReferences)
            {
                return false;
            }

            ScanContext scanContext;
            if (sizeof(ScanContext) != GCInterfaceOffsets.SIZEOF__ScanContext
                || OffsetOf(&scanContext, &scanContext.thread_under_crawl) != GCInterfaceOffsets.OFFSETOF__ScanContext__thread_under_crawl
                || OffsetOf(&scanContext, &scanContext.thread_number) != GCInterfaceOffsets.OFFSETOF__ScanContext__thread_number
                || OffsetOf(&scanContext, &scanContext.thread_count) != GCInterfaceOffsets.OFFSETOF__ScanContext__thread_count
                || OffsetOf(&scanContext, &scanContext.stack_limit) != GCInterfaceOffsets.OFFSETOF__ScanContext__stack_limit
                || OffsetOf(&scanContext, &scanContext.promotion) != GCInterfaceOffsets.OFFSETOF__ScanContext__promotion
                || OffsetOf(&scanContext, &scanContext.concurrent) != GCInterfaceOffsets.OFFSETOF__ScanContext__concurrent
                || OffsetOf(&scanContext, &scanContext.pMD) != GCInterfaceOffsets.OFFSETOF__ScanContext__pMD)
            {
                return false;
            }

            VersionInfo versionInfo;
            if (sizeof(VersionInfo) != GCInterfaceOffsets.SIZEOF__VersionInfo
                || OffsetOf(&versionInfo, &versionInfo.MajorVersion) != GCInterfaceOffsets.OFFSETOF__VersionInfo__MajorVersion
                || OffsetOf(&versionInfo, &versionInfo.MinorVersion) != GCInterfaceOffsets.OFFSETOF__VersionInfo__MinorVersion
                || OffsetOf(&versionInfo, &versionInfo.BuildVersion) != GCInterfaceOffsets.OFFSETOF__VersionInfo__BuildVersion
                || OffsetOf(&versionInfo, &versionInfo.Name) != GCInterfaceOffsets.OFFSETOF__VersionInfo__Name)
            {
                return false;
            }

            // The vtable structs have one pointer-sized field per virtual slot, so their size is
            // the only thing that can be checked mechanically; the order of the slots has to match
            // the declaration order in the C++ header, which is enforced by review.
            if (sizeof(IGCHandleStoreVtable) != IGCHandleStoreVtable.SlotCount * sizeof(void*)
                || sizeof(IGCHandleManagerVtable) != IGCHandleManagerVtable.SlotCount * sizeof(void*)
                || sizeof(IGCHeapVtable) != IGCHeapVtable.SlotCount * sizeof(void*)
                || sizeof(IGCToCLRVtable) != IGCToCLRVtable.SlotCount * sizeof(void*)
                || sizeof(IGCToCLREventSinkVtable) != IGCToCLREventSinkVtable.SlotCount * sizeof(void*))
            {
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int OffsetOf(void* structure, void* field) => (int)((byte*)field - (byte*)structure);
    }
}
