// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gc.h"
#include "env/gcenv.ee.h"
#include "gcref.h"
#include "lxr_reference_counting.h"
#include "lxrrcverticaltest.h"

#include <atomic>
#include <new>

#ifndef DLLEXPORT
#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#endif

namespace
{
    constexpr size_t MaximumRanges = 128;
    constexpr size_t MaximumRoots = 65536;
    struct Range
    {
        Object** start;
        size_t count;
    };
    Range Ranges[MaximumRanges]{};
    std::atomic<size_t> RangeCount{0};
    size_t AdmittedSlots;
    std::atomic<LxrReferenceCounting*> Observer{nullptr};
    std::atomic<uint64_t> Births{0};
    std::atomic<uint64_t> BirthErrors{0};
    std::atomic<uint64_t> Errors{0};
    uint64_t ObservedIncrements;
    uint64_t ObservedDecrements;
    uint64_t ObservedNonNullCurrent;
    uintptr_t CopiedRoots[MaximumRoots]{};
    size_t RootCount;
    unsigned InitialGcCount;
    LxrRcMetrics LastMetrics{};
    struct DrainContext
    {
        LxrReferenceCounting* observer;
        LxrRcResult result = LxrRcResult::InvalidPhase;
        std::atomic<bool> done{false};
    };

    bool IsObservedSlot(Object** slot)
    {
        uintptr_t address = reinterpret_cast<uintptr_t>(slot);
        size_t count = RangeCount.load(std::memory_order_acquire);
        for (size_t index = 0; index < count; index++)
        {
            uintptr_t start = reinterpret_cast<uintptr_t>(Ranges[index].start);
            if (address >= start && (address - start) / sizeof(Object*) < Ranges[index].count)
            {
                return true;
            }
        }
        return false;
    }

    void Unlog(Object** slot, void*)
    {
        if (!LxrRcVerticalUnlogObservedSlot(slot))
        {
            Errors.fetch_add(1, std::memory_order_relaxed);
        }
    }

    void Observe(bool increment, uintptr_t, Object* current, void*)
    {
        if (increment)
        {
            ObservedIncrements++;
            ObservedNonNullCurrent += current != nullptr;
        }
        else
        {
            ObservedDecrements++;
        }
    }

    void CopyRoot(Object** slot, ScanContext*, uint32_t flags)
    {
        Object* target = *slot;
        if (target == nullptr)
        {
            return;
        }
        if ((flags & GC_CALL_INTERIOR) != 0)
        {
            target = g_theGCHeap->GetContainingObject(target, false);
            if (target == nullptr)
            {
                Errors.fetch_add(1, std::memory_order_relaxed);
                return;
            }
        }
        if (RootCount == MaximumRoots)
        {
            Errors.fetch_add(1, std::memory_order_relaxed);
            return;
        }
        CopiedRoots[RootCount++] = reinterpret_cast<uintptr_t>(target);
    }

    void DrainOnGcThread(void* context)
    {
        DrainContext* drain = static_cast<DrainContext*>(context);
        unsigned generation = g_theGCHeap->GetMaxGeneration();
        int heaps = g_theGCHeap->GetNumberOfHeaps();
        for (int heap = 0; heap < heaps; heap++)
        {
            ScanContext scan{};
            scan.promotion = true;
            scan.thread_number = heap;
            scan.thread_count = heaps;
            GCToEEInterface::GcScanRoots(CopyRoot, generation, generation, &scan);
        }
        drain->result = drain->observer->DrainObservedLogs(Unlog, Observe, nullptr);
        LastMetrics = drain->observer->GetMetrics();
        drain->done.store(true, std::memory_order_release);
    }
}

void LxrRcVerticalObserveWrite(Object** destination, Object* oldReference)
{
    LxrReferenceCounting* observer = Observer.load(std::memory_order_acquire);
    if (observer == nullptr || !IsObservedSlot(destination))
    {
        return;
    }
    LxrRcMutator* mutator = observer->FindMutator(GCToEEInterface::GetAllocContext());
    if (mutator == nullptr ||
        observer->CaptureClaimedWrite(mutator, destination, oldReference) != LxrRcResult::Success)
    {
        GCToEEInterface::HandleFatalError(static_cast<unsigned int>(E_FAIL));
    }
}

