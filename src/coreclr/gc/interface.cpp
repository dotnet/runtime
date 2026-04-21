// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

class NoGCRegionLockHolder
{
public:
    NoGCRegionLockHolder()
    {
        enter_spin_lock_noinstru(&g_no_gc_lock);
    }

    ~NoGCRegionLockHolder()
    {
        leave_spin_lock_noinstru(&g_no_gc_lock);
    }
};
void GCHeap::Shutdown()
{
    // This does not work for standalone GC on Windows because windows closed the file
    // handle in DllMain for the standalone GC before we get here.
#if defined(TRACE_GC) && defined(SIMPLE_DPRINTF) && !defined(BUILD_AS_STANDALONE)
    flush_gc_log (true);
#endif //TRACE_GC && SIMPLE_DPRINTF && !BUILD_AS_STANDALONE
}

void
init_sync_log_stats()
{
#ifdef SYNCHRONIZATION_STATS
    if (gc_count_during_log == 0)
    {
        gc_heap::init_sync_stats();
        suspend_ee_during_log = 0;
        restart_ee_during_log = 0;
        gc_during_log = 0;
        gc_lock_contended = 0;

        log_start_tick = GCToOSInterface::GetLowPrecisionTimeStamp();
        log_start_hires = GCToOSInterface::QueryPerformanceCounter();
    }
    gc_count_during_log++;
#endif //SYNCHRONIZATION_STATS
}

void GCHeap::ValidateObjectMember (Object* obj)
{
#ifdef VERIFY_HEAP
    size_t s = size (obj);
    uint8_t* o = (uint8_t*)obj;

    go_through_object_cl (method_table (obj), o, s, oo,
        {
            uint8_t* child_o = *oo;
            if (child_o)
            {
                //dprintf (3, ("VOM: m: %zx obj %zx", (size_t)child_o, o));
                MethodTable *pMT = method_table (child_o);
                assert(pMT);
                if (!pMT->SanityCheck()) {
                    dprintf (1, ("Bad member of %zx %zx",
                                (size_t)oo, (size_t)child_o));
                    FATAL_GC_ERROR();
                }
            }
        } );
#endif // VERIFY_HEAP
}

HRESULT GCHeap::StaticShutdown()
{
    deleteGCShadow();

    GCScan::GcRuntimeStructuresValid (FALSE);

    // Cannot assert this, since we use SuspendEE as the mechanism to quiesce all
    // threads except the one performing the shutdown.
    // ASSERT( !GcInProgress );

    // Guard against any more GC occurring and against any threads blocking
    // for GC to complete when the GC heap is gone.  This fixes a race condition
    // where a thread in GC is destroyed as part of process destruction and
    // the remaining threads block for GC complete.

    //GCTODO
    //EnterAllocLock();
    //Enter();
    //EnterFinalizeLock();
    //SetGCDone();

    // during shutdown lot of threads are suspended
    // on this even, we don't want to wake them up just yet
    //CloseHandle (WaitForGCEvent);

    //find out if the global card table hasn't been used yet
    uint32_t* ct = &g_gc_card_table[card_word (gcard_of (g_gc_lowest_address))];
    if (card_table_refcount (ct) == 0)
    {
        destroy_card_table (ct);
        g_gc_card_table = nullptr;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        g_gc_card_bundle_table = nullptr;
#endif
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        SoftwareWriteWatch::StaticClose();
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    }

#ifndef USE_REGIONS
    //destroy all segments on the standby list
    while(gc_heap::segment_standby_list != 0)
    {
        heap_segment* next_seg = heap_segment_next (gc_heap::segment_standby_list);
#ifdef MULTIPLE_HEAPS
        (gc_heap::g_heaps[0])->delete_heap_segment (gc_heap::segment_standby_list, FALSE);
#else //MULTIPLE_HEAPS
        pGenGCHeap->delete_heap_segment (gc_heap::segment_standby_list, FALSE);
#endif //MULTIPLE_HEAPS
        gc_heap::segment_standby_list = next_seg;
    }
#endif // USE_REGIONS

#ifdef MULTIPLE_HEAPS

    for (int i = 0; i < gc_heap::n_heaps; i ++)
    {
        //destroy pure GC stuff
        gc_heap::destroy_gc_heap (gc_heap::g_heaps[i]);
    }
#else
    gc_heap::destroy_gc_heap (pGenGCHeap);

#endif //MULTIPLE_HEAPS
    gc_heap::shutdown_gc();

    return S_OK;
}

// init the instance heap
HRESULT GCHeap::Init(size_t hn)
{
    HRESULT hres = S_OK;

#ifdef MULTIPLE_HEAPS
    if ((pGenGCHeap = gc_heap::make_gc_heap(this, (int)hn)) == 0)
        hres = E_OUTOFMEMORY;
#else
    UNREFERENCED_PARAMETER(hn);
    if (!gc_heap::make_gc_heap())
        hres = E_OUTOFMEMORY;
#endif //MULTIPLE_HEAPS

    // Failed.
    return hres;
}

