// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * gcload.cpp
 *
 * Code for loading and initializing the GC. The code in this file
 * is used in the startup path of both a standalone and non-standalone GC.
 */

#include "common.h"
#include "gcenv.h"
#include "gc.h"

#ifdef BUILD_AS_STANDALONE
#ifndef DLLEXPORT
#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__ ((visibility ("default")))
#endif // _MSC_VER
#endif // DLLEXPORT

#define GC_EXPORT extern "C" DLLEXPORT
#else
#define GC_EXPORT extern "C"
#endif

// These symbols are defined in gc.cpp and populate the GcDacVars
// structure with the addresses of DAC variables within the GC.
namespace WKS
{
    extern void PopulateDacVars(GcDacVars* dacVars);
}

namespace SVR
{
    extern void PopulateDacVars(GcDacVars* dacVars);
}

// This symbol populates GcDacVars with handle table dacvars.
extern void PopulateHandleTableDacVars(GcDacVars* dacVars);

GC_EXPORT
void LOCALGC_CALLCONV
GC_VersionInfo(/* InOut */ VersionInfo* info)
{
#ifdef BUILD_AS_STANDALONE
    // On entry, the info argument contains the interface version that the runtime supports.
    // It is later used to enable backwards compatibility between the GC and the runtime.
    // For example, GC would only call functions on g_theGCToCLR interface that the runtime
    // supports.
    g_runtimeSupportedVersion = *info;
    g_oldMethodTableFlags = g_runtimeSupportedVersion.MajorVersion < 2;
#endif
    info->MajorVersion = GC_INTERFACE_MAJOR_VERSION;
    info->MinorVersion = GC_INTERFACE_MINOR_VERSION;
    info->BuildVersion = 0;
    info->Name = "CoreCLR GC";
}

GC_EXPORT
HRESULT LOCALGC_CALLCONV
GC_Initialize(
    /* In  */ IGCToCLR* clrToGC,
    /* Out */ IGCHeap** gcHeap,
    /* Out */ IGCHandleManager** gcHandleManager,
    /* Out */ GcDacVars* gcDacVars
)
{
    IGCHeapInternal* heap;

    assert(gcDacVars != nullptr);
    assert(gcHeap != nullptr);
    assert(gcHandleManager != nullptr);

#ifdef BUILD_AS_STANDALONE
    assert(clrToGC != nullptr);
    g_theGCToCLR = clrToGC;
#else
    UNREFERENCED_PARAMETER(clrToGC);
    assert(clrToGC == nullptr);
#endif

#ifndef FEATURE_NATIVEAOT

    // For NativeAOT, GCConfig and GCToOSInterface are initialized in PalInit

    // Initialize GCConfig before anything else - initialization of our
    // various components may want to query the current configuration.
    GCConfig::Initialize();

#if defined(TRACE_GC) && defined(SIMPLE_DPRINTF)
    HRESULT hr = initialize_log_file();
    if (hr != S_OK)
    {
        return hr;
    }
#endif //TRACE_GC && SIMPLE_DPRINTF

    if (!GCToOSInterface::Initialize())
    {
        GCToEEInterface::LogErrorToHost("Failed to initialize GCToOSInterface");
        return E_FAIL;
    }
#endif

    IGCHandleManager* handleManager = CreateGCHandleManager();
    if (handleManager == nullptr)
    {
        return E_OUTOFMEMORY;
    }

#ifdef FEATURE_SVR_GC
    if (GCConfig::GetServerGC() && GCToEEInterface::GetCurrentProcessCpuCount() > 1)
    {
#ifdef WRITE_BARRIER_CHECK
        g_GCShadow = 0;
        g_GCShadowEnd = 0;
#endif // WRITE_BARRIER_CHECK

        g_gc_heap_type = GC_HEAP_SVR;
        heap = SVR::CreateGCHeap();
        SVR::PopulateDacVars(gcDacVars);
    }
    else
#endif
    {
        g_gc_heap_type = GC_HEAP_WKS;
        heap = WKS::CreateGCHeap();
        WKS::PopulateDacVars(gcDacVars);
    }

    PopulateHandleTableDacVars(gcDacVars);
    if (heap == nullptr)
    {
        return E_OUTOFMEMORY;
    }

    g_theGCHeap = heap;
    *gcHandleManager = handleManager;
    *gcHeap = heap;
    return S_OK;
}
