// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_OBJECT_REFERENCE_ENUMERATION_TEST

#include "gcinternal.h"

#ifndef DLLEXPORT
#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#endif

namespace
{
constexpr int32_t MaxScanThreads = 1024;

struct LocalScan
{
    int64_t ranges;
    int64_t slots;
    int64_t nonNullSlots;
    uint64_t checksum;
};

struct ThreadScan
{
    int32_t active;
    int32_t epoch;
    LocalScan scan;
    int64_t objectScans;
    uint8_t padding[16];
};
static_assert(sizeof(ThreadScan) == 64);

int32_t g_snapshotMode;
int32_t g_epoch;
int32_t g_errors;
int32_t g_threadCount;
alignas(64) ThreadScan g_threadScans[MaxScanThreads];
PLATFORM_THREAD_LOCAL int32_t t_scanIndex = -1;
PLATFORM_THREAD_LOCAL int32_t t_scanEpoch;

NOINLINE void ObjectReferenceEnumerationTestVisitSlot(
    Object** slot,
    LocalScan* scan)
{
    scan->slots++;
    Object* value = *slot;
    if (value != nullptr)
    {
        scan->nonNullSlots++;
    }

    scan->checksum += static_cast<uint64_t>(
        (reinterpret_cast<uintptr_t>(slot) >> 3) ^
        (reinterpret_cast<uintptr_t>(value) >> 3));
}

void ScanWithCallback(Object* object, LocalScan* scan)
{
    MethodTable* methodTable = object->GetGCSafeMethodTable();
    if (!methodTable->ContainsGCPointers())
    {
        return;
    }

    GCReferenceObjectLayout layout =
        GetGCReferenceObjectLayout(object, methodTable);
    CGCDesc* map = CGCDesc::GetCGCDescFromMT(methodTable);
    CGCDescSeries* current = map->GetHighestSeries();
    ptrdiff_t seriesCount = static_cast<ptrdiff_t>(map->GetNumSeries());

    if (seriesCount > 0)
    {
        CGCDescSeries* last = map->GetLowestSeries();
        while (true)
        {
            Object** slot = reinterpret_cast<Object**>(
                reinterpret_cast<uint8_t*>(object) +
                current->GetSeriesOffset());
            size_t count =
                (current->GetSeriesSize() + layout.objectSize) /
                sizeof(Object*);
            if (count != 0)
            {
                scan->ranges++;
                for (size_t index = 0; index < count; index++)
                {
                    ObjectReferenceEnumerationTestVisitSlot(slot + index, scan);
                }
            }

            if (current == last)
            {
                return;
            }
            current--;
        }
    }

    Object** slot = reinterpret_cast<Object**>(
        reinterpret_cast<uint8_t*>(object) +
        current->GetSeriesOffset());
    for (uint32_t component = 0;
         component < layout.componentCount;
         component++)
    {
        for (ptrdiff_t seriesIndex = 0;
             seriesIndex > seriesCount;
             seriesIndex--)
        {
            val_serie_item* item = current->val_serie + seriesIndex;
            scan->ranges++;
            for (size_t index = 0; index < item->nptrs; index++)
            {
                ObjectReferenceEnumerationTestVisitSlot(slot + index, scan);
            }
            slot = reinterpret_cast<Object**>(
                reinterpret_cast<uint8_t*>(slot + item->nptrs) +
                item->skip);
        }
    }
}

void ScanWithVisitor(Object* object, LocalScan* scan)
{
    GCReferenceRanges::Enumerate(
        object,
        [scan](Object** start, size_t count)
        {
            scan->ranges++;
            for (size_t index = 0; index < count; index++)
            {
                ObjectReferenceEnumerationTestVisitSlot(start + index, scan);
            }
            return true;
        });
}

void ScanWithCursor(Object* object, LocalScan* scan)
{
    GCReferenceRangeIterator iterator(object);
    GCReferenceRange range = {};
    while (iterator.Next(&range))
    {
        scan->ranges++;
        for (size_t index = 0; index < range.count; index++)
        {
            ObjectReferenceEnumerationTestVisitSlot(range.start + index, scan);
        }
    }
}

ThreadScan* GetThreadScan()
{
    int32_t epoch = VolatileLoad(&g_epoch);
    if ((t_scanIndex >= 0) && (t_scanEpoch == epoch))
    {
        return &g_threadScans[t_scanIndex];
    }

    int32_t index = Interlocked::Increment(&g_threadCount) - 1;
    if (index >= MaxScanThreads)
    {
        Interlocked::Increment(&g_errors);
        return nullptr;
    }

    ThreadScan* threadScan = &g_threadScans[index];
    threadScan->scan = {};
    threadScan->objectScans = 0;
    threadScan->epoch = epoch;
    VolatileStore(&threadScan->active, static_cast<int32_t>(0));
    t_scanIndex = index;
    t_scanEpoch = epoch;
    return threadScan;
}
}