//System wide initialization
HRESULT GCHeap::Initialize()
{
#ifndef TRACE_GC
    STRESS_LOG_VA (1, (ThreadStressLog::gcLoggingIsOffMsg()));
#endif
    HRESULT hr = S_OK;

    qpf = (uint64_t)GCToOSInterface::QueryPerformanceFrequency();
    qpf_ms = 1000.0 / (double)qpf;
    qpf_us = 1000.0 * 1000.0 / (double)qpf;

    g_gc_pFreeObjectMethodTable = GCToEEInterface::GetFreeObjectMethodTable();
    g_num_processors = GCToOSInterface::GetTotalProcessorCount();
    assert(g_num_processors != 0);

    gc_heap::total_physical_mem = (size_t)GCConfig::GetGCTotalPhysicalMemory();
    if (gc_heap::total_physical_mem != 0)
    {
        gc_heap::is_restricted_physical_mem = true;
#ifdef FEATURE_EVENT_TRACE
        gc_heap::physical_memory_from_config = (size_t)gc_heap::total_physical_mem;
#endif //FEATURE_EVENT_TRACE
    }
    else
    {
        gc_heap::total_physical_mem = GCToOSInterface::GetPhysicalMemoryLimit (&gc_heap::is_restricted_physical_mem);
    }
    memset (gc_heap::committed_by_oh, 0, sizeof (gc_heap::committed_by_oh));
    if (!gc_heap::compute_hard_limit())
    {
        log_init_error_to_host ("compute_hard_limit failed, check your heap hard limit related configs");
        return CLR_E_GC_BAD_HARD_LIMIT;
    }

    uint32_t nhp = 1;
    uint32_t nhp_from_config = 0;
    uint32_t max_nhp_from_config = (uint32_t)GCConfig::GetMaxHeapCount();

#ifndef MULTIPLE_HEAPS
    GCConfig::SetServerGC(false);
#else //!MULTIPLE_HEAPS
    GCConfig::SetServerGC(true);
    AffinitySet config_affinity_set;
    if (!config_affinity_set.Initialize(GCToOSInterface::GetMaxProcessorCount()))
    {
        log_init_error_to_host ("Failed to initialize affinity set for GC heap affinity configuration");
        return E_OUTOFMEMORY;
    }

    GCConfigStringHolder cpu_index_ranges_holder(GCConfig::GetGCHeapAffinitizeRanges());

    uintptr_t config_affinity_mask = static_cast<uintptr_t>(GCConfig::GetGCHeapAffinitizeMask());
    if (!ParseGCHeapAffinitizeRanges(cpu_index_ranges_holder.Get(), &config_affinity_set, config_affinity_mask))
    {
        log_init_error_to_host ("ParseGCHeapAffinitizeRange failed, check your HeapAffinitizeRanges config");
        return CLR_E_GC_BAD_AFFINITY_CONFIG_FORMAT;
    }

    const AffinitySet* process_affinity_set = GCToOSInterface::SetGCThreadsAffinitySet(config_affinity_mask, &config_affinity_set);
    GCConfig::SetGCHeapAffinitizeMask(static_cast<int64_t>(config_affinity_mask));

    if (process_affinity_set->IsEmpty())
    {
        log_init_error_to_host ("This process is affinitize to 0 CPUs, check your GC heap affinity related configs");
        return CLR_E_GC_BAD_AFFINITY_CONFIG;
    }

    if ((cpu_index_ranges_holder.Get() != nullptr)
#ifdef TARGET_WINDOWS
        || (config_affinity_mask != 0)
#endif
    )
    {
        affinity_config_specified_p = true;
    }

    nhp_from_config = static_cast<uint32_t>(GCConfig::GetHeapCount());

    // The CPU count may be overridden by the user. Ensure that we create no more than g_num_processors
    // heaps as that is the number of slots we have allocated for handle tables.
    g_num_active_processors = min (GCToEEInterface::GetCurrentProcessCpuCount(), g_num_processors);

    if (nhp_from_config)
    {
        // Even when the user specifies a heap count, it should not be more
        // than the number of procs this process can use.
        nhp_from_config = min (nhp_from_config, g_num_active_processors);
    }

    nhp = ((nhp_from_config == 0) ? g_num_active_processors : nhp_from_config);

    nhp = min (nhp, (uint32_t)MAX_SUPPORTED_HEAPS);

    gc_heap::gc_thread_no_affinitize_p = (gc_heap::heap_hard_limit ?
        !affinity_config_specified_p : (GCConfig::GetNoAffinitize() != 0));

    if (!(gc_heap::gc_thread_no_affinitize_p))
    {
        uint32_t num_affinitized_processors = (uint32_t)process_affinity_set->Count();

        if (num_affinitized_processors != 0)
        {
            nhp = min(nhp, num_affinitized_processors);
        }
    }
#endif //!MULTIPLE_HEAPS

    if (gc_heap::heap_hard_limit)
    {
        gc_heap::hard_limit_config_p = true;
    }

    size_t seg_size_from_config = 0;
    bool compute_memory_settings_succeed = gc_heap::compute_memory_settings(true, nhp, nhp_from_config, seg_size_from_config, 0);
    assert (compute_memory_settings_succeed);

    if ((!gc_heap::heap_hard_limit) && gc_heap::use_large_pages_p)
    {
        return CLR_E_GC_LARGE_PAGE_MISSING_HARD_LIMIT;
    }
    GCConfig::SetGCLargePages(gc_heap::use_large_pages_p);

#ifdef USE_REGIONS
    gc_heap::regions_range = (size_t)GCConfig::GetGCRegionRange();
    if (gc_heap::regions_range == 0)
    {
        if (gc_heap::heap_hard_limit)
        {
#ifndef HOST_64BIT
            // Regions are not supported on 32bit
            assert(false);
#endif //!HOST_64BIT

            if (gc_heap::heap_hard_limit_oh[soh])
            {
                gc_heap::regions_range = gc_heap::heap_hard_limit;
            }
            else
            {
                // We use this calculation because it's close to what we used for segments.
                gc_heap::regions_range = ((gc_heap::use_large_pages_p) ? (2 * gc_heap::heap_hard_limit)
                                                                       : (5 * gc_heap::heap_hard_limit));
            }
        }
        else
        {
            gc_heap::regions_range =
#ifdef MULTIPLE_HEAPS
            // For SVR use max of 2x total_physical_memory or 256gb
            max(
#else // MULTIPLE_HEAPS
            // for WKS use min
            min(
#endif // MULTIPLE_HEAPS
                (size_t)256 * 1024 * 1024 * 1024, (size_t)(2 * gc_heap::total_physical_mem));
        }
        size_t virtual_mem_limit = GCToOSInterface::GetVirtualMemoryLimit();
        gc_heap::regions_range = min(gc_heap::regions_range, virtual_mem_limit/2);
        gc_heap::regions_range = align_on_page(gc_heap::regions_range);
    }
    GCConfig::SetGCRegionRange(gc_heap::regions_range);
#endif //USE_REGIONS

    size_t seg_size = 0;
    size_t large_seg_size = 0;
    size_t pin_seg_size = 0;
    seg_size = gc_heap::soh_segment_size;

#ifndef USE_REGIONS

    if (gc_heap::heap_hard_limit)
    {
        if (gc_heap::heap_hard_limit_oh[soh])
        {
            // On 32bit we have next guarantees:
            //   0 <= seg_size_from_config <= 1Gb (from max_heap_hard_limit/2)
            //   0 <= (heap_hard_limit = heap_hard_limit_oh[soh] + heap_hard_limit_oh[loh] + heap_hard_limit_oh[poh]) < 4Gb (from gc_heap::compute_hard_limit_from_heap_limits)
            //   0 <= heap_hard_limit_oh[loh] <= 1Gb or < 2Gb
            //   0 <= heap_hard_limit_oh[poh] <= 1Gb or < 2Gb
            //   0 <= large_seg_size <= 1Gb or <= 2Gb (alignment and round up)
            //   0 <= pin_seg_size <= 1Gb or <= 2Gb (alignment and round up)
            //   0 <= soh_segment_size + large_seg_size + pin_seg_size <= 4Gb
            // 4Gb overflow is ok, because 0 size allocation will fail
            large_seg_size = max (gc_heap::adjust_segment_size_hard_limit (gc_heap::heap_hard_limit_oh[loh], nhp), seg_size_from_config);
            pin_seg_size = max (gc_heap::adjust_segment_size_hard_limit (gc_heap::heap_hard_limit_oh[poh], nhp), seg_size_from_config);
        }
        else
        {
            // On 32bit we have next guarantees:
            //   0 <= heap_hard_limit <= 1Gb (from gc_heap::compute_hard_limit)
            //   0 <= soh_segment_size <= 1Gb
            //   0 <= large_seg_size <= 1Gb
            //   0 <= pin_seg_size <= 1Gb
            //   0 <= soh_segment_size + large_seg_size + pin_seg_size <= 3Gb
#ifdef HOST_64BIT
            large_seg_size = gc_heap::use_large_pages_p ? gc_heap::soh_segment_size : gc_heap::soh_segment_size * 2;
#else //HOST_64BIT
            assert (!gc_heap::use_large_pages_p);
            large_seg_size = gc_heap::soh_segment_size;
#endif //HOST_64BIT
            pin_seg_size = large_seg_size;
        }
        if (gc_heap::use_large_pages_p)
            gc_heap::min_segment_size = min_segment_size_hard_limit;
    }
    else
    {
        large_seg_size = get_valid_segment_size (TRUE);
        pin_seg_size = large_seg_size;
    }
    assert (g_theGCHeap->IsValidSegmentSize (seg_size));
    assert (g_theGCHeap->IsValidSegmentSize (large_seg_size));
    assert (g_theGCHeap->IsValidSegmentSize (pin_seg_size));

    dprintf (1, ("%d heaps, soh seg size: %zd mb, loh: %zd mb\n",
        nhp,
        (seg_size / (size_t)1024 / 1024),
        (large_seg_size / 1024 / 1024)));

    gc_heap::min_uoh_segment_size = min (large_seg_size, pin_seg_size);

    if (gc_heap::min_segment_size == 0)
    {
        gc_heap::min_segment_size = min (seg_size, gc_heap::min_uoh_segment_size);
    }
#endif //!USE_REGIONS

    GCConfig::SetHeapCount(static_cast<int64_t>(nhp));

    loh_size_threshold = (size_t)GCConfig::GetLOHThreshold();
    loh_size_threshold = max (loh_size_threshold, LARGE_OBJECT_SIZE);

#ifdef USE_REGIONS
    gc_heap::enable_special_regions_p = (bool)GCConfig::GetGCEnableSpecialRegions();
    size_t gc_region_size = (size_t)GCConfig::GetGCRegionSize();

    if (gc_region_size >= MAX_REGION_SIZE)
    {
        log_init_error_to_host ("The GC RegionSize config is set to %zd bytes (%zd GiB), it needs to be < %zd GiB",
            gc_region_size, gib (gc_region_size), gib (MAX_REGION_SIZE));
        return CLR_E_GC_BAD_REGION_SIZE;
    }

    // Adjust GCRegionSize based on how large each heap would be, for smaller heaps we would
    // like to keep Region sizes small. We choose between 4, 2 and 1mb based on the calculations
    // below (unless its configured explicitly) such that there are at least 2 regions available
    // except for the smallest case. Now the lowest limit possible is 4mb.
    if (gc_region_size == 0)
    {
        // We have a minimum amount of basic regions we have to fit per heap, and we'd like to have the initial
        // regions only take up half of the space.
        size_t max_region_size = gc_heap::regions_range / 2 / nhp / min_regions_per_heap;
        if (max_region_size >= (4 * 1024 * 1024))
        {
            gc_region_size = 4 * 1024 * 1024;
        }
        else if (max_region_size >= (2 * 1024 * 1024))
        {
            gc_region_size = 2 * 1024 * 1024;
        }
        else
        {
            gc_region_size = 1 * 1024 * 1024;
        }
    }

    if (!power_of_two_p(gc_region_size) || ((gc_region_size * nhp * min_regions_per_heap) > gc_heap::regions_range))
    {
        log_init_error_to_host ("Region size is %zd bytes, range is %zd bytes, (%d heaps * %d regions/heap = %d) regions needed initially",
            gc_region_size, gc_heap::regions_range, nhp, min_regions_per_heap, (nhp * min_regions_per_heap));
        return E_OUTOFMEMORY;
    }

    /*
     * Allocation requests less than loh_size_threshold will be allocated on the small object heap.
     *
     * An object cannot span more than one region and regions in small object heap are of the same size - gc_region_size.
     * However, the space available for actual allocations is reduced by the following implementation details -
     *
     * 1.) heap_segment_mem is set to the new pages + sizeof(aligned_plug_and_gap) in make_heap_segment.
     * 2.) a_fit_segment_end_p set pad to Align(min_obj_size, align_const).
     * 3.) a_size_fit_p requires the available space to be >= the allocated size + Align(min_obj_size, align_const)
     *
     * It is guaranteed that an allocation request with this amount or less will succeed unless
     * we cannot commit memory for it.
     */
    int align_const = get_alignment_constant (TRUE);
    size_t effective_max_small_object_size = gc_region_size - sizeof(aligned_plug_and_gap) - Align(min_obj_size, align_const) * 2;

#ifdef FEATURE_STRUCTALIGN
    /*
     * The above assumed FEATURE_STRUCTALIGN is not turned on for platforms where USE_REGIONS is supported, otherwise it is possible
     * that the allocation size is inflated by ComputeMaxStructAlignPad in GCHeap::Alloc and we have to compute an upper bound of that
     * function.
     *
     * Note that ComputeMaxStructAlignPad is defined to be 0 if FEATURE_STRUCTALIGN is turned off.
     */
#error "FEATURE_STRUCTALIGN is not supported for USE_REGIONS"
#endif //FEATURE_STRUCTALIGN

    loh_size_threshold = min (loh_size_threshold, effective_max_small_object_size);
    GCConfig::SetLOHThreshold(loh_size_threshold);

    gc_heap::min_segment_size_shr = index_of_highest_set_bit (gc_region_size);
#else
    gc_heap::min_segment_size_shr = index_of_highest_set_bit (gc_heap::min_segment_size);
#endif //USE_REGIONS

#ifdef MULTIPLE_HEAPS
    assert (nhp <= g_num_processors);
    if (max_nhp_from_config)
    {
        nhp = min (nhp, max_nhp_from_config);
    }
    gc_heap::n_max_heaps = nhp;
    gc_heap::n_heaps = nhp;
    hr = gc_heap::initialize_gc (seg_size, large_seg_size, pin_seg_size, nhp);
#else
    hr = gc_heap::initialize_gc (seg_size, large_seg_size, pin_seg_size);
#endif //MULTIPLE_HEAPS

    GCConfig::SetGCHeapHardLimit(static_cast<int64_t>(gc_heap::heap_hard_limit));
    GCConfig::SetGCHeapHardLimitSOH(static_cast<int64_t>(gc_heap::heap_hard_limit_oh[soh]));
    GCConfig::SetGCHeapHardLimitLOH(static_cast<int64_t>(gc_heap::heap_hard_limit_oh[loh]));
    GCConfig::SetGCHeapHardLimitPOH(static_cast<int64_t>(gc_heap::heap_hard_limit_oh[poh]));

    if (hr != S_OK)
        return hr;

    gc_heap::pm_stress_on = (GCConfig::GetGCProvModeStress() != 0);

#if defined(HOST_64BIT)
    gc_heap::youngest_gen_desired_th = gc_heap::mem_one_percent;
#endif // HOST_64BIT

    WaitForGCEvent = new (nothrow) GCEvent;

    if (!WaitForGCEvent)
    {
        return E_OUTOFMEMORY;
    }

    if (!WaitForGCEvent->CreateManualEventNoThrow(TRUE))
    {
        log_init_error_to_host ("Creation of WaitForGCEvent failed");
        return E_FAIL;
    }

#ifndef FEATURE_NATIVEAOT // NativeAOT forces relocation a different way
#if defined (STRESS_HEAP) && !defined (MULTIPLE_HEAPS)
    if (GCStress<cfg_any>::IsEnabled())
    {
        for (int i = 0; i < GCHeap::NUM_HEAP_STRESS_OBJS; i++)
        {
            m_StressObjs[i] = CreateGlobalHandle(0);
        }
        m_CurStressObj = 0;
    }
#endif //STRESS_HEAP && !MULTIPLE_HEAPS
#endif // FEATURE_NATIVEAOT

    initGCShadow();         // If we are debugging write barriers, initialize heap shadow

#ifdef USE_REGIONS
    gc_heap::ephemeral_low = MAX_PTR;

    gc_heap::ephemeral_high = nullptr;
#endif //!USE_REGIONS

#ifdef MULTIPLE_HEAPS

    for (uint32_t i = 0; i < nhp; i++)
    {
        GCHeap* Hp = new (nothrow) GCHeap();
        if (!Hp)
            return E_OUTOFMEMORY;

        if ((hr = Hp->Init (i))!= S_OK)
        {
            return hr;
        }
    }

    if (!heap_select::init_numa_node_to_heap_map (nhp))
    {
        log_init_error_to_host ("Initialization of NUMA node to heap map failed");
        return E_OUTOFMEMORY;
    }

    // If we have more active processors than heaps we still want to initialize some of the
    // mapping for the rest of the active processors because user threads can still run on
    // them which means it's important to know their numa nodes and map them to a reasonable
    // heap, ie, we wouldn't want to have all such procs go to heap 0.
    if (g_num_active_processors > nhp)
    {
        bool distribute_all_p = false;
#ifdef DYNAMIC_HEAP_COUNT
        distribute_all_p = (gc_heap::dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes);
#endif //DYNAMIC_HEAP_COUNT
        heap_select::distribute_other_procs (distribute_all_p);
    }

    gc_heap* hp = gc_heap::g_heaps[0];

    dynamic_data* gen0_dd = hp->dynamic_data_of (0);
    gc_heap::min_gen0_balance_delta = (dd_min_size (gen0_dd) >> 6);

    bool can_use_cpu_groups = GCToOSInterface::CanEnableGCCPUGroups();
    GCConfig::SetGCCpuGroup(can_use_cpu_groups);

#ifdef HEAP_BALANCE_INSTRUMENTATION
    cpu_group_enabled_p = can_use_cpu_groups;

    if (!GCToOSInterface::GetNumaInfo (&total_numa_nodes_on_machine, &procs_per_numa_node))
    {
        total_numa_nodes_on_machine = 1;

        // Note that if we are in cpu groups we need to take the way proc index is calculated
        // into consideration. It would mean we have more than 64 procs on one numa node -
        // this is mostly for testing (if we want to simulate no numa on a numa system).
        // see vm\gcenv.os.cpp GroupProcNo implementation.
        if (GCToOSInterface::GetCPUGroupInfo (&total_cpu_groups_on_machine, &procs_per_cpu_group))
            procs_per_numa_node = procs_per_cpu_group + ((total_cpu_groups_on_machine - 1) << 6);
        else
            procs_per_numa_node = g_num_processors;
    }
    hb_info_numa_nodes = new (nothrow) heap_balance_info_numa[total_numa_nodes_on_machine];
    dprintf (HEAP_BALANCE_LOG, ("total: %d, numa: %d", g_num_processors, total_numa_nodes_on_machine));

    int hb_info_size_per_proc = sizeof (heap_balance_info_proc);

    for (int numa_node_index = 0; numa_node_index < total_numa_nodes_on_machine; numa_node_index++)
    {
        int hb_info_size_per_node = hb_info_size_per_proc * procs_per_numa_node;
        uint8_t* numa_mem = (uint8_t*)GCToOSInterface::VirtualReserve (hb_info_size_per_node, 0, 0, (uint16_t)numa_node_index);
        if (!numa_mem)
        {
            return E_FAIL;
        }
        if (!GCToOSInterface::VirtualCommit (numa_mem, hb_info_size_per_node, (uint16_t)numa_node_index))
        {
            return E_FAIL;
        }

        heap_balance_info_proc* hb_info_procs = (heap_balance_info_proc*)numa_mem;
        hb_info_numa_nodes[numa_node_index].hb_info_procs = hb_info_procs;

        for (int proc_index = 0; proc_index < (int)procs_per_numa_node; proc_index++)
        {
            heap_balance_info_proc* hb_info_proc = &hb_info_procs[proc_index];
            hb_info_proc->count = default_max_hb_heap_balance_info;
            hb_info_proc->index = 0;
        }
    }
#endif //HEAP_BALANCE_INSTRUMENTATION
#else
    hr = Init (0);
#endif //MULTIPLE_HEAPS
#ifdef USE_REGIONS
    if (initial_regions)
    {
        delete[] initial_regions;
    }
#endif //USE_REGIONS
    if (hr == S_OK)
    {
#ifdef MULTIPLE_HEAPS
        dprintf (6666, ("conserve mem %d, concurent %d, max heap %d", gc_heap::conserve_mem_setting, gc_heap::gc_can_use_concurrent, gc_heap::n_heaps));
#else
        dprintf (6666, ("conserve mem %d, concurent %d, WKS", gc_heap::conserve_mem_setting, gc_heap::gc_can_use_concurrent));
#endif

#ifdef DYNAMIC_HEAP_COUNT
        // if no heap count was specified, and we are told to adjust heap count dynamically ...
        if (gc_heap::dynamic_adaptation_mode == dynamic_adaptation_to_application_sizes)
        {
            // start with only 1 heap
            gc_heap::smoothed_desired_total[0] /= gc_heap::n_heaps;
            int initial_n_heaps = 1;

            dprintf (6666, ("n_heaps is %d, initial n_heaps is %d, %d cores", gc_heap::n_heaps, initial_n_heaps, g_num_processors));

            {
                if (!gc_heap::prepare_to_change_heap_count (initial_n_heaps))
                {
                    // we don't have sufficient resources.
                    return E_FAIL;
                }

                gc_heap::dynamic_heap_count_data.new_n_heaps = initial_n_heaps;
                gc_heap::dynamic_heap_count_data.idle_thread_count = 0;
                gc_heap::dynamic_heap_count_data.init_only_p = true;

                int max_threads_to_wake = max (gc_heap::n_heaps, initial_n_heaps);
                gc_t_join.update_n_threads (max_threads_to_wake);
                gc_heap::gc_start_event.Set ();
            }

            gc_heap::g_heaps[0]->change_heap_count (initial_n_heaps);
            gc_heap::gc_start_event.Reset ();

            // This needs to be different from our initial heap count so we can make sure we wait for
            // the idle threads correctly in gc_thread_function.
            gc_heap::dynamic_heap_count_data.last_n_heaps = 0;

            int target_tcp = (int)GCConfig::GetGCDTargetTCP();
            if (target_tcp > 0)
            {
                gc_heap::dynamic_heap_count_data.target_tcp = (float)target_tcp;
            }
            // This should be adjusted based on the target tcp. See comments in gcpriv.h
            gc_heap::dynamic_heap_count_data.around_target_threshold = 10.0;

            int gen0_growth_soh_ratio_percent = (int)GCConfig::GetGCDGen0GrowthPercent();
            if (gen0_growth_soh_ratio_percent)
            {
                gc_heap::dynamic_heap_count_data.gen0_growth_soh_ratio_percent = (int)GCConfig::GetGCDGen0GrowthPercent() * 0.01f;
            }
            // You can specify what sizes you want to allow DATAS to stay within wrt the SOH stable size.
            // By default DATAS allows 10x this size for gen0 budget when the size is small, and 0.1x when the size is large.
            int gen0_growth_min_permil = (int)GCConfig::GetGCDGen0GrowthMinFactor();
            int gen0_growth_max_permil = (int)GCConfig::GetGCDGen0GrowthMaxFactor();
            if (gen0_growth_min_permil)
            {
                gc_heap::dynamic_heap_count_data.gen0_growth_soh_ratio_min = gen0_growth_min_permil * 0.001f;
            }
            if (gen0_growth_max_permil)
            {
                gc_heap::dynamic_heap_count_data.gen0_growth_soh_ratio_max = gen0_growth_max_permil * 0.001f;
            }

            if (gc_heap::dynamic_heap_count_data.gen0_growth_soh_ratio_min > gc_heap::dynamic_heap_count_data.gen0_growth_soh_ratio_max)
            {
                log_init_error_to_host ("DATAS min permil for gen0 growth %d is greater than max %d, it needs to be lower",
                    gc_heap::dynamic_heap_count_data.gen0_growth_soh_ratio_min, gc_heap::dynamic_heap_count_data.gen0_growth_soh_ratio_max);
                return E_FAIL;
            }

            GCConfig::SetGCDTargetTCP ((int)gc_heap::dynamic_heap_count_data.target_tcp);
            GCConfig::SetGCDGen0GrowthPercent ((int)(gc_heap::dynamic_heap_count_data.gen0_growth_soh_ratio_percent * 100.0f));
            GCConfig::SetGCDGen0GrowthMinFactor ((int)(gc_heap::dynamic_heap_count_data.gen0_growth_soh_ratio_min * 1000.0f));
            GCConfig::SetGCDGen0GrowthMaxFactor ((int)(gc_heap::dynamic_heap_count_data.gen0_growth_soh_ratio_max * 1000.0f));
            dprintf (6666, ("DATAS gen0 growth multiplier will be adjusted by %d%%, cap %.3f-%.3f, min budget %Id, max %Id",
                (int)GCConfig::GetGCDGen0GrowthPercent(),
                gc_heap::dynamic_heap_count_data.gen0_growth_soh_ratio_min, gc_heap::dynamic_heap_count_data.gen0_growth_soh_ratio_max,
                gc_heap::dynamic_heap_count_data.min_gen0_new_allocation, gc_heap::dynamic_heap_count_data.max_gen0_new_allocation));
        }

        GCConfig::SetGCDynamicAdaptationMode (gc_heap::dynamic_adaptation_mode);
#endif //DYNAMIC_HEAP_COUNT
        GCScan::GcRuntimeStructuresValid (TRUE);

        GCToEEInterface::DiagUpdateGenerationBounds();

#if defined(STRESS_REGIONS) && defined(FEATURE_BASICFREEZE)
#ifdef MULTIPLE_HEAPS
        gc_heap* hp = gc_heap::g_heaps[0];
#else
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

        // allocate some artificial ro seg datastructures.
        for (int i = 0; i < 2; i++)
        {
            size_t ro_seg_size = 1024 * 1024;
            // I'm not allocating this within the normal reserved range
            // because ro segs are supposed to always be out of range
            // for regions.
            uint8_t* seg_mem = new (nothrow) uint8_t [ro_seg_size];

            if (seg_mem == nullptr)
            {
                hr = E_FAIL;
                break;
            }

            segment_info seg_info;
            seg_info.pvMem = seg_mem;
            seg_info.ibFirstObject = 0; // nothing is there, don't fake it with sizeof(ObjHeader)
            seg_info.ibAllocated = 0;
            seg_info.ibCommit = ro_seg_size;
            seg_info.ibReserved = seg_info.ibCommit;

            if (!RegisterFrozenSegment(&seg_info))
            {
                hr = E_FAIL;
                break;
            }
        }
#endif //STRESS_REGIONS && FEATURE_BASICFREEZE
    }

    return hr;
}

////
// GC callback functions
bool GCHeap::IsPromoted(Object* object)
{
    return IsPromoted2(object, true);
}

bool GCHeap::IsPromoted2(Object* object, bool bVerifyNextHeader)
{
    uint8_t* o = (uint8_t*)object;

    bool is_marked;

    if (gc_heap::settings.condemned_generation == max_generation)
    {
#ifdef MULTIPLE_HEAPS
        gc_heap* hp = gc_heap::g_heaps[0];
#else
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

#ifdef BACKGROUND_GC
        if (gc_heap::settings.concurrent)
        {
            is_marked = (!((o < hp->background_saved_highest_address) && (o >= hp->background_saved_lowest_address))||
                            hp->background_marked (o));
        }
        else
#endif //BACKGROUND_GC
        {
            is_marked = (!((o < hp->highest_address) && (o >= hp->lowest_address))
                        || hp->is_mark_set (o));
        }
    }
    else
    {
#ifdef USE_REGIONS
        is_marked = (gc_heap::is_in_gc_range (o) ? (gc_heap::is_in_condemned_gc (o) ? gc_heap::is_mark_set (o) : true) : true);
#else
        gc_heap* hp = gc_heap::heap_of (o);
        is_marked = (!((o < hp->gc_high) && (o >= hp->gc_low))
                   || hp->is_mark_set (o));
#endif //USE_REGIONS
    }

// Walking refs when objects are marked seems unexpected
#ifdef _DEBUG
    if (o)
    {
        ((CObjectHeader*)o)->Validate(TRUE, bVerifyNextHeader, is_marked);

        // Frozen objects aren't expected to be "not promoted" here
        assert(is_marked || !IsInFrozenSegment(object));
    }
#endif //_DEBUG

    return is_marked;
}

size_t GCHeap::GetPromotedBytes(int heap_index)
{
#ifdef BACKGROUND_GC
    if (gc_heap::settings.concurrent)
    {
        return gc_heap::bpromoted_bytes (heap_index);
    }
    else
#endif //BACKGROUND_GC
    {
        gc_heap* hp =
#ifdef MULTIPLE_HEAPS
            gc_heap::g_heaps[heap_index];
#else
            pGenGCHeap;
#endif //MULTIPLE_HEAPS
        return hp->get_promoted_bytes();
    }
}

void GCHeap::SetYieldProcessorScalingFactor (float scalingFactor)
{
    if (!gc_heap::spin_count_unit_config_p)
    {
        assert (yp_spin_count_unit != 0);
        uint32_t saved_yp_spin_count_unit = yp_spin_count_unit;
        yp_spin_count_unit = (uint32_t)((float)original_spin_count_unit * scalingFactor / (float)9);

        // It's very suspicious if it becomes 0 and also, we don't want to spin too much.
        if ((yp_spin_count_unit == 0) || (yp_spin_count_unit > MAX_YP_SPIN_COUNT_UNIT))
        {
            yp_spin_count_unit = saved_yp_spin_count_unit;
        }
    }
}

unsigned int GCHeap::WhichGeneration (Object* object)
{
    uint8_t* o = (uint8_t*)object;
#ifdef FEATURE_BASICFREEZE
    if (!((o < g_gc_highest_address) && (o >= g_gc_lowest_address)))
    {
        return INT32_MAX;
    }
#ifndef USE_REGIONS
    if (GCHeap::IsInFrozenSegment (object))
    {
        // in case if the object belongs to an in-range frozen segment
        // For regions those are never in-range.
        return INT32_MAX;
    }
#endif
#endif //FEATURE_BASICFREEZE
    gc_heap* hp = gc_heap::heap_of (o);
    unsigned int g = hp->object_gennum (o);
    dprintf (3, ("%zx is in gen %d", (size_t)object, g));
    return g;
}

enable_no_gc_region_callback_status GCHeap::EnableNoGCRegionCallback(NoGCRegionCallbackFinalizerWorkItem* callback, uint64_t callback_threshold)
{
    return gc_heap::enable_no_gc_callback(callback, callback_threshold);
}

FinalizerWorkItem* GCHeap::GetExtraWorkForFinalization()
{
    return Interlocked::ExchangePointer(&gc_heap::finalizer_work, nullptr);
}

unsigned int GCHeap::GetGenerationWithRange (Object* object, uint8_t** ppStart, uint8_t** ppAllocated, uint8_t** ppReserved)
{
    int generation = -1;
    heap_segment * hs = gc_heap::find_segment ((uint8_t*)object, FALSE);
#ifdef USE_REGIONS
    generation = heap_segment_gen_num (hs);
    if (generation == max_generation)
    {
        if (heap_segment_loh_p (hs))
        {
            generation = loh_generation;
        }
        else if (heap_segment_poh_p (hs))
        {
            generation = poh_generation;
        }
    }

    *ppStart = heap_segment_mem (hs);
    *ppAllocated = heap_segment_allocated (hs);
    *ppReserved = heap_segment_reserved (hs);
#else
#ifdef MULTIPLE_HEAPS
    gc_heap* hp = heap_segment_heap (hs);
#else
    gc_heap* hp = __this;
#endif //MULTIPLE_HEAPS
    if (hs == hp->ephemeral_heap_segment)
    {
        uint8_t* reserved = heap_segment_reserved (hs);
        uint8_t* end = heap_segment_allocated(hs);
        for (int gen = 0; gen < max_generation; gen++)
        {
            uint8_t* start = generation_allocation_start (hp->generation_of (gen));
            if ((uint8_t*)object >= start)
            {
                generation = gen;
                *ppStart = start;
                *ppAllocated = end;
                *ppReserved = reserved;
                break;
            }
            end = reserved = start;
        }
        if (generation == -1)
        {
            generation = max_generation;
            *ppStart = heap_segment_mem (hs);
            *ppAllocated = *ppReserved = generation_allocation_start (hp->generation_of (max_generation - 1));
        }
    }
    else
    {
        generation = max_generation;
        if (heap_segment_loh_p (hs))
        {
            generation = loh_generation;
        }
        else if (heap_segment_poh_p (hs))
        {
            generation = poh_generation;
        }
        *ppStart = heap_segment_mem (hs);
        *ppAllocated = heap_segment_allocated (hs);
        *ppReserved = heap_segment_reserved (hs);
    }
#endif //USE_REGIONS
    return (unsigned int)generation;
}

bool GCHeap::IsEphemeral (Object* object)
{
    uint8_t* o = (uint8_t*)object;
#if defined(FEATURE_BASICFREEZE) && defined(USE_REGIONS)
    if (!is_in_heap_range (o))
    {
        // Objects in frozen segments are not ephemeral
        return FALSE;
    }
#endif
    gc_heap* hp = gc_heap::heap_of (o);
    return !!hp->ephemeral_pointer_p (o);
}

// Return NULL if can't find next object. When EE is not suspended,
// the result is not accurate: if the input arg is in gen0, the function could
// return zeroed out memory as next object
Object * GCHeap::NextObj (Object * object)
{
#ifdef VERIFY_HEAP
    uint8_t* o = (uint8_t*)object;

#ifndef FEATURE_BASICFREEZE
    if (!((o < g_gc_highest_address) && (o >= g_gc_lowest_address)))
    {
        return NULL;
    }
#endif //!FEATURE_BASICFREEZE

    heap_segment * hs = gc_heap::find_segment (o, FALSE);
    if (!hs)
    {
        return NULL;
    }

    BOOL large_object_p = heap_segment_uoh_p (hs);
    if (large_object_p)
        return NULL; //could be racing with another core allocating.
#ifdef MULTIPLE_HEAPS
    gc_heap* hp = heap_segment_heap (hs);
#else //MULTIPLE_HEAPS
    gc_heap* hp = 0;
#endif //MULTIPLE_HEAPS
#ifdef USE_REGIONS
    unsigned int g = heap_segment_gen_num (hs);
#else
    unsigned int g = hp->object_gennum ((uint8_t*)object);
#endif
    int align_const = get_alignment_constant (!large_object_p);
    uint8_t* nextobj = o + Align (size (o), align_const);
    if (nextobj <= o) // either overflow or 0 sized object.
    {
        return NULL;
    }

    if (nextobj < heap_segment_mem (hs))
    {
        return NULL;
    }

    uint8_t* saved_alloc_allocated = hp->alloc_allocated;
    heap_segment* saved_ephemeral_heap_segment = hp->ephemeral_heap_segment;

    // We still want to verify nextobj that lands between heap_segment_allocated and alloc_allocated
    // on the ephemeral segment. In regions these 2 could be changed by another thread so we need
    // to make sure they are still in sync by the time we check. If they are not in sync, we just
    // bail which means we don't validate the next object during that small window and that's fine.
    //
    // We also miss validating nextobj if it's in the segment that just turned into the new ephemeral
    // segment since we saved which is also a very small window and again that's fine.
    if ((nextobj >= heap_segment_allocated (hs)) &&
        ((hs != saved_ephemeral_heap_segment) ||
         !in_range_for_segment(saved_alloc_allocated, saved_ephemeral_heap_segment) ||
         (nextobj >= saved_alloc_allocated)))
    {
        return NULL;
    }

    return (Object *)nextobj;
#else
    return nullptr;
#endif // VERIFY_HEAP
}

// returns TRUE if the pointer is in one of the GC heaps.
bool GCHeap::IsHeapPointer (void* vpObject, bool small_heap_only)
{
    uint8_t* object = (uint8_t*) vpObject;
#ifndef FEATURE_BASICFREEZE
    if (!((object < g_gc_highest_address) && (object >= g_gc_lowest_address)))
        return FALSE;
#endif //!FEATURE_BASICFREEZE

    heap_segment * hs = gc_heap::find_segment (object, small_heap_only);
    return !!hs;
}

void GCHeap::Promote(Object** ppObject, ScanContext* sc, uint32_t flags)
{
    THREAD_NUMBER_FROM_CONTEXT;
#ifndef MULTIPLE_HEAPS
    const int thread = 0;
#endif //!MULTIPLE_HEAPS

    uint8_t* o = (uint8_t*)*ppObject;

    if (!gc_heap::is_in_find_object_range (o))
    {
        return;
    }

#ifdef DEBUG_DestroyedHandleValue
    // we can race with destroy handle during concurrent scan
    if (o == (uint8_t*)DEBUG_DestroyedHandleValue)
        return;
#endif //DEBUG_DestroyedHandleValue

    HEAP_FROM_THREAD;

    gc_heap* hp = gc_heap::heap_of (o);

#ifdef USE_REGIONS
    if (!gc_heap::is_in_condemned_gc (o))
#else //USE_REGIONS
    if ((o < hp->gc_low) || (o >= hp->gc_high))
#endif //USE_REGIONS
    {
        return;
    }

    dprintf (3, ("Promote %zx", (size_t)o));

    if (flags & GC_CALL_INTERIOR)
    {
        if ((o = hp->find_object (o)) == 0)
        {
            return;
        }
    }

#ifdef FEATURE_CONSERVATIVE_GC
    // For conservative GC, a value on stack may point to middle of a free object.
    // In this case, we don't need to promote the pointer.
    if (GCConfig::GetConservativeGC()
        && ((CObjectHeader*)o)->IsFree())
    {
        return;
    }
#endif

#ifdef _DEBUG
    ((CObjectHeader*)o)->Validate();
#else
    UNREFERENCED_PARAMETER(sc);
#endif //_DEBUG

    if (flags & GC_CALL_PINNED)
        hp->pin_object (o, (uint8_t**) ppObject);

#ifdef STRESS_PINNING
    if ((++n_promote % 20) == 1)
            hp->pin_object (o, (uint8_t**) ppObject);
#endif //STRESS_PINNING

    hpt->mark_object_simple (&o THREAD_NUMBER_ARG);

    STRESS_LOG_ROOT_PROMOTE(ppObject, o, o ? header(o)->GetMethodTable() : NULL);
}

void GCHeap::Relocate (Object** ppObject, ScanContext* sc,
                       uint32_t flags)
{
    UNREFERENCED_PARAMETER(sc);

    uint8_t* object = (uint8_t*)(Object*)(*ppObject);

    if (!gc_heap::is_in_find_object_range (object))
    {
        return;
    }

    THREAD_NUMBER_FROM_CONTEXT;

    //dprintf (3, ("Relocate location %zx\n", (size_t)ppObject));
    dprintf (3, ("R: %zx", (size_t)ppObject));

    gc_heap* hp = gc_heap::heap_of (object);

#ifdef _DEBUG
    if (!(flags & GC_CALL_INTERIOR))
    {
        // We cannot validate this object if it's in the condemned gen because it could
        // be one of the objects that were overwritten by an artificial gap due to a pinned plug.
#ifdef USE_REGIONS
        if (!gc_heap::is_in_condemned_gc (object))
#else //USE_REGIONS
        if (!((object >= hp->gc_low) && (object < hp->gc_high)))
#endif //USE_REGIONS
        {
            ((CObjectHeader*)object)->Validate(FALSE);
        }
    }
#endif //_DEBUG

    dprintf (3, ("Relocate %zx\n", (size_t)object));

    uint8_t* pheader;

    if ((flags & GC_CALL_INTERIOR) && gc_heap::settings.loh_compaction)
    {
#ifdef USE_REGIONS
        if (!gc_heap::is_in_condemned_gc (object))
#else //USE_REGIONS
        if (!((object >= hp->gc_low) && (object < hp->gc_high)))
#endif //USE_REGIONS
        {
            return;
        }

        if (gc_heap::loh_object_p (object))
        {
            pheader = hp->find_object (object);
            if (pheader == 0)
            {
                return;
            }

            ptrdiff_t ref_offset = object - pheader;
            hp->relocate_address(&pheader THREAD_NUMBER_ARG);
            *ppObject = (Object*)(pheader + ref_offset);
            return;
        }
    }

    {
        pheader = object;
        hp->relocate_address(&pheader THREAD_NUMBER_ARG);
        *ppObject = (Object*)pheader;
    }

    STRESS_LOG_ROOT_RELOCATE(ppObject, object, pheader, ((!(flags & GC_CALL_INTERIOR)) ? ((Object*)object)->GetGCSafeMethodTable() : 0));
}

#ifndef FEATURE_NATIVEAOT // NativeAOT forces relocation a different way
#ifdef STRESS_HEAP
// Allocate small object with an alignment requirement of 8-bytes.

// CLRRandom implementation can produce FPU exceptions if
// the test/application run by CLR is enabling any FPU exceptions.
// We want to avoid any unexpected exception coming from stress
// infrastructure, so CLRRandom is not an option.
// The code below is a replicate of CRT rand() implementation.
// Using CRT rand() is not an option because we will interfere with the user application
// that may also use it.
int StressRNG(int iMaxValue)
{
    static BOOL bisRandInit = FALSE;
    static int lHoldrand = 1L;

    if (!bisRandInit)
    {
        lHoldrand = (int)time(NULL);
        bisRandInit = TRUE;
    }
    int randValue = (((lHoldrand = lHoldrand * 214013L + 2531011L) >> 16) & 0x7fff);
    return randValue % iMaxValue;
}

#endif //STRESS_HEAP
#endif //!FEATURE_NATIVEAOT

/*static*/ bool GCHeap::IsLargeObject(Object *pObj)
{
    return size( pObj ) >= loh_size_threshold;
}


// free up object so that things will move and then do a GC
//return TRUE if GC actually happens, otherwise FALSE
bool GCHeap::StressHeap(gc_alloc_context * context)
{
#if defined(STRESS_HEAP) && !defined(FEATURE_NATIVEAOT)
    alloc_context* acontext = static_cast<alloc_context*>(context);
    assert(context != nullptr);

    // if GC stress was dynamically disabled during this run we return FALSE
    if (!GCStressPolicy::IsEnabled())
        return FALSE;

#ifdef _DEBUG
    if (g_pConfig->FastGCStressLevel() && !GCToEEInterface::GetThread()->StressHeapIsEnabled()) {
        return FALSE;
    }
#endif //_DEBUG

    if ((g_pConfig->GetGCStressLevel() & EEConfig::GCSTRESS_UNIQUE)
#ifdef _DEBUG
        || g_pConfig->FastGCStressLevel() > 1
#endif //_DEBUG
        ) {
        if (!Thread::UniqueStack(&acontext)) {
            return FALSE;
        }
    }

#ifdef BACKGROUND_GC
    // don't trigger a GC from the GC threads but still trigger GCs from user threads.
    if (GCToEEInterface::WasCurrentThreadCreatedByGC())
    {
        return FALSE;
    }
#endif //BACKGROUND_GC

    if (g_pStringClass == 0)
    {
        // If the String class has not been loaded, dont do any stressing. This should
        // be kept to a minimum to get as complete coverage as possible.
        _ASSERTE(g_fEEInit);
        return FALSE;
    }

#ifndef MULTIPLE_HEAPS
    static int32_t OneAtATime = -1;

    // Only bother with this if the stress level is big enough and if nobody else is
    // doing it right now.  Note that some callers are inside the AllocLock and are
    // guaranteed synchronized.  But others are using AllocationContexts and have no
    // particular synchronization.
    //
    // For this latter case, we want a very high-speed way of limiting this to one
    // at a time.  A secondary advantage is that we release part of our StressObjs
    // buffer sparingly but just as effectively.

    if (Interlocked::Increment(&OneAtATime) == 0 &&
        !TrackAllocations()) // Messing with object sizes can confuse the profiler (see ICorProfilerInfo::GetObjectSize)
    {
        StringObject* str;

        // If the current string is used up
        if (HndFetchHandle(m_StressObjs[m_CurStressObj]) == 0)
        {
            // Populate handles with strings
            int i = m_CurStressObj;
            while(HndFetchHandle(m_StressObjs[i]) == 0)
            {
                _ASSERTE(m_StressObjs[i] != 0);
                unsigned strLen = ((unsigned)loh_size_threshold - 32) / sizeof(WCHAR);
                unsigned strSize = PtrAlign(StringObject::GetSize(strLen));

                // update the cached type handle before allocating
                SetTypeHandleOnThreadForAlloc(TypeHandle(g_pStringClass));
                str = (StringObject*) pGenGCHeap->allocate (strSize, acontext, /*flags*/ 0);
                if (str)
                {
                    str->SetMethodTable (g_pStringClass);
                    str->SetStringLength (strLen);
                    HndAssignHandle(m_StressObjs[i], ObjectToOBJECTREF(str));
                }
                i = (i + 1) % NUM_HEAP_STRESS_OBJS;
                if (i == m_CurStressObj) break;
            }

            // advance the current handle to the next string
            m_CurStressObj = (m_CurStressObj + 1) % NUM_HEAP_STRESS_OBJS;
        }

        // Get the current string
        str = (StringObject*) OBJECTREFToObject(HndFetchHandle(m_StressObjs[m_CurStressObj]));
        if (str)
        {
            // Chop off the end of the string and form a new object out of it.
            // This will 'free' an object at the beginning of the heap, which will
            // force data movement.  Note that we can only do this so many times.
            // before we have to move on to the next string.
            unsigned sizeOfNewObj = (unsigned)Align(min_obj_size * 31);
            if (str->GetStringLength() > sizeOfNewObj / sizeof(WCHAR))
            {
                unsigned sizeToNextObj = (unsigned)Align(size(str));
                uint8_t* freeObj = ((uint8_t*) str) + sizeToNextObj - sizeOfNewObj;
                pGenGCHeap->make_unused_array (freeObj, sizeOfNewObj);

#if !defined(TARGET_AMD64) && !defined(TARGET_X86)
                // ensure that the write to the new free object is seen by
                // background GC *before* the write to the string length below
                MemoryBarrier();
#endif

                str->SetStringLength(str->GetStringLength() - (sizeOfNewObj / sizeof(WCHAR)));
            }
            else
            {
                // Let the string itself become garbage.
                // will be realloced next time around
                HndAssignHandle(m_StressObjs[m_CurStressObj], 0);
            }
        }
    }
    Interlocked::Decrement(&OneAtATime);
#endif // !MULTIPLE_HEAPS

    if (g_pConfig->GetGCStressLevel() & EEConfig::GCSTRESS_INSTR_JIT)
    {
        // When GCSTRESS_INSTR_JIT is set we see lots of GCs - on every GC-eligible instruction.
        // We do not want all these GC to be gen2 because:
        // - doing only or mostly gen2 is very expensive in this mode
        // - doing only or mostly gen2 prevents coverage of generation-aware behaviors
        // - the main value of this stress mode is to catch stack scanning issues at various/rare locations
        //    in the code and gen2 is not needed for that.

        int rgen = StressRNG(100);

        // gen0:gen1:gen2 distribution: 90:8:2
        if (rgen >= 98)
            rgen = 2;
        else if (rgen >= 90)
            rgen = 1;
        else
            rgen = 0;

        GarbageCollectTry (rgen, FALSE, collection_gcstress);
    }
    else if (IsConcurrentGCEnabled())
    {
        int rgen = StressRNG(10);

        // gen0:gen1:gen2 distribution: 40:40:20
        if (rgen >= 8)
            rgen = 2;
        else if (rgen >= 4)
            rgen = 1;
        else
            rgen = 0;

        GarbageCollectTry (rgen, FALSE, collection_gcstress);
    }
    else
    {
        GarbageCollect(max_generation, FALSE, collection_gcstress);
    }

    return TRUE;
#else
    UNREFERENCED_PARAMETER(context);
    return FALSE;
#endif //STRESS_HEAP && !FEATURE_NATIVEAOT
}

Object* AllocAlign8(alloc_context* acontext, gc_heap* hp, size_t size, uint32_t flags)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    Object* newAlloc = NULL;

    // Depending on where in the object the payload requiring 8-byte alignment resides we might have to
    // align the object header on an 8-byte boundary or midway between two such boundaries. The unaligned
    // case is indicated to the GC via the GC_ALLOC_ALIGN8_BIAS flag.
    size_t desiredAlignment = (flags & GC_ALLOC_ALIGN8_BIAS) ? 4 : 0;

    // Retrieve the address of the next allocation from the context (note that we're inside the alloc
    // lock at this point).
    uint8_t*  result = acontext->alloc_ptr;

    // Will an allocation at this point yield the correct alignment and fit into the remainder of the
    // context?
    if ((((size_t)result & 7) == desiredAlignment) && ((result + size) <= acontext->alloc_limit))
    {
        // Yes, we can just go ahead and make the allocation.
        newAlloc = (Object*) hp->allocate (size, acontext, flags);
        ASSERT(((size_t)newAlloc & 7) == desiredAlignment);
    }
    else
    {
        // No, either the next available address is not aligned in the way we require it or there's
        // not enough space to allocate an object of the required size. In both cases we allocate a
        // padding object (marked as a free object). This object's size is such that it will reverse
        // the alignment of the next header (asserted below).
        //
        // We allocate both together then decide based on the result whether we'll format the space as
        // free object + real object or real object + free object.
        ASSERT((Align(min_obj_size) & 7) == 4);
        CObjectHeader *freeobj = (CObjectHeader*) hp->allocate (Align(size) + Align(min_obj_size), acontext, flags);
        if (freeobj)
        {
            if (((size_t)freeobj & 7) == desiredAlignment)
            {
                // New allocation has desired alignment, return this one and place the free object at the
                // end of the allocated space.
                newAlloc = (Object*)freeobj;
                freeobj = (CObjectHeader*)((uint8_t*)freeobj + Align(size));
            }
            else
            {
                // New allocation is still mis-aligned, format the initial space as a free object and the
                // rest of the space should be correctly aligned for the real object.
                newAlloc = (Object*)((uint8_t*)freeobj + Align(min_obj_size));
                ASSERT(((size_t)newAlloc & 7) == desiredAlignment);
                if (flags & GC_ALLOC_ZEROING_OPTIONAL)
                {
                    // clean the syncblock of the aligned object.
                    *(((PTR_PTR)newAlloc)-1) = 0;
                }
            }
            freeobj->SetFree(min_obj_size);
        }
    }

    return newAlloc;
}

