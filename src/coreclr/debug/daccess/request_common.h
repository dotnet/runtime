// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains functions used by both request.cpp and request_svr.cpp
// to communicate with the debuggee's GC.

#ifndef _REQUEST_COMMON_H_
#define _REQUEST_COMMON_H_

// Indexes into an array of elements of type T, where the size of type
// T is not (or may not be) known at compile-time.
// Returns a DPTR to the requested element (the element at the given index).
template<typename T>
DPTR(T) TableIndex(DPTR(T) base, size_t index, size_t t_size)
{
    TADDR base_addr = base.GetAddr();
    TADDR element_addr = DacTAddrOffset(base_addr, index, t_size);
    return __DPtr<T>(element_addr);
}

// Dereferences a DPTR(T*), yielding a DPTR(T).
template<typename T>
DPTR(T) Dereference(DPTR(T*) ptr)
{
    TADDR ptr_base = (TADDR)*ptr;
    return __DPtr<T>(ptr_base);
}

// Indexes into the global heap table, returning a TADDR to the requested
// heap instance.
inline TADDR
HeapTableIndex(DPTR(dac_gc_heap**) heaps, size_t index)
{
    DPTR(dac_gc_heap*) heap_table = Dereference(heaps);
    DPTR(dac_gc_heap*) ptr = TableIndex(heap_table, index, sizeof(dac_gc_heap*));
    return Dereference(ptr).GetAddr();
}

// Indexes into a given generation table, returning a DPTR to the
// requested element (the element at the given index) of the table.
inline DPTR(dac_generation)
GenerationTableIndex(DPTR(dac_generation) base, size_t index)
{
    return TableIndex(base, index, g_gcDacGlobals->generation_size);
}

// Starting .NET 6, it is possible for the gc_heap object to have different layouts 
// for coreclr.dll and clrgc.dll. Instead of using assuming dac_gc_heap and gc_heap
// have identical layout, we have the gc exported an array of field offsets instead.
// These offsets could be -1, indicating the field does not exist in the current
// gc_heap being used.

// field_offset = g_gcDacGlobals->gc_heap_field_offsets
// p_field_offset = field_offset[field_index]
// p_field = heap + p_field_offset 
#define LOAD_BASE(field_name, field_index, field_type)                                       \
    DPTR(int) p_##field_name##_offset = TableIndex(field_offsets, field_index, sizeof(int)); \
    int field_name##_offset = *p_##field_name##_offset;                                      \
    DPTR(field_type) p_##field_name = heap + field_name##_offset;

// if (field_offset != -1)
//    result.field = *p_field
#define LOAD(field_name, field_index, field_type)                                            \
    LOAD_BASE(field_name, field_index, field_type)                                           \
    if (field_name##_offset != -1)                                                           \
    {                                                                                        \
        field_type field_name = *p_##field_name;                                             \
        result.field_name = field_name;                                                      \
    }

// if (field_offset != -1)
//    result.field = DPTR(field_type)field_name
#define LOAD_DPTR(field_name, field_index, field_type)                                       \
    LOAD_BASE(field_name, field_index, field_type*)                                          \
    if (field_name##_offset != -1)                                                           \
    {                                                                                        \
        field_type* field_name = *p_##field_name;                                            \
        result.field_name = DPTR(field_type)((TADDR)field_name);                             \
    }

// if (field_offset != -1)
//    for i from 0 to array_length - 1
//        result.field[i] = *p_field
//        p_field = p_field + 1
#define LOAD_ARRAY(field_name, field_index, field_type, array_length)                        \
    LOAD_BASE(field_name, field_index, field_type)                                           \
    if (field_name##_offset != -1)                                                           \
    {                                                                                        \
        for (int i = 0; i < array_length; i++)                                               \
        {                                                                                    \
            result.field_name[i] = *p_##field_name;                                          \
            p_##field_name = p_##field_name + 1;                                             \
        }                                                                                    \
    }

// Indexes into a heap's generation table, given the heap instance
// and the desired index. Returns a DPTR to the requested element.
inline DPTR(dac_generation)
ServerGenerationTableIndex(TADDR heap, size_t index)
{
    DPTR(int) field_offsets = g_gcDacGlobals->gc_heap_field_offsets;
    LOAD_BASE (generation_table, 18, dac_generation);
    assert (generation_table_offset != -1);
    return TableIndex(p_generation_table, index, g_gcDacGlobals->generation_size);
}

// Load an instance of dac_gc_heap for the heap pointed by heap.
// Fields that does not exist in the current gc_heap instance is zero initialized.
// Return the dac_gc_heap object.
inline dac_gc_heap
LoadGcHeapData(TADDR heap)
{
    dac_gc_heap result;
    memset(&result, 0, sizeof(dac_gc_heap));

    DPTR(int) field_offsets = g_gcDacGlobals->gc_heap_field_offsets;

    LOAD      (alloc_allocated,                    0,  uint8_t*);
    LOAD_DPTR (ephemeral_heap_segment,             1,  dac_heap_segment);
    LOAD_DPTR (finalize_queue,                     2,  dac_finalize_queue);
    LOAD      (oom_info,                           3,  oom_history);
    LOAD_ARRAY(interesting_data_per_heap,          4,  size_t, NUM_GC_DATA_POINTS);
    LOAD_ARRAY(compact_reasons_per_heap,           5,  size_t, MAX_COMPACT_REASONS_COUNT);
    LOAD_ARRAY(expand_mechanisms_per_heap,         6,  size_t, MAX_EXPAND_MECHANISMS_COUNT);
    LOAD_ARRAY(interesting_mechanism_bits_per_heap,7,  size_t, MAX_GC_MECHANISM_BITS_COUNT);
    LOAD      (internal_root_array,                8,  uint8_t*);
    LOAD      (internal_root_array_index,          9,  size_t);
    LOAD      (heap_analyze_success,               10, BOOL);
    LOAD      (card_table,                         11, uint32_t*);
    LOAD      (mark_array,                         12, uint32_t*);
    LOAD      (next_sweep_obj,                     13, uint8_t*);    
    LOAD      (background_saved_lowest_address,    14, uint8_t*);
    LOAD      (background_saved_highest_address,   15, uint8_t*);
    LOAD_DPTR (saved_sweep_ephemeral_seg,          16, dac_heap_segment);
    LOAD      (saved_sweep_ephemeral_start,        17, uint8_t*);

    return result;
}

#undef LOAD_ARRAY
#undef LOAD_DPTR
#undef LOAD
#undef LOAD_BASE

#endif // _REQUEST_COMMON_H_
