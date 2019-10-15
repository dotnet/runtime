// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#define NUMBERGENERATIONS               4
#define INITIAL_HANDLE_TABLE_ARRAY_SIZE 10
#define HANDLE_MAX_INTERNAL_TYPES       12

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

// Analogue for the GC generation class, containing information about the start segment
// of a generation and its allocation context.
class dac_generation {
public:
    gc_alloc_context allocation_context;
    DPTR(dac_heap_segment) start_segment;
    uint8_t* allocation_start;
};

// Analogue for the GC CFinalize class, containing information about the finalize queue.
class dac_finalize_queue {
public:
    static const int ExtraSegCount = 2;
    uint8_t** m_FillPointers[NUMBERGENERATIONS + ExtraSegCount];
};

class dac_handle_table {
public:
    // We do try to keep everything that the DAC knows about as close to the
    // start of the struct as possible to avoid having padding members. However,
    // HandleTable has rgTypeFlags at offset 0 for performance reasons and
    // we don't want to disrupt that.
    uint32_t padding[HANDLE_MAX_INTERNAL_TYPES];
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
/* changes in toolbox\sos\strike\strike.cpp.    */
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

// A record of the last OOM that occured in the GC, with some
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
    uint8_t* alloc_allocated;
    DPTR(dac_heap_segment) ephemeral_heap_segment;
    DPTR(dac_finalize_queue) finalize_queue;
    oom_history oom_info;
    size_t interesting_data_per_heap[NUM_GC_DATA_POINTS];
    size_t compact_reasons_per_heap[MAX_COMPACT_REASONS_COUNT];
    size_t expand_mechanisms_per_heap[MAX_EXPAND_MECHANISMS_COUNT];
    size_t interesting_mechanism_bits_per_heap[MAX_GC_MECHANISM_BITS_COUNT];
    uint8_t* internal_root_array;
    size_t internal_root_array_index;
    BOOL heap_analyze_success;

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
// GC's global DAC variabels. When DACCESS_COMPILE is defined (i.e. the DAC build),
// this structure contains __DPtrs for every DAC variable that will marshal values
// from the debugee process to the debugger process when dereferenced.
struct GcDacVars {
  uint8_t major_version_number;
  uint8_t minor_version_number;
  size_t generation_size;
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
