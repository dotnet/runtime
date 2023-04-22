// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _GC_INTERFACE_DAC_H_
#define _GC_INTERFACE_DAC_H_

// This file defines the interface between the GC and the DAC. The interface consists of two things:
//   1. A number of variables ("DAC vars") whose addresses are exposed to the DAC (see "struct GcDacVars")
//   2. A number of types that are analogues to GC-internal types. These types expose a subset of the
//      GC-internal type's fields, while still maintaining the same layout.
// This interface is strictly versioned, see gcinterface.dacvars.def for more information.

#define HEAP_SEGMENT_FLAGS_READONLY     1
#define NUM_GC_DATA_POINTS              9
#define MAX_COMPACT_REASONS_COUNT       11
#define MAX_EXPAND_MECHANISMS_COUNT     6
#define MAX_GC_MECHANISM_BITS_COUNT     2
#define MAX_GLOBAL_GC_MECHANISMS_COUNT  6
#define FREE_REGION_KINDS               3

// The number of generations is hardcoded in to the dac APIS (DacpGcHeapDetails hard codes the size of its arrays)
// The number of generations is hardcoded into some older dac APIS (for example DacpGcHeapDetails hard codes the size of its arrays)
// This value cannot change and should not be used in new DAC APIs. New APIs can query GcDacVars.total_generation_count
// variable which is dynamically initialized at runtime

#define NUMBERGENERATIONS               4


#include "handletableconstants.h"

// Analogue for the GC heap_segment class, containing information regarding a single
// heap segment.
class dac_heap_segment {
public:
    uint8_t* allocated;
    uint8_t* committed;
    uint8_t* reserved;
    uint8_t* used;
    uint8_t* mem;
    size_t flags;
    DPTR(dac_heap_segment) next;
    uint8_t* background_allocated;
    class dac_gc_heap* heap;
};

class dac_region_free_list {
public:
    size_t  num_free_regions;
    size_t  size_free_regions;
    size_t  size_committed_in_free_regions;
    size_t  num_free_regions_added;
    size_t  num_free_regions_removed;
    DPTR(dac_heap_segment) head_free_region;
    DPTR(dac_heap_segment) tail_free_region;
};

// Analogue for the GC generation class, containing information about the start segment
// of a generation and its allocation context.
class dac_generation {
public:
#define ALL_FIELDS
#define DEFINE_FIELD(field_name, field_type) field_type field_name;
#define DEFINE_DPTR_FIELD(field_name, field_type) DPTR(field_type) field_name;
#define DEFINE_MISSING_FIELD(field_name)

#include "dac_generation_fields.h"

#undef DEFINE_DPTR_FIELD
#undef DEFINE_FIELD
#undef ALL_FIELDS
#undef DEFINE_MISSING_FIELD
};

// Analogue for the GC CFinalize class, containing information about the finalize queue.
class dac_finalize_queue {
public:
    static const int ExtraSegCount = 2;
    uint8_t** m_FillPointers[NUMBERGENERATIONS + ExtraSegCount];
};

class dac_handle_table_segment {
public:
    uint8_t rgGeneration[HANDLE_BLOCKS_PER_SEGMENT * sizeof(uint32_t) / sizeof(uint8_t)];
    uint8_t rgAllocation[HANDLE_BLOCKS_PER_SEGMENT];
    uint32_t rgFreeMask[HANDLE_MASKS_PER_SEGMENT];
    uint8_t rgBlockType[HANDLE_BLOCKS_PER_SEGMENT];
    uint8_t rgUserData[HANDLE_BLOCKS_PER_SEGMENT];
    uint8_t rgLocks[HANDLE_BLOCKS_PER_SEGMENT];
    uint8_t rgTail[HANDLE_MAX_INTERNAL_TYPES];
    uint8_t rgHint[HANDLE_MAX_INTERNAL_TYPES];
    uint32_t rgFreeCount[HANDLE_MAX_INTERNAL_TYPES];
    DPTR(dac_handle_table_segment) pNextSegment;
 };


class dac_handle_table {
public:
    // We do try to keep everything that the DAC knows about as close to the
    // start of the struct as possible to avoid having padding members. However,
    // HandleTable has rgTypeFlags at offset 0 for performance reasons and
    // we don't want to disrupt that.
    uint32_t padding[HANDLE_MAX_INTERNAL_TYPES];
    DPTR(dac_handle_table_segment) pSegmentList;
};

class dac_handle_table_bucket {
public:
    DPTR(DPTR(dac_handle_table)) pTable;
    uint32_t HandleTableIndex;
};

class dac_handle_table_map {
public:
    DPTR(DPTR(dac_handle_table_bucket)) pBuckets;
    DPTR(dac_handle_table_map) pNext;
    uint32_t dwMaxIndex;
};