Object*
GCHeap::Alloc(gc_alloc_context* context, size_t size, uint32_t flags REQD_ALIGN_DCL)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
    } CONTRACTL_END;

    TRIGGERSGC();

    Object* newAlloc = NULL;
    alloc_context* acontext = static_cast<alloc_context*>(context);

#ifdef MULTIPLE_HEAPS
    if (acontext->get_alloc_heap() == 0)
    {
        AssignHeap (acontext);
        assert (acontext->get_alloc_heap());
    }
    gc_heap* hp = acontext->get_alloc_heap()->pGenGCHeap;
#else
    gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

    assert(size < loh_size_threshold || (flags & GC_ALLOC_LARGE_OBJECT_HEAP));

    if (flags & GC_ALLOC_USER_OLD_HEAP)
    {
        // The LOH always guarantees at least 8-byte alignment, regardless of platform. Moreover it doesn't
        // support mis-aligned object headers so we can't support biased headers. Luckily for us
        // we've managed to arrange things so the only case where we see a bias is for boxed value types and
        // these can never get large enough to be allocated on the LOH.
        ASSERT((flags & GC_ALLOC_ALIGN8_BIAS) == 0);
        ASSERT(65536 < loh_size_threshold);

        int gen_num = (flags & GC_ALLOC_PINNED_OBJECT_HEAP) ? poh_generation : loh_generation;
        newAlloc = (Object*) hp->allocate_uoh_object (size + ComputeMaxStructAlignPadLarge(requiredAlignment), flags, gen_num, acontext->alloc_bytes_uoh);
        ASSERT(((size_t)newAlloc & 7) == 0);

#ifdef MULTIPLE_HEAPS
        if (flags & GC_ALLOC_FINALIZE)
        {
            // the heap may have changed due to heap balancing - it's important
            // to register the object for finalization on the heap it was allocated on
            hp = gc_heap::heap_of ((uint8_t*)newAlloc);
        }
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_STRUCTALIGN
        newAlloc = (Object*) hp->pad_for_alignment_large ((uint8_t*) newAlloc, requiredAlignment, size);
#endif // FEATURE_STRUCTALIGN
    }
    else
    {
        if (flags & GC_ALLOC_ALIGN8)
        {
            newAlloc = AllocAlign8 (acontext, hp, size, flags);
        }
        else
        {
            newAlloc = (Object*) hp->allocate (size + ComputeMaxStructAlignPad(requiredAlignment), acontext, flags);
        }

#ifdef MULTIPLE_HEAPS
        if (flags & GC_ALLOC_FINALIZE)
        {
            // the heap may have changed due to heap balancing or heaps going out of service
            // to register the object for finalization on the heap it was allocated on
#ifdef DYNAMIC_HEAP_COUNT
            hp = (newAlloc == nullptr) ? acontext->get_alloc_heap()->pGenGCHeap : gc_heap::heap_of ((uint8_t*)newAlloc);
#else //DYNAMIC_HEAP_COUNT
            hp = acontext->get_alloc_heap()->pGenGCHeap;
            assert ((newAlloc == nullptr) || (hp == gc_heap::heap_of ((uint8_t*)newAlloc)));
#endif //DYNAMIC_HEAP_COUNT
        }
#endif //MULTIPLE_HEAPS

#ifdef FEATURE_STRUCTALIGN
        newAlloc = (Object*) hp->pad_for_alignment ((uint8_t*) newAlloc, requiredAlignment, size, acontext);
#endif // FEATURE_STRUCTALIGN
    }

    CHECK_ALLOC_AND_POSSIBLY_REGISTER_FOR_FINALIZATION(newAlloc, size, flags & GC_ALLOC_FINALIZE);
