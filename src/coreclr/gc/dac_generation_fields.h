DEFINE_FIELD      (allocation_context, gc_alloc_context)
DEFINE_DPTR_FIELD (start_segment,      dac_heap_segment)
#ifndef USE_REGIONS
DEFINE_FIELD      (allocation_start,   uint8_t*)
#else
DEFINE_MISSING_FIELD(allocation_start)
#endif