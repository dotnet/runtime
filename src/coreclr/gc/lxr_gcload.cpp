// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "lxr_gc.h"

#ifdef TARGET_WINDOWS
#define LXR_EXPORT extern "C" __declspec(dllexport)
#else
#define LXR_EXPORT extern "C" __attribute__((visibility("default")))
#endif

namespace
{
    LxrGcHeap* Heap = nullptr;
}

LXR_EXPORT void LOCALGC_CALLCONV GC_VersionInfo(VersionInfo* info)
{
    g_runtimeSupportedVersion = *info;
    g_oldMethodTableFlags = info->MajorVersion < 2;
    info->MajorVersion = GC_INTERFACE_MAJOR_VERSION;
    info->MinorVersion = GC_INTERFACE_MINOR_VERSION;
    info->BuildVersion = 10;
    info->Name = "LXR nursery and mature prototype";
}

LXR_EXPORT HRESULT LOCALGC_CALLCONV GC_Initialize(
    IGCToCLR* runtime, IGCHeap** heap, IGCHandleManager** handles, GcDacVars* dac)
{
    if (runtime == nullptr || heap == nullptr || handles == nullptr || dac == nullptr || Heap != nullptr)
    {
        return E_FAIL;
    }
    g_theGCToCLR = runtime;
    GCConfig::Initialize();
    if (!GCToOSInterface::Initialize())
    {
        GCToEEInterface::LogErrorToHost("LXR OS initialization failed.");
        return E_FAIL;
    }
    IGCHandleManager* manager = CreateGCHandleManager();
    LxrGcHeap* collector = new (std::nothrow) LxrGcHeap;
    if (manager == nullptr || collector == nullptr)
    {
        return E_OUTOFMEMORY;
    }
    // This existing EE category selects single-heap compatibility, not the stock WKS collector.
    g_gc_heap_type = GC_HEAP_WKS;
    g_num_processors = 1;
    g_max_generation = 2;
    g_theGCHeap = collector;
    g_theGCHandleManager = manager;
    Heap = collector;
    *heap = collector;
    *handles = manager;
    // Stock WKS/SVR DAC layouts are not LXR layouts. Version zero fails closed in DAC.
    *dac = {};
    return S_OK;
}

LXR_EXPORT uint64_t GC_LxrPrototype_GetMetric(uint32_t metric)
{
    return Heap == nullptr ? (metric == 0 ? 0 : UINT64_MAX) : Heap->GetPrototypeMetric(metric);
}

LXR_EXPORT const char* GC_LxrPrototype_GetIdentity()
{
    return "LXR nursery and mature prototype; GC interface 6.3; P6 nursery+mature engine; costs-v1; lifecycle-costs-v1; log-publication-v1; full-trace-batch-v1";
}

LXR_EXPORT uintptr_t GC_LxrPrototype_GetOwnedStart()
{
    return Heap == nullptr ? 0 : Heap->GetOwnedStart();
}

LXR_EXPORT size_t GC_LxrPrototype_GetOwnedBytes()
{
    return Heap == nullptr ? 0 : Heap->GetOwnedBytes();
}

LXR_EXPORT uintptr_t GC_LxrPrototype_GetHandleAddress(OBJECTHANDLE handle)
{
    return Heap == nullptr ? 0 : Heap->GetHandleAddress(handle);
}

LXR_EXPORT size_t GC_LxrPrototype_GetAddressExtent(uintptr_t address)
{
    return Heap == nullptr ? 0 : Heap->GetAddressExtent(address);
}

LXR_EXPORT int32_t GC_LxrPrototype_IsOwnedHandle(OBJECTHANDLE handle)
{
    return Heap != nullptr && Heap->IsOwnedHandle(handle) ? 1 : 0;
}

LXR_EXPORT int32_t GC_LxrPrototype_Collect(uint32_t kind)
{
    if (Heap == nullptr)
    {
        return static_cast<int32_t>(LxrRcResult::NotInitialized);
    }
    if (kind >= static_cast<uint32_t>(LxrPauseKind::Count))
    {
        return static_cast<int32_t>(LxrRcResult::InvalidArgument);
    }
    // P26 (F8). This entry point is a synchronous collection for tests, like a user collection: it returns after the lazy work.
    return static_cast<int32_t>(Heap->CollectPrototype(static_cast<LxrPauseKind>(kind), true));
}

LXR_EXPORT int32_t GC_LxrPrototype_HoldTrace(int32_t hold)
{
    return Heap == nullptr ? static_cast<int32_t>(LxrRcResult::NotInitialized) :
        static_cast<int32_t>(Heap->HoldPrototypeTrace(hold != 0));
}

LXR_EXPORT int32_t GC_LxrPrototype_ArmScanCheckpoint(OBJECTHANDLE handle)
{
    return Heap == nullptr ? static_cast<int32_t>(LxrRcResult::NotInitialized) :
        static_cast<int32_t>(Heap->ArmPrototypeScan(handle));
}

LXR_EXPORT int32_t GC_LxrPrototype_ArmLifetimeProbe(OBJECTHANDLE handle)
{
    return Heap == nullptr ? static_cast<int32_t>(LxrRcResult::NotInitialized) :
        static_cast<int32_t>(Heap->ArmPrototypeLifetime(handle));
}

LXR_EXPORT int32_t GC_LxrPrototype_EnableStorageDiagnostics()
{
    return Heap == nullptr ? static_cast<int32_t>(LxrRcResult::NotInitialized) :
        static_cast<int32_t>(Heap->EnablePrototypeStorageDiagnostics());
}

LXR_EXPORT int32_t GC_LxrPrototype_GetStorageRange(uint32_t component, uintptr_t* start, size_t* bytes)
{
    return Heap == nullptr ? static_cast<int32_t>(LxrRcResult::NotInitialized) :
        static_cast<int32_t>(Heap->GetPrototypeStorageRange(component, start, bytes));
}

LXR_EXPORT HRESULT GC_LxrPrototype_WaitForTrace(int32_t timeout)
{
    return Heap == nullptr ? E_FAIL : Heap->WaitUntilConcurrentGCCompleteAsync(timeout);
}

LXR_EXPORT segment_handle GC_LxrPrototype_RegisterFrozenArray(
    void* memory, size_t capacity, OBJECTHANDLE array, OBJECTHANDLE target)
{
    return Heap == nullptr ? nullptr : Heap->RegisterPrototypeFrozenArray(memory, capacity, array, target);
}

LXR_EXPORT void GC_LxrPrototype_UnregisterFrozen(segment_handle segment)
{
    if (Heap != nullptr)
    {
        Heap->UnregisterFrozenSegment(segment);
    }
}