#ifdef USE_REGIONS
    assert (IsHeapPointer (newAlloc));
#endif //USE_REGIONS

    return newAlloc;
}

void
GCHeap::FixAllocContext (gc_alloc_context* context, void* arg, void *heap)
{
    alloc_context* acontext = static_cast<alloc_context*>(context);
#ifdef MULTIPLE_HEAPS

    if (arg != 0)
        acontext->init_alloc_count();

    uint8_t * alloc_ptr = acontext->alloc_ptr;

    if (!alloc_ptr)
        return;

    // The acontext->alloc_heap can be out of sync with the ptrs because
    // of heap re-assignment in allocate
    gc_heap* hp = gc_heap::heap_of (alloc_ptr);
#else
    gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS

    if (heap == NULL || heap == hp)
    {
        hp->fix_allocation_context (acontext, ((arg != 0)? TRUE : FALSE), TRUE);
    }
}

Object*
GCHeap::GetContainingObject (void *pInteriorPtr, bool fCollectedGenOnly)
{
    uint8_t *o = (uint8_t*)pInteriorPtr;

    if (!gc_heap::is_in_find_object_range (o))
    {
        return NULL;
    }

    gc_heap* hp = gc_heap::heap_of (o);

#ifdef USE_REGIONS
    if (fCollectedGenOnly && !gc_heap::is_in_condemned_gc (o))
    {
        return NULL;
    }

#else //USE_REGIONS

    uint8_t* lowest = (fCollectedGenOnly ? hp->gc_low : hp->lowest_address);
    uint8_t* highest = (fCollectedGenOnly ? hp->gc_high : hp->highest_address);

    if (!((o >= lowest) && (o < highest)))
    {
        return NULL;
    }
#endif //USE_REGIONS

    return (Object*)(hp->find_object (o));
}

