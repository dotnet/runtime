// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/large_object_space.h"

#include <atomic>
#include <chrono>
#include <thread>

#ifdef _MSC_VER
#define P25_EXPORT extern "C" __declspec(dllexport)
#else
#define P25_EXPORT extern "C" __attribute__((visibility("default")))
#endif

namespace
{
    enum class BenchmarkMode : uint32_t
    {
        Geometry,
        Fresh,
        Recycled,
        ReuseCounter,
        ExtraReuseUpdates,
        InjectedOwnerDelay,
    };

    LxrSideMetadataLayout* s_layout;
    SideMetadataManager* s_metadata;
    LxrLargeObjectSpace* s_space;

    bool AllocateBatch(
        size_t operations,
        bool concurrent,
        BenchmarkMode mode,
        uint32_t workerCount,
        uintptr_t* checksum)
    {
        LxrLargeObjectAllocation allocations[64];
        if ((operations > 64) ||
            (workerCount == 0) ||
            (workerCount > 16) ||
            ((operations % workerCount) != 0))
        {
            return false;
        }
        if (concurrent &&
            (s_space->BeginCollection(true, false) != LxrLargeObjectResult::Success))
        {
            return false;
        }

        std::atomic<bool> failed(false);
        uintptr_t workerChecksums[16] = {};
        std::thread workers[16];
        size_t operationsPerWorker = operations / workerCount;
        for (uint32_t worker = 0; worker < workerCount; worker++)
        {
            workers[worker] = std::thread([&, worker]()
            {
                size_t first = worker * operationsPerWorker;
                size_t end = first + operationsPerWorker;
                for (size_t index = first; index < end; index++)
                {
                    if (s_space->Allocate(
                            LxrLargeObjectSpace::MinimumObjectBytes,
                            sizeof(uintptr_t),
                            0,
                            index + 1,
                            &allocations[index]) != LxrLargeObjectResult::Success)
                    {
                        failed.store(true, std::memory_order_relaxed);
                        return;
                    }
                    workerChecksums[worker] += allocations[index].object;

                    if (mode == BenchmarkMode::ExtraReuseUpdates)
                    {
                        for (size_t repeat = 0; repeat < 4096; repeat++)
                        {
                            uintptr_t previous;
                            if (s_metadata->FetchAddWrapping(
                                    LxrSideMetadataKind::LargeObjectPageReuse,
                                    allocations[index].page_start,
                                    1,
                                    SideMetadataMemoryOrder::SequentiallyConsistent,
                                    &previous) != SideMetadataResult::Success)
                            {
                                failed.store(true, std::memory_order_relaxed);
                                return;
                            }
                            workerChecksums[worker] += previous;
                        }
                    }
                    else if (mode == BenchmarkMode::InjectedOwnerDelay)
                    {
                        std::this_thread::sleep_for(std::chrono::milliseconds(1));
                        workerChecksums[worker] +=
                            reinterpret_cast<uint8_t*>(allocations[index].object)[0];
                    }
                }
            });
        }
        for (uint32_t worker = 0; worker < workerCount; worker++)
        {
            workers[worker].join();
            *checksum += workerChecksums[worker];
        }
        if (failed.load(std::memory_order_relaxed))
        {
            return false;
        }

        if (concurrent)
        {
            if (s_space->BeginRelease() != LxrLargeObjectResult::Success)
            {
                return false;
            }
        }
        else if (s_space->BeginCollection(false, false) != LxrLargeObjectResult::Success)
        {
            return false;
        }

        for (uint32_t worker = 0; worker < workerCount; worker++)
        {
            workers[worker] = std::thread([&, worker]()
            {
                size_t first = worker * operationsPerWorker;
                size_t end = first + operationsPerWorker;
                for (size_t index = first; index < end; index++)
                {
                    if (s_space->TryRelease(allocations[index].object, index + 1) !=
                        LxrLargeObjectResult::Success)
                    {
                        failed.store(true, std::memory_order_relaxed);
                        return;
                    }
                }
            });
        }
        for (uint32_t worker = 0; worker < workerCount; worker++)
        {
            workers[worker].join();
        }
        return !failed.load(std::memory_order_relaxed) &&
            (s_space->EndCollection() == LxrLargeObjectResult::Success);
    }

    bool ResetSpace()
    {
        s_space->Shutdown();
        return s_space->Initialize(s_metadata, 64 * 1024 * 1024) ==
            LxrLargeObjectResult::Success;
    }
}

P25_EXPORT int P25_Initialize()
{
    s_layout = new (std::nothrow) LxrSideMetadataLayout();
    s_metadata = new (std::nothrow) SideMetadataManager();
    s_space = new (std::nothrow) LxrLargeObjectSpace();
    if ((s_layout == nullptr) || (s_metadata == nullptr) || (s_space == nullptr))
    {
        return 1;
    }
    if (LxrSideMetadataLayout::Create(1, s_layout) != SideMetadataResult::Success)
    {
        return 2;
    }
    if (s_metadata->Initialize(s_layout) != SideMetadataResult::Success)
    {
        return 3;
    }
    if (s_space->Initialize(s_metadata, 64 * 1024 * 1024) !=
        LxrLargeObjectResult::Success)
    {
        return 4;
    }
    return 0;
}

P25_EXPORT void P25_Shutdown()
{
    if (s_space != nullptr)
    {
        s_space->Shutdown();
        delete s_space;
        s_space = nullptr;
    }
    if (s_metadata != nullptr)
    {
        s_metadata->Shutdown();
        delete s_metadata;
        s_metadata = nullptr;
    }
    delete s_layout;
    s_layout = nullptr;
}

P25_EXPORT uintptr_t P25_RunBatch(
    uint32_t rawMode,
    size_t operations,
    uint32_t workerCount)
{
    BenchmarkMode mode = static_cast<BenchmarkMode>(rawMode);
    uintptr_t checksum = 0;
    if (mode == BenchmarkMode::Geometry)
    {
        for (size_t index = 0; index < operations; index++)
        {
            LxrLargeObjectAllocation allocation;
            if (LxrLargeObjectSpace::ComputeAllocationGeometry(
                    UINT64_C(0x0000000200000000) +
                        (index * LxrLargeObjectSpace::PageBytes),
                    LxrLargeObjectSpace::MinimumObjectBytes,
                    64,
                    8,
                    &allocation) != LxrLargeObjectResult::Success)
            {
                return UINTPTR_MAX;
            }
            checksum += allocation.object + allocation.page_count;
        }
        return checksum;
    }

    if ((mode == BenchmarkMode::Fresh) && !ResetSpace())
    {
        return UINTPTR_MAX;
    }
    if ((mode == BenchmarkMode::Recycled) ||
        (mode == BenchmarkMode::InjectedOwnerDelay))
    {
        uintptr_t primingChecksum = 0;
        if (!AllocateBatch(
                operations,
                false,
                BenchmarkMode::Recycled,
                workerCount,
                &primingChecksum))
        {
            return UINTPTR_MAX;
        }
        checksum += primingChecksum;
    }

    bool concurrent =
        (mode == BenchmarkMode::ReuseCounter) ||
        (mode == BenchmarkMode::ExtraReuseUpdates);
    return AllocateBatch(operations, concurrent, mode, workerCount, &checksum) ?
        checksum :
        UINTPTR_MAX;
}