void LxrRcVerticalOnThreadDetach(gc_alloc_context* context)
{
    LxrReferenceCounting* observer = Observer.load(std::memory_order_acquire);
    if (observer == nullptr)
    {
        return;
    }
    LxrRcMutator* mutator = observer->FindMutator(context);
    if (mutator != nullptr && observer->DetachMutator(mutator) != LxrRcResult::Success)
    {
        GCToEEInterface::HandleFatalError(static_cast<unsigned int>(E_FAIL));
    }
}

extern "C" DLLEXPORT int32_t GC_LxrRcVerticalObserver_Begin()
{
    if (Observer.load() != nullptr)
    {
        return 1;
    }
    LxrReferenceCounting* observer = new (std::nothrow) LxrReferenceCounting;
    if (observer == nullptr)
    {
        return 2;
    }
    if (observer->InitializeObserver(8192, 32) != LxrRcResult::Success)
    {
        delete observer;
        return 3;
    }
    RangeCount.store(0);
    AdmittedSlots = 0;
    RootCount = 0;
    Births.store(0);
    BirthErrors.store(0);
    Errors.store(0);
    ObservedIncrements = 0;
    ObservedDecrements = 0;
    ObservedNonNullCurrent = 0;
    InitialGcCount = g_theGCHeap->GetGcCount();
    Observer.store(observer, std::memory_order_release);
    return 0;
}

extern "C" DLLEXPORT int32_t GC_LxrRcVerticalObserver_Register(OBJECTHANDLE handle)
{
    LxrReferenceCounting* observer = Observer.load(std::memory_order_acquire);
    if (observer == nullptr || handle == nullptr)
    {
        return 1;
    }
    Object* object = *reinterpret_cast<Object**>(handle);
    if (object == nullptr)
    {
        return 2;
    }
    bool complete = GCReferenceRanges::Enumerate(object, [](Object** start, size_t count)
    {
        size_t index = RangeCount.load(std::memory_order_relaxed);
        if (index == MaximumRanges || count > 8192 - AdmittedSlots)
        {
            return false;
        }
        Ranges[index] = {start, count};
        AdmittedSlots += count;
        for (size_t slot = 0; slot < count; slot++)
        {
            if (!LxrRcVerticalUnlogObservedSlot(start + slot))
            {
                return false;
            }
            RangeCount.store(index + 1, std::memory_order_release);
        }
        return true;
    });
    return complete ? 0 : 3;
}

extern "C" DLLEXPORT int32_t GC_LxrRcVerticalObserver_Attach()
{
    LxrReferenceCounting* observer = Observer.load(std::memory_order_acquire);
    if (observer == nullptr)
    {
        return 1;
    }
    LxrRcMutator* mutator;
    return observer->AttachMutator(GCToEEInterface::GetAllocContext(), &mutator) ==
        LxrRcResult::Success ? 0 : 2;
}

extern "C" DLLEXPORT int32_t GC_LxrRcVerticalObserver_End()
{
    LxrReferenceCounting* observer = Observer.exchange(nullptr, std::memory_order_acq_rel);
    if (observer == nullptr)
    {
        return 1;
    }
    if (g_theGCHeap->GetGcCount() != InitialGcCount)
    {
        delete observer;
        return 2;
    }
    GCToEEInterface::SuspendEE(SUSPEND_FOR_GC);
    DrainContext drain{observer};
    if (!GCToEEInterface::CreateThread(DrainOnGcThread, &drain, false, "LXR P4 observer"))
    {
        GCToEEInterface::RestartEE(false);
        delete observer;
        return 3;
    }
    while (!drain.done.load(std::memory_order_acquire))
    {
        GCToOSInterface::YieldThread(0);
    }
    GCToEEInterface::RestartEE(false);
    delete observer;
    return drain.result == LxrRcResult::Success && Errors.load() == 0 && BirthErrors.load() == 0 ? 0 : 3;
}

extern "C" DLLEXPORT uint64_t GC_LxrRcVerticalObserver_GetMetric(uint32_t metric)
{
    switch (metric)
    {
        case 0: return ObservedIncrements;
        case 1: return ObservedDecrements;
        case 2: return ObservedNonNullCurrent;
        case 3: return LastMetrics.threadFlushes;
        case 4: return LastMetrics.bufferFlushes;
        case 5: return Births.load();
        case 6: return RootCount;
        case 7: return Errors.load() + BirthErrors.load();
        default: return UINT64_MAX;
    }
}