BOOL should_collect_optimized (dynamic_data* dd, BOOL low_memory_p)
{
    if (dd_new_allocation (dd) < 0)
    {
        return TRUE;
    }

    if (((float)(dd_new_allocation (dd)) / (float)dd_desired_allocation (dd)) < (low_memory_p ? 0.7 : 0.3))
    {
        return TRUE;
    }

    return FALSE;
}

//----------------------------------------------------------------------------
// #GarbageCollector
//
//  API to ensure that a complete new garbage collection takes place
//
HRESULT
GCHeap::GarbageCollect (int generation, bool low_memory_p, int mode)
{
#if defined(HOST_64BIT)
    if (low_memory_p)
    {
        size_t total_allocated = 0;
        size_t total_desired = 0;
#ifdef MULTIPLE_HEAPS
        int hn = 0;
        for (hn = 0; hn < gc_heap::n_heaps; hn++)
        {
            gc_heap* hp = gc_heap::g_heaps [hn];
            total_desired += dd_desired_allocation (hp->dynamic_data_of (0));
            total_allocated += dd_desired_allocation (hp->dynamic_data_of (0))-
                dd_new_allocation (hp->dynamic_data_of (0));
        }
#else
        gc_heap* hp = pGenGCHeap;
        total_desired = dd_desired_allocation (hp->dynamic_data_of (0));
        total_allocated = dd_desired_allocation (hp->dynamic_data_of (0))-
            dd_new_allocation (hp->dynamic_data_of (0));
#endif //MULTIPLE_HEAPS

        if ((total_desired > gc_heap::mem_one_percent) && (total_allocated < gc_heap::mem_one_percent))
        {
            dprintf (2, ("Async low mem but we've only allocated %zu (< 10%% of physical mem) out of %zu, returning",
                         total_allocated, total_desired));

            return S_OK;
        }
    }
#endif // HOST_64BIT

#ifdef MULTIPLE_HEAPS
    gc_heap* hpt = gc_heap::g_heaps[0];
#else
    gc_heap* hpt = 0;
#endif //MULTIPLE_HEAPS

    generation = (generation < 0) ? max_generation : min (generation, (int)max_generation);
    dynamic_data* dd = hpt->dynamic_data_of (generation);

#ifdef BACKGROUND_GC
    if (gc_heap::background_running_p())
    {
        if ((mode == collection_optimized) || (mode & collection_non_blocking))
        {
            return S_OK;
        }
        if (mode & collection_blocking)
        {
            pGenGCHeap->background_gc_wait();
            if (mode & collection_optimized)
            {
                return S_OK;
            }
        }
    }
#endif //BACKGROUND_GC

    if (mode & collection_optimized)
    {
        if (pGenGCHeap->gc_started)
        {
            return S_OK;
        }
        else
        {
            BOOL should_collect = FALSE;
            BOOL should_check_uoh = (generation == max_generation);
#ifdef MULTIPLE_HEAPS
            for (int heap_number = 0; heap_number < gc_heap::n_heaps; heap_number++)
            {
                dynamic_data* dd1 = gc_heap::g_heaps [heap_number]->dynamic_data_of (generation);
                should_collect = should_collect_optimized (dd1, low_memory_p);
                if (should_check_uoh)
                {
                    for (int i = uoh_start_generation; i < total_generation_count && !should_collect; i++)
                    {
                        should_collect = should_collect_optimized (gc_heap::g_heaps [heap_number]->dynamic_data_of (i), low_memory_p);
                    }
                }

                if (should_collect)
                    break;
            }
#else
            should_collect = should_collect_optimized (dd, low_memory_p);
            if (should_check_uoh)
            {
                for (int i = uoh_start_generation; i < total_generation_count && !should_collect; i++)
                {
                    should_collect = should_collect_optimized (hpt->dynamic_data_of (i), low_memory_p);
                }
            }
#endif //MULTIPLE_HEAPS
            if (!should_collect)
            {
                return S_OK;
            }
        }
    }

    size_t CollectionCountAtEntry = dd_collection_count (dd);
    size_t BlockingCollectionCountAtEntry = gc_heap::full_gc_counts[gc_type_blocking];
    size_t CurrentCollectionCount = 0;

retry:

    CurrentCollectionCount = GarbageCollectTry(generation, low_memory_p, mode);

    if ((mode & collection_blocking) &&
        (generation == max_generation) &&
        (gc_heap::full_gc_counts[gc_type_blocking] == BlockingCollectionCountAtEntry))
    {
#ifdef BACKGROUND_GC
        if (gc_heap::background_running_p())
        {
            pGenGCHeap->background_gc_wait();
        }
#endif //BACKGROUND_GC

        goto retry;
    }

    if (CollectionCountAtEntry == CurrentCollectionCount)
    {
        goto retry;
    }

    return S_OK;
}