class dac_card_table_info {
public:
    unsigned    recount;
    size_t      size;
    TADDR       next_card_table;
};

// Possible values of the current_c_gc_state dacvar, indicating the state of
// a background GC.
enum c_gc_state
{
    c_gc_state_marking,
    c_gc_state_planning,
    c_gc_state_free
};

// Reasons why an OOM might occur, recorded in the oom_history
// struct below.
enum oom_reason
{
    oom_no_failure = 0,
    oom_budget = 1,
    oom_cant_commit = 2,
    oom_cant_reserve = 3,
    oom_loh = 4,
    oom_low_mem = 5,
    oom_unproductive_full_gc = 6
};

/*!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/
/* If you modify failure_get_memory and         */
/* oom_reason be sure to make the corresponding */
/* changes in ClrMD.                            */
/*!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/
enum failure_get_memory
{
    fgm_no_failure = 0,
    fgm_reserve_segment = 1,
    fgm_commit_segment_beg = 2,
    fgm_commit_eph_segment = 3,
    fgm_grow_table = 4,
    fgm_commit_table = 5
};

// A record of the last OOM that occurred in the GC, with some
// additional information as to what triggered the OOM.
struct oom_history
{
    oom_reason reason;
    size_t alloc_size;
    uint8_t* reserved;
    uint8_t* allocated;
    size_t gc_index;
    failure_get_memory fgm;
    size_t size;
    size_t available_pagefile_mb;
    BOOL loh_p;
};

// Analogue for the GC gc_heap class, containing information regarding a single
// GC heap (of which there are multiple, with server GC).
class dac_gc_heap {
public:
#define ALL_FIELDS
#define DEFINE_FIELD(field_name, field_type) field_type field_name;
#define DEFINE_DPTR_FIELD(field_name, field_type) DPTR(field_type) field_name;
#define DEFINE_ARRAY_FIELD(field_name, field_type, array_length) field_type field_name[array_length];

#include "dac_gcheap_fields.h"

#undef DEFINE_ARRAY_FIELD
#undef DEFINE_DPTR_FIELD
#undef DEFINE_FIELD
#undef ALL_FIELDS

    // The generation table must always be last, because the size of this array
    // (stored inline in the gc_heap class) can vary.
    //
    // The size of the generation class is not part of the GC-DAC interface,
    // despite being embedded by-value into the gc_heap class. The DAC variable
    // "generation_size" stores the size of the generation class, so the DAC can
    // use it and pointer arithmetic to calculate correct offsets into the generation
    // table. (See "GenerationTableIndex" function in the DAC for details)
    //
    // Also note that this array has length 1 because the C++ standard doesn't allow
    // for 0-length arrays, although every major compiler is willing to tolerate it.
    dac_generation generation_table[1];
};

#define GENERATION_TABLE_FIELD_INDEX 21

// Unlike other DACized structures, these types are loaded manually in the debugger.
// To avoid misuse, pointers to them are explicitly casted to these unused type.
struct unused_gc_heap
{
    uint8_t unused;
};

struct unused_generation
{
    uint8_t unused;
};

// The DAC links against six symbols that build as part of the VM DACCESS_COMPILE
// build. These symbols are considered to be GC-private functions, but the DAC needs
// to use them in order to perform some handle-related functions. These six functions
// are adorned by this macro to make clear that their implementations must be versioned
// alongside the rest of this file.
//
// Practically, this macro ensures that the target symbols aren't mangled, since the
// DAC calls them with a signature slightly different than the one used when they
// were defined.
#define GC_DAC_VISIBLE

#ifdef DACCESS_COMPILE
#define GC_DAC_VISIBLE_NO_MANGLE extern "C"
#else
#define GC_DAC_VISIBLE_NO_MANGLE
#endif // DACCESS_COMPILE

// The actual structure containing the DAC variables. When DACCESS_COMPILE is not
// defined (i.e. the normal runtime build), this structure contains pointers to the
// GC's global DAC variables. When DACCESS_COMPILE is defined (i.e. the DAC build),
// this structure contains __DPtrs for every DAC variable that will marshal values
// from the debugee process to the debugger process when dereferenced.
struct GcDacVars {
  uint8_t major_version_number;
  uint8_t minor_version_number;
  size_t generation_size;
  size_t total_generation_count;
  int total_bookkeeping_elements;
  int count_free_region_kinds;
  size_t card_table_info_size;
#ifdef DACCESS_COMPILE
 #define GC_DAC_VAR(type, name)       DPTR(type) name;
 #define GC_DAC_PTR_VAR(type, name)   DPTR(type*) name;
 #define GC_DAC_ARRAY_VAR(type, name) DPTR(type) name;
#else
 #define GC_DAC_VAR(type, name) type *name;
#endif
#include "gcinterface.dacvars.def"
};

#endif // _GC_INTERFACE_DAC_H_
