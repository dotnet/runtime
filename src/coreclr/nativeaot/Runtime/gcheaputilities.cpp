// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gcheaputilities.h"
#include "gchandleutilities.h"

#include "gceventstatus.h"
#include "gcinterface.h"

// This is the global GC heap, maintained by the VM.
GPTR_IMPL(IGCHeap, g_pGCHeap);

GVAL_IMPL_INIT(GCHeapType, g_heap_type,     GC_HEAP_INVALID);

IGCHandleManager* g_pGCHandleManager = nullptr;

GcDacVars g_gc_dac_vars;
GPTR_IMPL(GcDacVars, g_gcDacGlobals);

#ifndef DACCESS_COMPILE
static WriteBarrierHelperDescriptor s_writeBarrierHelpers = {};
WriteBarrierFunctions g_writeBarrierFunctions = {};

#ifndef FEATURE_PORTABLE_HELPERS
EXTERN_C uint8_t RhpAssignRefAVLocation;
EXTERN_C uint8_t RhpCheckedAssignRefAVLocation;
#ifdef HOST_X86
#define X86_WRITE_BARRIER_AV_LOCATION(reg) \
    EXTERN_C uint8_t RhpAssignRef##reg##AVLocation; \
    EXTERN_C uint8_t RhpCheckedAssignRef##reg##AVLocation;
X86_WRITE_BARRIER_AV_LOCATION(EAX)
X86_WRITE_BARRIER_AV_LOCATION(ECX)
X86_WRITE_BARRIER_AV_LOCATION(EBX)
X86_WRITE_BARRIER_AV_LOCATION(ESI)
X86_WRITE_BARRIER_AV_LOCATION(EDI)
X86_WRITE_BARRIER_AV_LOCATION(EBP)
#undef X86_WRITE_BARRIER_AV_LOCATION
#endif // HOST_X86

static void InitializeStaticWriteBarrierHelpers()
{
    WriteBarrierHelperDescriptor helpers = {};
    helpers.av_locations[helpers.av_location_count++] =
        reinterpret_cast<uintptr_t>(&RhpAssignRefAVLocation);
#ifdef HOST_X86
#define X86_ADD_WRITE_BARRIER_AV_LOCATION(reg) \
    helpers.av_locations[helpers.av_location_count++] = \
        reinterpret_cast<uintptr_t>(&RhpAssignRef##reg##AVLocation);
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EAX)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(ECX)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EBX)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(ESI)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EDI)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EBP)
#undef X86_ADD_WRITE_BARRIER_AV_LOCATION
#endif // HOST_X86

    helpers.av_locations[helpers.av_location_count++] =
        reinterpret_cast<uintptr_t>(&RhpCheckedAssignRefAVLocation);
#ifdef HOST_X86
#define X86_ADD_WRITE_BARRIER_AV_LOCATION(reg) \
    helpers.av_locations[helpers.av_location_count++] = \
        reinterpret_cast<uintptr_t>(&RhpCheckedAssignRef##reg##AVLocation);
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EAX)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(ECX)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EBX)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(ESI)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EDI)
    X86_ADD_WRITE_BARRIER_AV_LOCATION(EBP)
#undef X86_ADD_WRITE_BARRIER_AV_LOCATION
#endif // HOST_X86

    s_writeBarrierHelpers = helpers;
}
#endif // !FEATURE_PORTABLE_HELPERS

void GCHeapUtilities::InitializeWriteBarrierFunctions(IGCHeap* gcHeap)
{
    gcHeap->GetWriteBarrierFunctions(&g_writeBarrierFunctions);
    assert(g_writeBarrierFunctions.context != nullptr);
    assert(g_writeBarrierFunctions.write_barrier != nullptr);
    assert(g_writeBarrierFunctions.checked_write_barrier != nullptr);
    assert(g_writeBarrierFunctions.is_in_gc_heap != nullptr);
    assert(g_writeBarrierFunctions.bulk_move_with_write_barrier != nullptr);
#ifndef FEATURE_PORTABLE_HELPERS
    InitializeStaticWriteBarrierHelpers();
#endif // !FEATURE_PORTABLE_HELPERS
}

void GCHeapUtilities::SetWriteBarrierHelpers(const WriteBarrierHelperDescriptor& helpers)
{
#ifdef FEATURE_PORTABLE_HELPERS
    s_writeBarrierHelpers = helpers;
#else
    (void)helpers;
#endif // FEATURE_PORTABLE_HELPERS
}

bool GCHeapUtilities::IsIPInWriteBarrierHelper(uintptr_t instructionPointer)
{
    for (size_t i = 0; i < s_writeBarrierHelpers.av_location_count; i++)
    {
        if (s_writeBarrierHelpers.av_locations[i] == instructionPointer)
        {
            return true;
        }
    }

    for (size_t i = 0; i < s_writeBarrierHelpers.exception_range_count; i++)
    {
        WriteBarrierExceptionRange range = s_writeBarrierHelpers.exception_ranges[i];
        if (reinterpret_cast<uintptr_t>(range.start) <= instructionPointer &&
            instructionPointer < reinterpret_cast<uintptr_t>(range.end))
        {
            return true;
        }
    }

    return false;
}

bool IsIPInWriteBarrierHelper(uintptr_t instructionPointer)
{
    return GCHeapUtilities::IsIPInWriteBarrierHelper(instructionPointer);
}
#endif // !DACCESS_COMPILE

// GC entrypoints for the linked-in GC. These symbols are invoked
// directly if we are not using a standalone GC.
extern "C" HRESULT LOCALGC_CALLCONV GC_Initialize(
    /* In  */ IGCToCLR* clrToGC,
    /* Out */ IGCHeap** gcHeap,
    /* Out */ IGCHandleManager** gcHandleManager,
    /* Out */ GcDacVars* gcDacVars
);

#ifndef DACCESS_COMPILE

HRESULT InitializeGCSelector();

HRESULT GCHeapUtilities::InitializeGC()
{
    return InitializeGCSelector();
}

HRESULT InitializeDefaultGC()
{
    return GCHeapUtilities::InitializeDefaultGC();
}

// Initializes a non-standalone GC. The protocol for initializing a non-standalone GC
// is similar to loading a standalone one, except that the GC_VersionInfo and
// GC_Initialize symbols are linked to directory and thus don't need to be loaded.
//
HRESULT GCHeapUtilities::InitializeDefaultGC()
{
    // we should only call this once on startup. Attempting to load a GC
    // twice is an error.
    assert(g_pGCHeap == nullptr);

    IGCHeap* heap;
    IGCHandleManager* manager;
    g_gc_dac_vars.major_version_number = GC_INTERFACE_MAJOR_VERSION;
    g_gc_dac_vars.minor_version_number = GC_INTERFACE_MINOR_VERSION;
    HRESULT initResult = GC_Initialize(nullptr, &heap, &manager, &g_gc_dac_vars);
    if (initResult == S_OK)
    {
        InitializeWriteBarrierFunctions(heap);
        g_pGCHeap = heap;
        g_pGCHandleManager = manager;
        g_gcDacGlobals = &g_gc_dac_vars;
        LOG((LF_GC, LL_INFO100, "GC load successful\n"));
    }
    else
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with HR = 0x%X\n", initResult));
    }

    return initResult;
}

#endif // DACCESS_COMPILE
