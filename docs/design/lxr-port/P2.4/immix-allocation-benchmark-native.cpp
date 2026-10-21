// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/immix_allocation.h"

#include <atomic>
#include <condition_variable>
#include <mutex>
#include <thread>

#ifdef _MSC_VER
#define P24_EXPORT extern "C" __declspec(dllexport)
#define P24_NOINLINE __declspec(noinline)
#else
#define P24_EXPORT extern "C" __attribute__((visibility("default")))
#define P24_NOINLINE __attribute__((noinline))
#endif

namespace
{
    constexpr uintptr_t DataStart = UINT64_C(0x0000000400000000);
    constexpr size_t BlockCount = 4096;
    constexpr size_t HalfBlockCount = BlockCount / 2;
    constexpr size_t DataSize = BlockCount * ImmixBlockGeometry::BlockBytes;
    constexpr size_t HalfDataSize =
        HalfBlockCount * ImmixBlockGeometry::BlockBytes;
    constexpr uint32_t MaxWorkers = 16;

    enum class BenchmarkMode : uint32_t
    {
        Fresh,
        Recycled,
        ConcurrentRecycled,
        HoleScan,
        LongLivedCache,
        ExtraScan,
        OwnerDelay,
    };

    LxrSideMetadataLayout s_layout;
    SideMetadataManager s_metadata;
    ImmixBlockManager s_blocks;
    ImmixAllocationManager s_fresh;
    ImmixAllocationManager s_recycled;
    bool s_initialized;
    bool s_data_reserved;
    std::thread s_workers[MaxWorkers];
    std::atomic<bool> s_shutdown(false);
    std::atomic<uint64_t> s_generation(0);
    std::atomic<uint32_t> s_completed(0);
    std::atomic<uint32_t> s_worker_count(0);
    std::atomic<uint32_t> s_mode(0);
    std::atomic<size_t> s_operation_count(0);
    std::atomic<uintptr_t> s_checksum(0);
    std::atomic<bool> s_failed(false);
    std::condition_variable s_work_available;
    std::mutex s_work_mutex;

    bool IsRecycledMode(BenchmarkMode mode)
    {
        return (mode == BenchmarkMode::Recycled) ||
            (mode == BenchmarkMode::ConcurrentRecycled) ||
            (mode == BenchmarkMode::LongLivedCache) ||
            (mode == BenchmarkMode::OwnerDelay);
    }

    void SeedBlock(uintptr_t block, uint8_t state)
    {
        SideMetadataResult result = s_metadata.Store(
            LxrSideMetadataKind::BlockMark,
            block,
            state,
            SideMetadataMemoryOrder::SequentiallyConsistent);
        if (result == SideMetadataResult::Success)
        {
            result = s_metadata.Store(
                LxrSideMetadataKind::BlockOwner,
                block,
                ImmixBlockManager::NoOwner,
                SideMetadataMemoryOrder::SequentiallyConsistent);
        }
        if (result == SideMetadataResult::Success)
        {
            result = s_metadata.Store(
                LxrSideMetadataKind::BlockInUse,
                block,
                0,
                SideMetadataMemoryOrder::SequentiallyConsistent);
        }
        if (result == SideMetadataResult::Success)
        {
            result = s_metadata.Store(
                LxrSideMetadataKind::PhaseEpoch,
                block,
                0,
                SideMetadataMemoryOrder::SequentiallyConsistent);
        }
        if (result != SideMetadataResult::Success)
        {
            s_failed.store(true, std::memory_order_relaxed);
        }
    }

    bool RunAllocationOperation(
        ImmixAllocationManager* manager,
        uintptr_t owner,
        ImmixAllocationSource source,
        ImmixMarkingContext markingContext,
        bool delayWhileOwned,
        uintptr_t* checksum)
    {
        ImmixBumpAllocator allocator;
        if (allocator.Initialize(manager, owner) !=
            SideMetadataResult::Success)
        {
            return false;
        }

        ImmixAllocationStatus status;
        if ((allocator.TryRefill(source, markingContext, &status) !=
             SideMetadataResult::Success) ||
            (status != ImmixAllocationStatus::Ready))
        {
            return false;
        }

        ImmixAllocationRequest request = {
            MIN_OBJECT_SIZE,
            DATA_ALIGNMENT,
            0,
        };
        uintptr_t address;
        if ((allocator.TryAllocate(request, &address, &status) !=
             SideMetadataResult::Success) ||
            (status != ImmixAllocationStatus::Allocated))
        {
            return false;
        }
        *checksum ^= address;

        if (delayWhileOwned)
        {
            for (size_t spin = 0; spin < 65536; spin++)
            {
                YieldProcessor();
            }
        }

        return (allocator.Reset(&status) == SideMetadataResult::Success) &&
            (status == ImmixAllocationStatus::Ready);
    }