size_t
GCHeap::GarbageCollectTry (int generation, BOOL low_memory_p, int mode)
{
    int gen = (generation < 0) ?
               max_generation : min (generation, (int)max_generation);

    gc_reason reason = reason_empty;

    if (low_memory_p)
    {
        if (mode & collection_blocking)
        {
            reason = reason_lowmemory_blocking;
        }
        else
        {
            reason = reason_lowmemory;
        }
    }
    else
    {
        reason = reason_induced;
    }

    if (reason == reason_induced)
    {
        if (mode & collection_aggressive)
        {
            reason = reason_induced_aggressive;
        }
        else if (mode & collection_compacting)
        {
            reason = reason_induced_compacting;
        }
        else if (mode & collection_non_blocking)
        {
            reason = reason_induced_noforce;
        }
#ifdef STRESS_HEAP
        else if (mode & collection_gcstress)
        {
            reason = reason_gcstress;
        }
#endif
    }

    return GarbageCollectGeneration (gen, reason);
}

unsigned GCHeap::GetGcCount()
{
    return (unsigned int)VolatileLoad(&pGenGCHeap->settings.gc_index);
}

size_t
GCHeap::GarbageCollectGeneration (unsigned int gen, gc_reason reason)
{
    dprintf (2, ("triggered a GC!"));

#ifdef COMMITTED_BYTES_SHADOW
    // This stress the refresh memory limit work by
    // refreshing all the time when a GC happens.
    GCHeap::RefreshMemoryLimit();
#endif //COMMITTED_BYTES_SHADOW

#ifdef MULTIPLE_HEAPS
    gc_heap* hpt = gc_heap::g_heaps[0];
#else
    gc_heap* hpt = 0;
#endif //MULTIPLE_HEAPS
    bool cooperative_mode = true;
    dynamic_data* dd = hpt->dynamic_data_of (gen);
    size_t localCount = dd_collection_count (dd);

    enter_spin_lock (&gc_heap::gc_lock);
    dprintf (SPINLOCK_LOG, ("GC Egc"));
    ASSERT_HOLDING_SPIN_LOCK(&gc_heap::gc_lock);

    //don't trigger another GC if one was already in progress
    //while waiting for the lock
    {
        size_t col_count = dd_collection_count (dd);

        if (localCount != col_count)
        {
#ifdef SYNCHRONIZATION_STATS
            gc_lock_contended++;
#endif //SYNCHRONIZATION_STATS
            dprintf (SPINLOCK_LOG, ("no need GC Lgc"));
            leave_spin_lock (&gc_heap::gc_lock);

            // We don't need to release msl here 'cause this means a GC
            // has happened and would have release all msl's.
            return col_count;
         }
    }

    gc_heap::g_low_memory_status = (reason == reason_lowmemory) ||
                                    (reason == reason_lowmemory_blocking) ||
                                    (gc_heap::latency_level == latency_level_memory_footprint);

    gc_trigger_reason = reason;

#ifdef MULTIPLE_HEAPS
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap::g_heaps[i]->reset_gc_done();
    }
#else
    gc_heap::reset_gc_done();
#endif //MULTIPLE_HEAPS

    gc_heap::gc_started = TRUE;

    {
        init_sync_log_stats();

#ifndef MULTIPLE_HEAPS
        cooperative_mode = gc_heap::enable_preemptive ();

        dprintf (2, ("Suspending EE"));
        gc_heap::suspended_start_time = GetHighPrecisionTimeStamp();
        BEGIN_TIMING(suspend_ee_during_log);
        GCToEEInterface::SuspendEE(SUSPEND_FOR_GC);
        END_TIMING(suspend_ee_during_log);
        gc_heap::proceed_with_gc_p = gc_heap::should_proceed_with_gc();
        gc_heap::disable_preemptive (cooperative_mode);
        if (gc_heap::proceed_with_gc_p)
            pGenGCHeap->settings.init_mechanisms();
        else
            gc_heap::update_collection_counts_for_no_gc();

#endif //!MULTIPLE_HEAPS
    }

    unsigned int condemned_generation_number = gen;

    // We want to get a stack from the user thread that triggered the GC
    // instead of on the GC thread which is the case for Server GC.
    // But we are doing it for Workstation GC as well to be uniform.
    FIRE_EVENT(GCTriggered, static_cast<uint32_t>(reason));

#ifdef MULTIPLE_HEAPS
    GcCondemnedGeneration = condemned_generation_number;

    cooperative_mode = gc_heap::enable_preemptive ();

    BEGIN_TIMING(gc_during_log);
    gc_heap::ee_suspend_event.Set();
    gc_heap::wait_for_gc_done();
    END_TIMING(gc_during_log);

    gc_heap::disable_preemptive (cooperative_mode);

    condemned_generation_number = GcCondemnedGeneration;
#else
    if (gc_heap::proceed_with_gc_p)
    {
        BEGIN_TIMING(gc_during_log);
        pGenGCHeap->garbage_collect (condemned_generation_number);
        if (gc_heap::pm_trigger_full_gc)
        {
            pGenGCHeap->garbage_collect_pm_full_gc();
        }
        END_TIMING(gc_during_log);
    }
#endif //MULTIPLE_HEAPS

#ifndef MULTIPLE_HEAPS
#ifdef BACKGROUND_GC
    if (!gc_heap::dont_restart_ee_p)
#endif //BACKGROUND_GC
    {
#ifdef BACKGROUND_GC
        gc_heap::add_bgc_pause_duration_0();
#endif //BACKGROUND_GC
        BEGIN_TIMING(restart_ee_during_log);
        GCToEEInterface::RestartEE(TRUE);
        END_TIMING(restart_ee_during_log);
    }
    process_sync_log_stats();
    gc_heap::gc_started = FALSE;
    gc_heap::set_gc_done();
    dprintf (SPINLOCK_LOG, ("GC Lgc"));
    leave_spin_lock (&gc_heap::gc_lock);
#endif //!MULTIPLE_HEAPS

#ifdef FEATURE_PREMORTEM_FINALIZATION
    GCToEEInterface::EnableFinalization(!pGenGCHeap->settings.concurrent && pGenGCHeap->settings.found_finalizers);
#endif // FEATURE_PREMORTEM_FINALIZATION

    return dd_collection_count (dd);
}

size_t GCHeap::GetTotalBytesInUse ()
{
    // take lock here to ensure gc_heap::n_heaps doesn't change under us
    enter_spin_lock (&pGenGCHeap->gc_lock);

#ifdef MULTIPLE_HEAPS
    //enumerate all the heaps and get their size.
    size_t tot_size = 0;
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        GCHeap* Hp = gc_heap::g_heaps [i]->vm_heap;
        tot_size += Hp->ApproxTotalBytesInUse();
    }
#else
    size_t tot_size = ApproxTotalBytesInUse();
#endif //MULTIPLE_HEAPS
    leave_spin_lock (&pGenGCHeap->gc_lock);

    return tot_size;
}

// Get the total allocated bytes
uint64_t GCHeap::GetTotalAllocatedBytes()
{
#ifdef MULTIPLE_HEAPS
    uint64_t total_alloc_bytes = 0;
    for (int i = 0; i < gc_heap::n_heaps; i++)
    {
        gc_heap* hp = gc_heap::g_heaps[i];
        total_alloc_bytes += hp->total_alloc_bytes_soh;
        total_alloc_bytes += hp->total_alloc_bytes_uoh;
    }
    return total_alloc_bytes;
#else
    return (pGenGCHeap->total_alloc_bytes_soh +  pGenGCHeap->total_alloc_bytes_uoh);
#endif //MULTIPLE_HEAPS
}

int GCHeap::CollectionCount (int generation, int get_bgc_fgc_count)
{
    if (get_bgc_fgc_count != 0)
    {
#ifdef BACKGROUND_GC
        if (generation == max_generation)
        {
            return (int)(gc_heap::full_gc_counts[gc_type_background]);
        }
        else
        {
            return (int)(gc_heap::ephemeral_fgc_counts[generation]);
        }
#else
        return 0;
#endif //BACKGROUND_GC
    }

#ifdef MULTIPLE_HEAPS
    gc_heap* hp = gc_heap::g_heaps [0];
#else  //MULTIPLE_HEAPS
    gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
    if (generation > max_generation)
        return 0;
    else
        return (int)dd_collection_count (hp->dynamic_data_of (generation));
}