int32_t g_object_reference_enumeration_test_mode;

void ObjectReferenceEnumerationTestScan(Object* object)
{
    ObjectReferenceEnumerationTestMode mode =
        static_cast<ObjectReferenceEnumerationTestMode>(
            VolatileLoad(&g_object_reference_enumeration_test_mode));
    if (mode == ObjectReferenceEnumerationTestMode::Disabled)
    {
        return;
    }

    ThreadScan* threadScan = GetThreadScan();
    if (threadScan == nullptr)
    {
        return;
    }

    VolatileStore(&threadScan->active, static_cast<int32_t>(1));
    mode = static_cast<ObjectReferenceEnumerationTestMode>(
        VolatileLoad(&g_object_reference_enumeration_test_mode));
    if (mode == ObjectReferenceEnumerationTestMode::Disabled)
    {
        VolatileStore(&threadScan->active, static_cast<int32_t>(0));
        return;
    }

    LocalScan scan = {};
    switch (mode)
    {
        case ObjectReferenceEnumerationTestMode::Callback:
            ScanWithCallback(object, &scan);
            break;
        case ObjectReferenceEnumerationTestMode::Visitor:
            ScanWithVisitor(object, &scan);
            break;
        case ObjectReferenceEnumerationTestMode::Cursor:
            ScanWithCursor(object, &scan);
            break;
        default:
            Interlocked::Increment(&g_errors);
            VolatileStore(&threadScan->active, static_cast<int32_t>(0));
            return;
    }

    threadScan->objectScans++;
    threadScan->scan.ranges += scan.ranges;
    threadScan->scan.slots += scan.slots;
    threadScan->scan.nonNullSlots += scan.nonNullSlots;
    threadScan->scan.checksum += scan.checksum;
    VolatileStore(&threadScan->active, static_cast<int32_t>(0));
}

extern "C" DLLEXPORT void GC_ObjectReferenceEnumerationTest_Reset()
{
    Interlocked::Exchange(
        &g_object_reference_enumeration_test_mode,
        static_cast<int32_t>(0));
    Interlocked::Increment(&g_epoch);
    VolatileStore(&g_threadCount, static_cast<int32_t>(0));
    VolatileStore(&g_errors, static_cast<int32_t>(0));

    int64_t requestedMode = GCConfig::GetObjectReferenceEnumerationTestMode();
    VolatileStore(&g_snapshotMode, static_cast<int32_t>(requestedMode));
    if ((requestedMode < static_cast<int64_t>(
             ObjectReferenceEnumerationTestMode::Callback)) ||
        (requestedMode > static_cast<int64_t>(
             ObjectReferenceEnumerationTestMode::Cursor)))
    {
        Interlocked::Increment(&g_errors);
        return;
    }

    Interlocked::Exchange(
        &g_object_reference_enumeration_test_mode,
        static_cast<int32_t>(requestedMode));
}

extern "C" DLLEXPORT void GC_ObjectReferenceEnumerationTest_Stop()
{
    Interlocked::Exchange(
        &g_object_reference_enumeration_test_mode,
        static_cast<int32_t>(0));
    int32_t threadCount = min(
        VolatileLoad(&g_threadCount),
        MaxScanThreads);
    for (int32_t index = 0; index < threadCount; index++)
    {
        while (VolatileLoad(&g_threadScans[index].active) != 0)
        {
            YieldProcessor();
        }
    }
}

extern "C" DLLEXPORT void GC_ObjectReferenceEnumerationTest_GetSnapshot(
    ObjectReferenceEnumerationTestSnapshot* snapshot)
{
    if (snapshot == nullptr)
    {
        Interlocked::Increment(&g_errors);
        return;
    }

    snapshot->mode = VolatileLoad(&g_snapshotMode);
    snapshot->errors = VolatileLoad(&g_errors);
    snapshot->objectScans = 0;
    snapshot->ranges = 0;
    snapshot->slots = 0;
    snapshot->nonNullSlots = 0;
    snapshot->checksum = 0;

    int32_t threadCount = min(
        VolatileLoad(&g_threadCount),
        MaxScanThreads);
    for (int32_t index = 0; index < threadCount; index++)
    {
        const ThreadScan& threadScan = g_threadScans[index];
        snapshot->objectScans += threadScan.objectScans;
        snapshot->ranges += threadScan.scan.ranges;
        snapshot->slots += threadScan.scan.slots;
        snapshot->nonNullSlots += threadScan.scan.nonNullSlots;
        snapshot->checksum += threadScan.scan.checksum;
    }
}

#endif // FEATURE_OBJECT_REFERENCE_ENUMERATION_TEST