    bool RunWorker(
        uint32_t worker,
        uint32_t workerCount,
        BenchmarkMode mode,
        size_t operationCount,
        uintptr_t* checksum)
    {
        size_t first = (operationCount * worker) / workerCount;
        size_t end = (operationCount * (worker + 1)) / workerCount;
        uintptr_t owner = static_cast<uintptr_t>(worker + 1);
        for (size_t operation = first; operation < end; operation++)
        {
            switch (mode)
            {
                case BenchmarkMode::Fresh:
                    if (!RunAllocationOperation(
                            &s_fresh,
                            owner,
                            ImmixAllocationSource::MutatorFresh,
                            ImmixMarkingContext::Inactive,
                            false,
                            checksum))
                    {
                        return false;
                    }
                    break;

                case BenchmarkMode::Recycled:
                    if (!RunAllocationOperation(
                            &s_recycled,
                            owner,
                            ImmixAllocationSource::MutatorRecycled,
                            ImmixMarkingContext::Inactive,
                            false,
                            checksum))
                    {
                        return false;
                    }
                    break;

                case BenchmarkMode::ConcurrentRecycled:
                    if (!RunAllocationOperation(
                            &s_recycled,
                            owner,
                            ImmixAllocationSource::MutatorRecycled,
                            ImmixMarkingContext::ConcurrentOrFinalMark,
                            false,
                            checksum))
                    {
                        return false;
                    }
                    break;

                case BenchmarkMode::HoleScan:
                case BenchmarkMode::ExtraScan:
                {
                    size_t repeats =
                        mode == BenchmarkMode::ExtraScan ? 64 : 1;
                    for (size_t repeat = 0; repeat < repeats; repeat++)
                    {
                        uintptr_t block =
                            DataStart + HalfDataSize +
                            ((operation % HalfBlockCount) *
                             ImmixBlockGeometry::BlockBytes);
                        ImmixHole hole;
                        size_t nextLine;
                        ImmixAllocationStatus status;
                        if ((s_recycled.TryFindNextHole(
                                 block,
                                 0,
                                 &hole,
                                 &nextLine,
                                 &status) != SideMetadataResult::Success) ||
                            (status != ImmixAllocationStatus::Ready))
                        {
                            return false;
                        }
                        *checksum ^= hole.end + nextLine;
                    }
                    break;
                }

                case BenchmarkMode::LongLivedCache:
                    if ((operation % 10) == 0)
                    {
                        if (!RunAllocationOperation(
                                &s_fresh,
                                owner,
                                ImmixAllocationSource::MutatorFresh,
                                ImmixMarkingContext::Inactive,
                                false,
                                checksum))
                        {
                            return false;
                        }
                    }
                    else if (!RunAllocationOperation(
                                 &s_recycled,
                                 owner,
                                 ImmixAllocationSource::MutatorRecycled,
                                 ImmixMarkingContext::Inactive,
                                 false,
                                 checksum))
                    {
                        return false;
                    }
                    break;

                case BenchmarkMode::OwnerDelay:
                    if (!RunAllocationOperation(
                            &s_recycled,
                            owner,
                            ImmixAllocationSource::MutatorRecycled,
                            ImmixMarkingContext::Inactive,
                            true,
                            checksum))
                    {
                        return false;
                    }
                    break;

                default:
                    return false;
            }
        }

        return true;
    }

    void WorkerMain(uint32_t worker)
    {
        uint64_t observedGeneration = 0;
        while (true)
        {
            std::unique_lock<std::mutex> lock(s_work_mutex);
            s_work_available.wait(lock, [&]()
            {
                return s_shutdown.load(std::memory_order_acquire) ||
                    (s_generation.load(std::memory_order_acquire) !=
                     observedGeneration);
            });
            if (s_shutdown.load(std::memory_order_acquire))
            {
                return;
            }
            uint64_t generation =
                s_generation.load(std::memory_order_relaxed);
            observedGeneration = generation;

            uint32_t workerCount =
                s_worker_count.load(std::memory_order_relaxed);
            lock.unlock();
            if (worker >= workerCount)
            {
                continue;
            }

            uintptr_t checksum = 0;
            bool success = RunWorker(
                worker,
                workerCount,
                static_cast<BenchmarkMode>(
                    s_mode.load(std::memory_order_relaxed)),
                s_operation_count.load(std::memory_order_relaxed),
                &checksum);
            if (!success)
            {
                s_failed.store(true, std::memory_order_relaxed);
            }
            s_checksum.fetch_xor(checksum, std::memory_order_relaxed);
            s_completed.fetch_add(1, std::memory_order_release);
        }
    }

    bool ReinitializeAllocators()
    {
        s_fresh.Shutdown();
        s_recycled.Shutdown();
        return
            (s_fresh.Initialize(
                 &s_metadata,
                 &s_blocks,
                 DataStart,
                 HalfDataSize,
                 1) == SideMetadataResult::Success) &&
            (s_recycled.Initialize(
                 &s_metadata,
                 &s_blocks,
                 DataStart + HalfDataSize,
                 HalfDataSize,
                 1) == SideMetadataResult::Success);
    }