size_t GCHeap::ApproxTotalBytesInUse(BOOL small_heap_only)
{
    size_t totsize = 0;

    // For gen0 it's a bit complicated because we are currently allocating in it. We get the fragmentation first
    // just so that we don't give a negative number for the resulting size.
    generation* gen = pGenGCHeap->generation_of (0);
    size_t gen0_frag = generation_free_list_space (gen) + generation_free_obj_space (gen);
    uint8_t* current_alloc_allocated = pGenGCHeap->alloc_allocated;
    heap_segment* current_eph_seg = pGenGCHeap->ephemeral_heap_segment;
    size_t gen0_size = 0;
#ifdef USE_REGIONS
    heap_segment* gen0_seg = generation_start_segment (gen);
    while (gen0_seg)
    {
        uint8_t* end = in_range_for_segment (current_alloc_allocated, gen0_seg) ?
                       current_alloc_allocated : heap_segment_allocated (gen0_seg);
        gen0_size += end - heap_segment_mem (gen0_seg);

        if (gen0_seg == current_eph_seg)
        {
            break;
        }

        gen0_seg = heap_segment_next (gen0_seg);
    }
#else //USE_REGIONS
    // For segments ephemeral seg does not change.
    gen0_size = current_alloc_allocated - heap_segment_mem (current_eph_seg);
#endif //USE_REGIONS

    totsize = gen0_size - gen0_frag;

    int stop_gen_index = max_generation;

#ifdef BACKGROUND_GC
    if (gc_heap::current_c_gc_state == c_gc_state_planning)
    {
        // During BGC sweep since we can be deleting SOH segments, we avoid walking the segment
        // list.
        generation* oldest_gen = pGenGCHeap->generation_of (max_generation);
        totsize = pGenGCHeap->background_soh_size_end_mark - generation_free_list_space (oldest_gen) - generation_free_obj_space (oldest_gen);
        stop_gen_index--;
    }
#endif //BACKGROUND_GC

    for (int i = (max_generation - 1); i <= stop_gen_index; i++)
    {
        generation* gen = pGenGCHeap->generation_of (i);
        totsize += pGenGCHeap->generation_size (i) - generation_free_list_space (gen) - generation_free_obj_space (gen);
    }

    if (!small_heap_only)
    {
        for (int i = uoh_start_generation; i < total_generation_count; i++)
        {
            generation* gen = pGenGCHeap->generation_of (i);
            totsize += pGenGCHeap->generation_size (i) - generation_free_list_space (gen) - generation_free_obj_space (gen);
        }
    }

    return totsize;
}

#ifdef MULTIPLE_HEAPS
void GCHeap::AssignHeap (alloc_context* acontext)
{
    // Assign heap based on processor
    acontext->set_alloc_heap(GetHeap(heap_select::select_heap(acontext)));
    acontext->set_home_heap(acontext->get_alloc_heap());
    acontext->init_handle_info();
}

GCHeap* GCHeap::GetHeap (int n)
{
    assert (n < gc_heap::n_heaps);
    return gc_heap::g_heaps[n]->vm_heap;
}

#endif //MULTIPLE_HEAPS

bool GCHeap::IsThreadUsingAllocationContextHeap(gc_alloc_context* context, int thread_number)
{
    alloc_context* acontext = static_cast<alloc_context*>(context);
#ifdef MULTIPLE_HEAPS
    // the thread / heap number must be in range
    assert (thread_number < gc_heap::n_heaps);
    assert ((acontext->get_home_heap() == 0) ||
            (acontext->get_home_heap()->pGenGCHeap->heap_number < gc_heap::n_heaps));

    return ((acontext->get_home_heap() == GetHeap(thread_number)) ||
            ((acontext->get_home_heap() == 0) && (thread_number == 0)));
#else
    UNREFERENCED_PARAMETER(acontext);
    UNREFERENCED_PARAMETER(thread_number);
    return true;
#endif //MULTIPLE_HEAPS
}

// Returns the number of processors required to trigger the use of thread based allocation contexts
int GCHeap::GetNumberOfHeaps ()
{
#ifdef MULTIPLE_HEAPS
    return gc_heap::n_heaps;
#else
    return 1;
#endif //MULTIPLE_HEAPS
}

/*
  in this way we spend extra time cycling through all the heaps while create the handle
  it ought to be changed by keeping alloc_context.home_heap as number (equals heap_number)
*/
int GCHeap::GetHomeHeapNumber ()
{
#ifdef MULTIPLE_HEAPS
    gc_alloc_context* ctx = GCToEEInterface::GetAllocContext();
    if (!ctx)
    {
        return 0;
    }

    GCHeap *hp = static_cast<alloc_context*>(ctx)->get_home_heap();
    return (hp ? hp->pGenGCHeap->heap_number : 0);
#else
    return 0;
#endif //MULTIPLE_HEAPS
}

unsigned int GCHeap::GetCondemnedGeneration()
{
    return gc_heap::settings.condemned_generation;
}

void GCHeap::GetMemoryInfo(uint64_t* highMemLoadThresholdBytes,
                           uint64_t* totalAvailableMemoryBytes,
                           uint64_t* lastRecordedMemLoadBytes,
                           uint64_t* lastRecordedHeapSizeBytes,
                           uint64_t* lastRecordedFragmentationBytes,
                           uint64_t* totalCommittedBytes,
                           uint64_t* promotedBytes,
                           uint64_t* pinnedObjectCount,
                           uint64_t* finalizationPendingCount,
                           uint64_t* index,
                           uint32_t* generation,
                           uint32_t* pauseTimePct,
                           bool* isCompaction,
                           bool* isConcurrent,
                           uint64_t* genInfoRaw,
                           uint64_t* pauseInfoRaw,
                           int kind)
{
    last_recorded_gc_info* last_gc_info = 0;

    if ((gc_kind)kind == gc_kind_ephemeral)
    {
        last_gc_info = &gc_heap::last_ephemeral_gc_info;
    }
    else if ((gc_kind)kind == gc_kind_full_blocking)
    {
        last_gc_info = &gc_heap::last_full_blocking_gc_info;
    }
#ifdef BACKGROUND_GC
    else if ((gc_kind)kind == gc_kind_background)
    {
        last_gc_info = gc_heap::get_completed_bgc_info();
    }
#endif //BACKGROUND_GC
    else
    {
        assert ((gc_kind)kind == gc_kind_any);
#ifdef BACKGROUND_GC
        if (gc_heap::is_last_recorded_bgc)
        {
            last_gc_info = gc_heap::get_completed_bgc_info();
        }
        else
#endif //BACKGROUND_GC
        {
            last_gc_info = ((gc_heap::last_ephemeral_gc_info.index > gc_heap::last_full_blocking_gc_info.index) ?
                &gc_heap::last_ephemeral_gc_info : &gc_heap::last_full_blocking_gc_info);
        }
    }

    *highMemLoadThresholdBytes = (uint64_t) (((double)(gc_heap::high_memory_load_th)) / 100 * gc_heap::total_physical_mem);
    *totalAvailableMemoryBytes = gc_heap::heap_hard_limit != 0 ? gc_heap::heap_hard_limit : gc_heap::total_physical_mem;
    *lastRecordedMemLoadBytes = (uint64_t) (((double)(last_gc_info->memory_load)) / 100 * gc_heap::total_physical_mem);
    *lastRecordedHeapSizeBytes = last_gc_info->heap_size;
    *lastRecordedFragmentationBytes = last_gc_info->fragmentation;
    *totalCommittedBytes = last_gc_info->total_committed;
    *promotedBytes = last_gc_info->promoted;
    *pinnedObjectCount = last_gc_info->pinned_objects;
    *finalizationPendingCount = last_gc_info->finalize_promoted_objects;
    *index = last_gc_info->index;
    *generation = last_gc_info->condemned_generation;
    *pauseTimePct = (int)(last_gc_info->pause_percentage * 100);
    *isCompaction = last_gc_info->compaction;
    *isConcurrent = last_gc_info->concurrent;
    int genInfoIndex = 0;
    for (int i = 0; i < total_generation_count; i++)
    {
        genInfoRaw[genInfoIndex++] = last_gc_info->gen_info[i].size_before;
        genInfoRaw[genInfoIndex++] = last_gc_info->gen_info[i].fragmentation_before;
        genInfoRaw[genInfoIndex++] = last_gc_info->gen_info[i].size_after;
        genInfoRaw[genInfoIndex++] = last_gc_info->gen_info[i].fragmentation_after;
    }
    for (int i = 0; i < 2; i++)
    {
        // convert it to 100-ns units that TimeSpan needs.
        pauseInfoRaw[i] = (uint64_t)(last_gc_info->pause_durations[i]) * 10;
    }

#ifdef _DEBUG
    if (VolatileLoadWithoutBarrier (&last_gc_info->index) != 0)
    {
        if ((gc_kind)kind == gc_kind_ephemeral)
        {
            assert (last_gc_info->condemned_generation < max_generation);
        }
        else if ((gc_kind)kind == gc_kind_full_blocking)
        {
            assert (last_gc_info->condemned_generation == max_generation);
            assert (last_gc_info->concurrent == false);
        }
#ifdef BACKGROUND_GC
        else if ((gc_kind)kind == gc_kind_background)
        {
            assert (last_gc_info->condemned_generation == max_generation);
            assert (last_gc_info->concurrent == true);
        }
#endif //BACKGROUND_GC
    }
#endif //_DEBUG
}

int64_t GCHeap::GetTotalPauseDuration()
{
    return (int64_t)(gc_heap::total_suspended_time * 10);
}

void GCHeap::EnumerateConfigurationValues(void* context, ConfigurationValueFunc configurationValueFunc)
{
    GCConfig::EnumerateConfigurationValues(context, configurationValueFunc);
}

uint32_t GCHeap::GetMemoryLoad()
{
    uint32_t memory_load = 0;
    if (gc_heap::settings.exit_memory_load != 0)
        memory_load = gc_heap::settings.exit_memory_load;
    else if (gc_heap::settings.entry_memory_load != 0)
        memory_load = gc_heap::settings.entry_memory_load;

    return memory_load;
}

int GCHeap::GetGcLatencyMode()
{
    return (int)(pGenGCHeap->settings.pause_mode);
}

int GCHeap::SetGcLatencyMode (int newLatencyMode)
{
    if (gc_heap::settings.pause_mode == pause_no_gc)
        return (int)set_pause_mode_no_gc;

    gc_pause_mode new_mode = (gc_pause_mode)newLatencyMode;

    if (new_mode == pause_low_latency)
    {
#ifndef MULTIPLE_HEAPS
        pGenGCHeap->settings.pause_mode = new_mode;
#endif //!MULTIPLE_HEAPS
    }
    else if (new_mode == pause_sustained_low_latency)
    {
#ifdef BACKGROUND_GC
        if (gc_heap::gc_can_use_concurrent)
        {
            pGenGCHeap->settings.pause_mode = new_mode;
        }
#endif //BACKGROUND_GC
    }
    else
    {
        pGenGCHeap->settings.pause_mode = new_mode;
    }

#ifdef BACKGROUND_GC
    if (gc_heap::background_running_p())
    {
        // If we get here, it means we are doing an FGC. If the pause
        // mode was altered we will need to save it in the BGC settings.
        if (gc_heap::saved_bgc_settings.pause_mode != new_mode)
        {
            gc_heap::saved_bgc_settings.pause_mode = new_mode;
        }
    }
#endif //BACKGROUND_GC

    return (int)set_pause_mode_success;
}

int GCHeap::GetLOHCompactionMode()
{
#ifdef FEATURE_LOH_COMPACTION
    return pGenGCHeap->loh_compaction_mode;
#else
    return loh_compaction_default;
#endif //FEATURE_LOH_COMPACTION
}

void GCHeap::SetLOHCompactionMode (int newLOHCompactionMode)
{
#ifdef FEATURE_LOH_COMPACTION
    pGenGCHeap->loh_compaction_mode = (gc_loh_compaction_mode)newLOHCompactionMode;
#endif //FEATURE_LOH_COMPACTION
}

bool GCHeap::RegisterForFullGCNotification(uint32_t gen2Percentage,
                                           uint32_t lohPercentage)
{
#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps [hn];
        hp->fgn_last_alloc = dd_new_allocation (hp->dynamic_data_of (0));
        hp->fgn_maxgen_percent = gen2Percentage;
    }
#else //MULTIPLE_HEAPS
    pGenGCHeap->fgn_last_alloc = dd_new_allocation (pGenGCHeap->dynamic_data_of (0));
    pGenGCHeap->fgn_maxgen_percent = gen2Percentage;
