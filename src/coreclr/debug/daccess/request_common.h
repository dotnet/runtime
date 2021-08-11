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
HeapTableIndex(DPTR(opaque_gc_heap**) heaps, size_t index)
{
    DPTR(opaque_gc_heap*) heap_table = Dereference(heaps);
    DPTR(opaque_gc_heap*) ptr = TableIndex(heap_table, index, sizeof(void*));
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
// field_index++
#define LOAD_BASE(field_name, field_type)                                                    \
    DPTR(int) p_##field_name##_offset = TableIndex(field_offsets, field_index, sizeof(int)); \
    int field_name##_offset = *p_##field_name##_offset;                                      \
    DPTR(field_type) p_##field_name = heap + field_name##_offset;                            \
    field_index++;

// if (field_offset != -1)
//    result.field = *p_field
#define LOAD(field_name, field_type)                                                         \
    LOAD_BASE(field_name, field_type)                                                        \
    if (field_name##_offset != -1)                                                           \
    {                                                                                        \
        field_type field_name = *p_##field_name;                                             \
        result.field_name = field_name;                                                      \
    }

// if (field_offset != -1)
//    result.field = DPTR(field_type)field_name
#define LOAD_DPTR(field_name, field_type)                                                    \
    LOAD_BASE(field_name, field_type*)                                                       \
    if (field_name##_offset != -1)                                                           \
    {                                                                                        \
        field_type* field_name = *p_##field_name;                                            \
        result.field_name = DPTR(field_type)((TADDR)field_name);                             \
    }

// if (field_offset != -1)
//    for i from 0 to array_length - 1
//        result.field[i] = *p_field
//        p_field = p_field + 1
#define LOAD_ARRAY(field_name, field_type, array_length)                                     \
    LOAD_BASE(field_name, field_type)                                                        \
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
    int field_index = GENERATION_TABLE_FIELD_INDEX;
    LOAD_BASE (generation_table, dac_generation);
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
    int field_index = 0;

#define ALL_FIELDS
#define DEFINE_FIELD(field_name, field_type) LOAD(field_name, field_type)
#define DEFINE_DPTR_FIELD(field_name, field_type) LOAD_DPTR(field_name, field_type)
#define DEFINE_ARRAY_FIELD(field_name, field_type, array_length) LOAD_ARRAY(field_name, field_type, array_length);

#include "../../gc/gc_typefields.h"

#undef DEFINE_ARRAY_FIELD
#undef DEFINE_DPTR_FIELD
#undef DEFINE_FIELD
#undef ALL_FIELDS

    return result;
}

#undef LOAD_ARRAY
#undef LOAD_DPTR
#undef LOAD
#undef LOAD_BASE

#endif // _REQUEST_COMMON_H_
