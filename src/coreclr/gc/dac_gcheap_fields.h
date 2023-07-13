// Whenever we add field here, we need to bear in mind that we have a scenario for a new clrgc
// is used in an old runtime. In that case, the old runtime's DAC will have to interpret the
// fields the way it was. So fields should only be added at the end of the struct.

DEFINE_FIELD       (alloc_allocated,                    uint8_t*)
DEFINE_DPTR_FIELD  (ephemeral_heap_segment,             dac_heap_segment)
DEFINE_DPTR_FIELD  (finalize_queue,                     dac_finalize_queue)
DEFINE_FIELD       (oom_info,                           oom_history)
DEFINE_ARRAY_FIELD (interesting_data_per_heap,          size_t, NUM_GC_DATA_POINTS)
DEFINE_ARRAY_FIELD (compact_reasons_per_heap,           size_t, MAX_COMPACT_REASONS_COUNT)
DEFINE_ARRAY_FIELD (expand_mechanisms_per_heap,         size_t, MAX_EXPAND_MECHANISMS_COUNT)
DEFINE_ARRAY_FIELD (interesting_mechanism_bits_per_heap,size_t, MAX_GC_MECHANISM_BITS_COUNT)
DEFINE_FIELD       (internal_root_array,                uint8_t*)
DEFINE_FIELD       (internal_root_array_index,          size_t)
DEFINE_FIELD       (heap_analyze_success,               BOOL)
DEFINE_FIELD       (card_table,                         uint32_t*)

#if defined(ALL_FIELDS) || defined(BACKGROUND_GC)
DEFINE_FIELD       (mark_array,                         uint32_t*)
DEFINE_FIELD       (next_sweep_obj,                     uint8_t*)    
DEFINE_FIELD       (background_saved_lowest_address,    uint8_t*)
DEFINE_FIELD       (background_saved_highest_address,   uint8_t*)
#if defined(ALL_FIELDS) || !defined(USE_REGIONS)
DEFINE_DPTR_FIELD  (saved_sweep_ephemeral_seg,          dac_heap_segment)
DEFINE_FIELD       (saved_sweep_ephemeral_start,        uint8_t*)
#else
DEFINE_MISSING_FIELD(saved_sweep_ephemeral_seg)
DEFINE_MISSING_FIELD(saved_sweep_ephemeral_start)
#endif // defined(ALL_FIELDS) || !defined(USE_REGIONS)
#else
DEFINE_MISSING_FIELD(mark_array)
DEFINE_MISSING_FIELD(next_sweep_obj)
DEFINE_MISSING_FIELD(background_saved_lowest_address)
DEFINE_MISSING_FIELD(background_saved_highest_address)
DEFINE_MISSING_FIELD(saved_sweep_ephemeral_seg)
DEFINE_MISSING_FIELD(saved_sweep_ephemeral_start)
#endif // defined(ALL_FIELDS) || defined(BACKGROUND_GC)

// This field is unused
DEFINE_FIELD(generation_table, void*)

// Here is where v5.2 fields starts

#if defined(ALL_FIELDS) || defined(BACKGROUND_GC)
DEFINE_DPTR_FIELD  (freeable_soh_segment,               dac_heap_segment)
DEFINE_DPTR_FIELD  (freeable_uoh_segment,               dac_heap_segment)
#else
DEFINE_MISSING_FIELD(freeable_soh_segment)
DEFINE_MISSING_FIELD(freeable_uoh_segment)
#endif // defined(ALL_FIELDS) || defined(BACKGROUND_GC)

#if defined(ALL_FIELDS) ||  defined(USE_REGIONS)
DEFINE_ARRAY_FIELD (free_regions,                        dac_region_free_list, FREE_REGION_KINDS)
#else
DEFINE_MISSING_FIELD(free_regions)
#endif // ALL_FIELDS
