// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_ALLOCATION_NOTIFICATION_TEST

#include "gcinternal.h"
#include "allocationnotificationtest.h"
#ifdef FEATURE_LXR_RC_VERTICAL
#include "lxrrcverticaltest.h"
#endif

#ifndef DLLEXPORT
#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#endif

namespace
{
constexpr int32_t MaxRecords = 1 << 18;

struct AllocationNotificationTestRecord
{
    Object* object;
    size_t alignedSize;
    AllocationCompleteFlags flags;
};

AllocationNotificationTestRecord g_records[MaxRecords];
int32_t g_recordCount;
int32_t g_errorCount;
int32_t g_countOnly;
void* g_methodTableFilter;
size_t g_alignedSizeFilter;
int g_context;

void LOCALGC_CALLCONV AllocationNotificationTestCallback(
    Object* object,
    size_t alignedObjectSize,
    AllocationCompleteFlags flags,
    void* context)
{
    if ((object == nullptr) ||
        (alignedObjectSize == 0) ||
        ((alignedObjectSize & (sizeof(void*) - 1)) != 0) ||
        (context != &g_context))
    {
        Interlocked::Increment(&g_errorCount);
        return;
    }

#ifdef FEATURE_LXR_RC_VERTICAL
    LxrRcVerticalObserveAllocation(object, alignedObjectSize, flags);
#endif
    void* methodTable = *reinterpret_cast<void**>(object);
    if ((g_methodTableFilter != nullptr) && (methodTable != g_methodTableFilter))
    {
        return;
    }
    if ((g_alignedSizeFilter != 0) && (alignedObjectSize != g_alignedSizeFilter))
    {
        return;
    }

    int32_t index = Interlocked::Increment(&g_recordCount) - 1;
    if ((index < 0) || (index >= MaxRecords))
    {
        Interlocked::Increment(&g_errorCount);
        return;
    }

    g_records[index].object = object;
    g_records[index].alignedSize = alignedObjectSize;
    g_records[index].flags = flags;
}

void LOCALGC_CALLCONV AllocationNotificationTestNoOp(
    Object* object,
    size_t alignedObjectSize,
    AllocationCompleteFlags flags,
    void* context)
{
    UNREFERENCED_PARAMETER(object);
    UNREFERENCED_PARAMETER(alignedObjectSize);
    UNREFERENCED_PARAMETER(flags);
    UNREFERENCED_PARAMETER(context);
}

void LOCALGC_CALLCONV AllocationNotificationTestCountOnly(
    Object* object,
    size_t alignedObjectSize,
    AllocationCompleteFlags flags,
    void* context)
{
    UNREFERENCED_PARAMETER(object);
    UNREFERENCED_PARAMETER(alignedObjectSize);
    UNREFERENCED_PARAMETER(flags);
    UNREFERENCED_PARAMETER(context);
    Interlocked::Increment(&g_countOnly);
}
}

AllocationCompleteCallback GetAllocationNotificationTestCallback()
{
    if (GCConfig::GetAllocationNotificationTestUncounted())
    {
        return AllocationNotificationTestNoOp;
    }
    if (GCConfig::GetAllocationNotificationTestCountOnly())
    {
        return AllocationNotificationTestCountOnly;
    }
    return AllocationNotificationTestCallback;
}

void* GetAllocationNotificationTestContext()
{
    return &g_context;
}

extern "C" DLLEXPORT void GC_AllocationNotificationTest_Reset(
    void* methodTableFilter,
    size_t alignedSizeFilter)
{
    g_methodTableFilter = methodTableFilter;
    g_alignedSizeFilter = alignedSizeFilter;
    VolatileStore(&g_errorCount, static_cast<int32_t>(0));
    VolatileStore(&g_recordCount, static_cast<int32_t>(0));
    VolatileStore(&g_countOnly, static_cast<int32_t>(0));
}

extern "C" DLLEXPORT int64_t GC_AllocationNotificationTest_GetCount()
{
    int32_t count = VolatileLoad(&g_recordCount);
    if (count < 0)
    {
        return 0;
    }
    return min(count, MaxRecords);
}

extern "C" DLLEXPORT int64_t GC_AllocationNotificationTest_GetCountOnly()
{
    int32_t count = VolatileLoad(&g_countOnly);
    return count < 0 ? 0 : count;
}

extern "C" DLLEXPORT int64_t GC_AllocationNotificationTest_GetErrorCount()
{
    return VolatileLoad(&g_errorCount);
}

extern "C" DLLEXPORT void* GC_AllocationNotificationTest_GetObject(int64_t index)
{
    if ((index < 0) || (index >= VolatileLoad(&g_recordCount)) || (index >= MaxRecords))
    {
        return nullptr;
    }

    return g_records[index].object;
}

extern "C" DLLEXPORT size_t GC_AllocationNotificationTest_GetSize(int64_t index)
{
    if ((index < 0) || (index >= VolatileLoad(&g_recordCount)) || (index >= MaxRecords))
    {
        return 0;
    }

    return g_records[index].alignedSize;
}

extern "C" DLLEXPORT uint32_t GC_AllocationNotificationTest_GetFlags(int64_t index)
{
    if ((index < 0) || (index >= VolatileLoad(&g_recordCount)) || (index >= MaxRecords))
    {
        return 0;
    }

    return static_cast<uint32_t>(g_records[index].flags);
}

#endif // FEATURE_ALLOCATION_NOTIFICATION_TEST
