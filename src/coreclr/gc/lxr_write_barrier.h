// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The LXR write barrier block: two assembly helpers (assign-ref and checked-assign-ref) that the runtime copies and
// installs through IGCHeap::GetAssignRefFunctions (PR #3316 mechanism, "specialized" barrier ABI). The assembly is in
// amd64/lxr_writebarrier.S and amd64/lxr_writebarrier.asm. This header does not depend on the GC headers, so the
// native barrier test can use it too.

#ifndef LXR_WRITE_BARRIER_H
#define LXR_WRITE_BARRIER_H

#include <stddef.h>
#include <stdint.h>

extern "C"
{
    // The helpers. Arguments are in the registers that the runtime barrier helpers use. They are not C calls:
    // they change only the registers that the JIT lets a barrier helper change.
    void LxrAssignRef(void** destination, void* reference);
    void LxrCheckedAssignRef(void** destination, void* reference);

    // Markers in the assembly. The block [LxrCheckedAssignRef, LxrWriteBarrierBlock_End) is relocatable.
    extern uint8_t LxrCheckedAssignRef_End;
    extern uint8_t LxrAssignRef_End;
    extern uint8_t LxrWriteBarrierBlock_End;

    // Places where a hardware fault means a null reference.
    extern uint8_t LxrCheckedAssignRefAVLocation;
    extern uint8_t LxrAssignRefMetadataAVLocation;
    extern uint8_t LxrAssignRefAVLocation;
    extern uint8_t LxrBarrierSlowAVLocation;

    // The patchable slots, 8 bytes each, inside the block.
    extern uint8_t LxrPatchable_MetadataBase;
    extern uint8_t LxrPatchable_HeapLow;
    extern uint8_t LxrPatchable_HeapSize;
    extern uint8_t LxrPatchable_SlowPath;
    extern uint8_t LxrPatchable_XsaveMask;
}

// The values that the slots cache. The runtime copies the current value of each global into its slot (right after it
// copies the block, and every time StompWriteBarrier runs). The layout of a pair is the same as
// GCWriteBarrierPatchableValue (gcinterface.h); lxr_gc.cpp checks it.
struct LxrWriteBarrierGlobals
{
    // Biased base of the FieldUnlogged plane: the metadata byte of address A is base[A >> 6], its bit is (A >> 3) & 7.
    uintptr_t metadata_base;
    // The LXR heap, [heap_low, heap_low + heap_size). The checked helper stores without a check outside of it.
    uintptr_t heap_low;
    uintptr_t heap_size;
    // void LxrBarrierSlowPath(Object** destination). Called with the whole vector state saved.
    uintptr_t slow_path;
    // The xsave components to save around the slow path; 0 means use fxsave.
    uintptr_t xsave_mask;
};

extern "C" LxrWriteBarrierGlobals g_lxr_write_barrier_globals;

struct LxrWriteBarrierPatchable
{
    void* slot;
    void* source;
};

struct LxrWriteBarrierBlockInfo
{
    void* assign_ref;
    size_t assign_ref_size;
    void* checked_assign_ref;
    size_t checked_assign_ref_size;
    // Zero-terminated list of fault addresses.
    uintptr_t* av_locations;
    // Terminated by a null slot.
    LxrWriteBarrierPatchable* patchable;
};

// Describes the block in the image (the original, not the runtime copy).
void LxrWriteBarrierGetBlockInfo(LxrWriteBarrierBlockInfo* info);

// The xsave components that the slow path saves: the state that C++ code (or a library it calls) can change.
// Returns 0 when the OS does not enable xsave; the slow path then uses fxsave.
uintptr_t LxrWriteBarrierComputeXsaveMask();

// Sets the values that the patchable slots cache.
void LxrWriteBarrierSetGlobals(
    uintptr_t metadataBase, uintptr_t heapLow, uintptr_t heapSize, void* slowPath, uintptr_t xsaveMask);

#endif // LXR_WRITE_BARRIER_H