#endif //MULTIPLE_HEAPS

    pGenGCHeap->full_gc_approach_event.Reset();
    pGenGCHeap->full_gc_end_event.Reset();
    pGenGCHeap->full_gc_approach_event_set = false;

    pGenGCHeap->fgn_loh_percent = lohPercentage;

    return TRUE;
}

bool GCHeap::CancelFullGCNotification()
{
#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps [hn];
        hp->fgn_maxgen_percent = 0;
    }
#else //MULTIPLE_HEAPS
    pGenGCHeap->fgn_maxgen_percent = 0;
#endif //MULTIPLE_HEAPS

    pGenGCHeap->fgn_loh_percent = 0;
    pGenGCHeap->full_gc_approach_event.Set();
    pGenGCHeap->full_gc_end_event.Set();

    return TRUE;
}

int GCHeap::WaitForFullGCApproach(int millisecondsTimeout)
{
    dprintf (2, ("WFGA: Begin wait"));
    int result = gc_heap::full_gc_wait (&(pGenGCHeap->full_gc_approach_event), millisecondsTimeout);
    dprintf (2, ("WFGA: End wait"));
    return result;
}

int GCHeap::WaitForFullGCComplete(int millisecondsTimeout)
{
    dprintf (2, ("WFGE: Begin wait"));
    int result = gc_heap::full_gc_wait (&(pGenGCHeap->full_gc_end_event), millisecondsTimeout);
    dprintf (2, ("WFGE: End wait"));
    return result;
}

int GCHeap::StartNoGCRegion(uint64_t totalSize, bool lohSizeKnown, uint64_t lohSize, bool disallowFullBlockingGC)
{
    NoGCRegionLockHolder lh;

    dprintf (1, ("begin no gc called"));
    start_no_gc_region_status status = gc_heap::prepare_for_no_gc_region (totalSize, lohSizeKnown, lohSize, disallowFullBlockingGC);
    if (status == start_no_gc_success)
    {
        GarbageCollect (max_generation);
        status = gc_heap::get_start_no_gc_region_status();
    }

    if (status != start_no_gc_success)
        gc_heap::handle_failure_for_no_gc();

    return (int)status;
}

int GCHeap::EndNoGCRegion()
{
    NoGCRegionLockHolder lh;
    return (int)gc_heap::end_no_gc_region();
}

void GCHeap::PublishObject (uint8_t* Obj)
{
#ifdef BACKGROUND_GC
    gc_heap* hp = gc_heap::heap_of (Obj);
    hp->bgc_alloc_lock->uoh_alloc_done (Obj);
    hp->bgc_untrack_uoh_alloc();
#endif //BACKGROUND_GC
}

// Get the segment size to use, making sure it conforms.
size_t GCHeap::GetValidSegmentSize(bool large_seg)
{
#ifdef USE_REGIONS
    return (large_seg ? global_region_allocator.get_large_region_alignment() :
                        global_region_allocator.get_region_alignment());
#else
    return (large_seg ? gc_heap::min_uoh_segment_size : gc_heap::soh_segment_size);
#endif //USE_REGIONS
}

void GCHeap::SetReservedVMLimit (size_t vmlimit)
{
    gc_heap::reserved_memory_limit = vmlimit;
}

//versions of same method on each heap

#ifdef FEATURE_PREMORTEM_FINALIZATION
Object* GCHeap::GetNextFinalizableObject()
{

#ifdef MULTIPLE_HEAPS

    //return the first non critical one in the first queue.
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps [hn];
        Object* O = hp->finalize_queue->GetNextFinalizableObject(TRUE);
        if (O)
            return O;
    }
    //return the first non critical/critical one in the first queue.
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps [hn];
        Object* O = hp->finalize_queue->GetNextFinalizableObject(FALSE);
        if (O)
            return O;
    }
    return 0;


#else //MULTIPLE_HEAPS
    return pGenGCHeap->finalize_queue->GetNextFinalizableObject();
#endif //MULTIPLE_HEAPS

}

size_t GCHeap::GetNumberFinalizableObjects()
{
#ifdef MULTIPLE_HEAPS
    size_t cnt = 0;
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps [hn];
        cnt += hp->finalize_queue->GetNumberFinalizableObjects();
    }
    return cnt;


#else //MULTIPLE_HEAPS
    return pGenGCHeap->finalize_queue->GetNumberFinalizableObjects();
#endif //MULTIPLE_HEAPS
}

size_t GCHeap::GetFinalizablePromotedCount()
{
#ifdef MULTIPLE_HEAPS
    size_t cnt = 0;

    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps [hn];
        cnt += hp->finalize_queue->GetPromotedCount();
    }
    return cnt;

#else //MULTIPLE_HEAPS
    return pGenGCHeap->finalize_queue->GetPromotedCount();
#endif //MULTIPLE_HEAPS
}

//---------------------------------------------------------------------------
// Finalized class tracking
//---------------------------------------------------------------------------

bool GCHeap::RegisterForFinalization (int gen, Object* obj)
{
    if (gen == -1)
        gen = 0;
    if (((((CObjectHeader*)obj)->GetHeader()->GetBits()) & BIT_SBLK_FINALIZER_RUN))
    {
        ((CObjectHeader*)obj)->GetHeader()->ClrBit(BIT_SBLK_FINALIZER_RUN);
        return true;
    }
    else
    {
        gc_heap* hp = gc_heap::heap_of ((uint8_t*)obj);
        return hp->finalize_queue->RegisterForFinalization (gen, obj);
    }
}

void GCHeap::SetFinalizationRun (Object* obj)
{
    ((CObjectHeader*)obj)->GetHeader()->SetBit(BIT_SBLK_FINALIZER_RUN);
}

#endif //FEATURE_PREMORTEM_FINALIZATION

void GCHeap::DiagWalkObject (Object* obj, walk_fn fn, void* context)
{
    uint8_t* o = (uint8_t*)obj;
    if (o)
    {
        go_through_object_cl (method_table (o), o, size(o), oo,
                                    {
                                        if (*oo)
                                        {
                                            Object *oh = (Object*)*oo;
                                            if (!fn (oh, context))
                                                return;
                                        }
                                    }
            );
    }
}

void GCHeap::DiagWalkObject2 (Object* obj, walk_fn2 fn, void* context)
{
    uint8_t* o = (uint8_t*)obj;
    if (o)
    {
        go_through_object_cl (method_table (o), o, size(o), oo,
                                    {
                                        if (*oo)
                                        {
                                            if (!fn (obj, oo, context))
                                                return;
                                        }
                                    }
            );
    }
}

void GCHeap::DiagWalkSurvivorsWithType (void* gc_context, record_surv_fn fn, void* diag_context, walk_surv_type type, int gen_number)
{
    gc_heap* hp = (gc_heap*)gc_context;

    if (type == walk_for_uoh)
    {
        hp->walk_survivors_for_uoh (diag_context, fn, gen_number);
    }
    else
    {
        hp->walk_survivors (fn, diag_context, type);
    }
}

void GCHeap::DiagWalkHeap (walk_fn fn, void* context, int gen_number, bool walk_large_object_heap_p)
{
    gc_heap::walk_heap (fn, context, gen_number, walk_large_object_heap_p);
}

// Walking the GC Heap requires that the EE is suspended and all heap allocation contexts are fixed.
// DiagWalkHeap is invoked only during a GC, where both requirements are met.
// So DiagWalkHeapWithACHandling facilitates a GC Heap walk outside of a GC by handling allocation contexts logic,
// and it leaves the responsibility of suspending and resuming EE to the callers.
void GCHeap::DiagWalkHeapWithACHandling (walk_fn fn, void* context, int gen_number, bool walk_large_object_heap_p)
{
#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps [hn];
#else
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        hp->fix_allocation_contexts (FALSE);
    }

    DiagWalkHeap (fn, context, gen_number, walk_large_object_heap_p);


#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps [hn];
#else
    {
        gc_heap* hp = pGenGCHeap;
#endif //MULTIPLE_HEAPS
        hp->repair_allocation_contexts (TRUE);
    }
}

void GCHeap::DiagWalkFinalizeQueue (void* gc_context, fq_walk_fn fn)
{
    gc_heap* hp = (gc_heap*)gc_context;
    hp->walk_finalize_queue (fn);
}

void GCHeap::DiagScanFinalizeQueue (fq_scan_fn fn, ScanContext* sc)
{
#ifdef MULTIPLE_HEAPS
    for (int hn = 0; hn < gc_heap::n_heaps; hn++)
    {
        gc_heap* hp = gc_heap::g_heaps [hn];
        hp->finalize_queue->GcScanRoots(fn, hn, sc);
    }
#else
        pGenGCHeap->finalize_queue->GcScanRoots(fn, 0, sc);
#endif //MULTIPLE_HEAPS
}

void GCHeap::DiagScanHandles (handle_scan_fn fn, int gen_number, ScanContext* context)
{
    GCScan::GcScanHandlesForProfilerAndETW (gen_number, context, fn);
}

void GCHeap::DiagScanDependentHandles (handle_scan_fn fn, int gen_number, ScanContext* context)
{
    GCScan::GcScanDependentHandlesForProfilerAndETW (gen_number, context, fn);
}

size_t GCHeap::GetLOHThreshold()
{
    return loh_size_threshold;
}

void GCHeap::DiagGetGCSettings(EtwGCSettingsInfo* etw_settings)
{
#ifdef FEATURE_EVENT_TRACE
    etw_settings->heap_hard_limit = gc_heap::heap_hard_limit;
    etw_settings->loh_threshold = loh_size_threshold;
    etw_settings->physical_memory_from_config = gc_heap::physical_memory_from_config;
    etw_settings->gen0_min_budget_from_config = gc_heap::gen0_min_budget_from_config;
    etw_settings->gen0_max_budget_from_config = gc_heap::gen0_max_budget_from_config;
    etw_settings->high_mem_percent_from_config = gc_heap::high_mem_percent_from_config;
#ifdef BACKGROUND_GC
    etw_settings->concurrent_gc_p = gc_heap::gc_can_use_concurrent;
#else
    etw_settings->concurrent_gc_p = false;
#endif //BACKGROUND_GC
    etw_settings->use_large_pages_p = gc_heap::use_large_pages_p;
    etw_settings->use_frozen_segments_p = gc_heap::use_frozen_segments_p;
    etw_settings->hard_limit_config_p = gc_heap::hard_limit_config_p;
    etw_settings->no_affinitize_p =
#ifdef MULTIPLE_HEAPS
        gc_heap::gc_thread_no_affinitize_p;
#else
        true;
#endif //MULTIPLE_HEAPS
#endif //FEATURE_EVENT_TRACE
}

void GCHeap::NullBridgeObjectsWeakRefs(size_t length, void* unreachableObjectHandles)
{
#ifdef FEATURE_JAVAMARSHAL
    Ref_NullBridgeObjectsWeakRefs(length, unreachableObjectHandles);
#else
    assert(false);
#endif
}

HRESULT GCHeap::WaitUntilConcurrentGCCompleteAsync(int millisecondsTimeout)
{
#ifdef BACKGROUND_GC
    if (gc_heap::background_running_p())
    {
        uint32_t dwRet = pGenGCHeap->background_gc_wait(awr_ignored, millisecondsTimeout);
        if (dwRet == WAIT_OBJECT_0)
            return S_OK;
        else if (dwRet == WAIT_TIMEOUT)
            return HRESULT_FROM_WIN32(ERROR_TIMEOUT);
        else
            return E_FAIL;      // It is not clear if what the last error would be if the wait failed,
                                // as there are too many layers in between. The best we can do is to return E_FAIL;
    }
#endif

    return S_OK;
}

void GCHeap::TemporaryEnableConcurrentGC()
{
#ifdef BACKGROUND_GC
    gc_heap::temp_disable_concurrent_p = false;
#endif //BACKGROUND_GC
}

void GCHeap::TemporaryDisableConcurrentGC()
{
#ifdef BACKGROUND_GC
    gc_heap::temp_disable_concurrent_p = true;
#endif //BACKGROUND_GC
}

bool GCHeap::IsConcurrentGCEnabled()
{
#ifdef BACKGROUND_GC
    return (gc_heap::gc_can_use_concurrent && !(gc_heap::temp_disable_concurrent_p));
#else
    return FALSE;
#endif //BACKGROUND_GC
}

int GCHeap::RefreshMemoryLimit()
{
    return gc_heap::refresh_memory_limit();
}