    bool AdvancePhase()
    {
        ImmixBlockOperationStatus status;
        return
            (s_blocks.StartGcPause(&status) ==
             SideMetadataResult::Success) &&
            (status == ImmixBlockOperationStatus::Updated) &&
            (s_blocks.ReleaseGcPause(&status) ==
             SideMetadataResult::Success) &&
            (status == ImmixBlockOperationStatus::Updated);
    }

    int32_t FailInitialize(int32_t error)
    {
        s_fresh.Shutdown();
        s_recycled.Shutdown();
        s_blocks.Shutdown();
        s_metadata.Shutdown();
        if (s_data_reserved)
        {
            GCToOSInterface::VirtualRelease(
                reinterpret_cast<void*>(DataStart),
                DataSize);
        }
        s_data_reserved = false;
        return error;
    }
}

P24_EXPORT int32_t P24_Initialize()
{
    if (s_initialized)
    {
        return 1;
    }

    void* data = GCToOSInterface::VirtualReserveAt(
        reinterpret_cast<void*>(DataStart),
        DataSize,
        VirtualReserveFlags::None);
    if (data != reinterpret_cast<void*>(DataStart))
    {
        return 2;
    }
    s_data_reserved = true;
    if (!GCToOSInterface::VirtualCommit(data, DataSize))
    {
        return FailInitialize(3);
    }
    if (LxrSideMetadataLayout::Create(1, &s_layout) !=
        SideMetadataResult::Success)
    {
        return FailInitialize(4);
    }
    if (s_metadata.Initialize(&s_layout) != SideMetadataResult::Success)
    {
        return FailInitialize(5);
    }
    if (s_blocks.Initialize(&s_metadata) != SideMetadataResult::Success)
    {
        return FailInitialize(6);
    }
    if (s_blocks.RegisterBlockRange(DataStart, DataSize) !=
        SideMetadataResult::Success)
    {
        return FailInitialize(7);
    }

    for (size_t index = 0; index < BlockCount; index++)
    {
        uintptr_t block =
            DataStart + (index * ImmixBlockGeometry::BlockBytes);
        SeedBlock(block, index < HalfBlockCount ? 0 : 1);
    }
    if (s_failed.load(std::memory_order_relaxed) ||
        !ReinitializeAllocators())
    {
        return FailInitialize(8);
    }

    s_shutdown.store(false, std::memory_order_relaxed);
    s_generation.store(0, std::memory_order_relaxed);
    for (uint32_t worker = 0; worker < MaxWorkers; worker++)
    {
        s_workers[worker] = std::thread(WorkerMain, worker);
    }
    s_initialized = true;
    return 0;
}

P24_EXPORT void P24_Shutdown()
{
    if (!s_initialized)
    {
        return;
    }

    {
        std::lock_guard<std::mutex> lock(s_work_mutex);
        s_shutdown.store(true, std::memory_order_release);
        s_generation.fetch_add(1, std::memory_order_release);
    }
    s_work_available.notify_all();
    for (uint32_t worker = 0; worker < MaxWorkers; worker++)
    {
        s_workers[worker].join();
    }

    s_fresh.Shutdown();
    s_recycled.Shutdown();
    s_blocks.Shutdown();
    s_metadata.Shutdown();
    if (s_data_reserved)
    {
        GCToOSInterface::VirtualRelease(
            reinterpret_cast<void*>(DataStart),
            DataSize);
    }
    s_data_reserved = false;
    s_initialized = false;
}

P24_EXPORT P24_NOINLINE uintptr_t P24_RunBatch(
    uint32_t rawMode,
    size_t operationCount,
    uint32_t workerCount)
{
    if (!s_initialized ||
        (rawMode > static_cast<uint32_t>(BenchmarkMode::OwnerDelay)) ||
        (operationCount == 0) ||
        (operationCount > HalfBlockCount) ||
        (workerCount == 0) ||
        (workerCount > MaxWorkers) ||
        !ReinitializeAllocators())
    {
        return UINTPTR_MAX;
    }

    BenchmarkMode mode = static_cast<BenchmarkMode>(rawMode);
    s_failed.store(false, std::memory_order_relaxed);
    s_completed.store(0, std::memory_order_relaxed);
    s_worker_count.store(workerCount, std::memory_order_relaxed);
    s_mode.store(rawMode, std::memory_order_relaxed);
    s_operation_count.store(operationCount, std::memory_order_relaxed);
    s_checksum.store(0, std::memory_order_relaxed);
    {
        std::lock_guard<std::mutex> lock(s_work_mutex);
        s_generation.fetch_add(1, std::memory_order_release);
    }
    s_work_available.notify_all();
    while (s_completed.load(std::memory_order_acquire) != workerCount)
    {
        YieldProcessor();
    }

    if (s_failed.load(std::memory_order_relaxed))
    {
        return UINTPTR_MAX;
    }
    if ((mode != BenchmarkMode::HoleScan) &&
        (mode != BenchmarkMode::ExtraScan) &&
        !AdvancePhase())
    {
        return UINTPTR_MAX;
    }

    return s_checksum.load(std::memory_order_relaxed);
}
