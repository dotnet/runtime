// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/immix_block.h"

#include <atomic>
#include <condition_variable>
#include <mutex>
#include <thread>

#ifdef _MSC_VER
#define P22_EXPORT extern "C" __declspec(dllexport)
#define P22_NOINLINE __declspec(noinline)
#else
#define P22_EXPORT extern "C" __attribute__((visibility("default")))
#define P22_NOINLINE __attribute__((noinline))
#endif

namespace
{
    constexpr uintptr_t DataStart = UINT64_C(0x0000000400000000);
    constexpr size_t BlockCount = 8192;
    constexpr size_t BlocksPerPath = BlockCount / 2;
    constexpr size_t DataSize = BlockCount * ImmixBlockGeometry::BlockBytes;
    constexpr uint32_t MaximumWorkers = 16;

    enum class BenchmarkMode : uint32_t
    {
        Geometry,
        MetadataMap,
        StateCas,
        PoolOnly,
        Fresh,
        Reuse,
        Copy,
        Contended,
        ExtraCas,
        OwnerDelay,
    };

    LxrSideMetadataLayout* s_layout;
    SideMetadataManager* s_metadata;
    ImmixBlockManager* s_blocks;
    std::thread s_workers[MaximumWorkers];
    std::mutex s_mutex;
    std::condition_variable s_condition;
    uint32_t s_generation;
    uint32_t s_completed;
    uint32_t s_active_workers;
    size_t s_operation_count;
    BenchmarkMode s_mode;
    uintptr_t s_checksum;
    bool s_failed;
    bool s_stopping;
    std::atomic<size_t> s_pool_cursor;

    uintptr_t FreshBlockAt(size_t index)
    {
        return DataStart +
            ((index & (BlocksPerPath - 1)) * ImmixBlockGeometry::BlockBytes);
    }

    uintptr_t ReuseBlockAt(size_t index)
    {
        return DataStart +
            ((BlocksPerPath + (index & (BlocksPerPath - 1))) *
             ImmixBlockGeometry::BlockBytes);
    }

    bool AcquireUntilUpdated(
        uintptr_t block,
        uintptr_t owner,
        ImmixBlockAcquireKind kind)
    {
        while (true)
        {
            ImmixBlockOperationStatus status;
            SideMetadataResult result = s_blocks->TryAcquire(
                block,
                owner,
                kind,
                &status);
            if (result != SideMetadataResult::Success)
            {
                return false;
            }
            if (status == ImmixBlockOperationStatus::Updated)
            {
                return true;
            }
            if ((status != ImmixBlockOperationStatus::Contended) &&
                (status != ImmixBlockOperationStatus::Stale))
            {
                return false;
            }
            YieldProcessor();
        }
    }

    bool ReleaseUntilUpdated(uintptr_t block, uintptr_t owner)
    {
        while (true)
        {
            ImmixBlockOperationStatus status;
            SideMetadataResult result = s_blocks->TryRelease(block, owner, &status);
            if (result != SideMetadataResult::Success)
            {
                return false;
            }
            if (status == ImmixBlockOperationStatus::Updated)
            {
                return true;
            }
            if (status != ImmixBlockOperationStatus::Contended)
            {
                return false;
            }
            YieldProcessor();
        }
    }

    bool RunOne(BenchmarkMode mode, size_t index, uint32_t worker, uintptr_t* checksum)
    {
        uintptr_t owner = static_cast<uintptr_t>(worker) + 1;
        uintptr_t block = FreshBlockAt(index);
        ImmixBlockOperationStatus status;
        switch (mode)
        {
            case BenchmarkMode::Geometry:
            {
                uintptr_t blockStart;
                size_t lineIndex;
                uintptr_t address =
                    block + ((index * 37) & ImmixBlockGeometry::BlockMask);
                if ((ImmixBlockGeometry::GetBlockStart(address, &blockStart) !=
                     ImmixGeometryResult::Success) ||
                    (ImmixBlockGeometry::GetLineIndexInBlock(address, &lineIndex) !=
                     ImmixGeometryResult::Success))
                {
                    return false;
                }
                *checksum += blockStart + lineIndex;
                return true;
            }

            case BenchmarkMode::MetadataMap:
            {
                SideMetadataLocation location;
                if (s_metadata->GetLocation(
                        LxrSideMetadataKind::BlockMark,
                        block,
                        &location) != SideMetadataResult::Success)
                {
                    return false;
                }
                *checksum += reinterpret_cast<uintptr_t>(location.word) + location.shift;
                return true;
            }

            case BenchmarkMode::StateCas:
            {
                bool logged;
                if ((s_blocks->TryLog(block, &logged) != SideMetadataResult::Success) ||
                    (s_blocks->Unlog(block) != SideMetadataResult::Success))
                {
                    return false;
                }
                *checksum += logged ? 1 : 0;
                return true;
            }

            case BenchmarkMode::PoolOnly:
                *checksum += s_pool_cursor.fetch_add(1, std::memory_order_seq_cst);
                return true;

            case BenchmarkMode::Fresh:
                if (!AcquireUntilUpdated(
                        block,
                        owner,
                        ImmixBlockAcquireKind::MutatorFresh) ||
                    (s_blocks->TryRelease(block, owner, &status) !=
                     SideMetadataResult::Success) ||
                    (status != ImmixBlockOperationStatus::Updated))
                {
                    return false;
                }
                *checksum += block;
                return true;

            case BenchmarkMode::Reuse:
                block = ReuseBlockAt(index);
                if (!AcquireUntilUpdated(
                        block,
                        owner,
                        ImmixBlockAcquireKind::MutatorReusable) ||
                    (s_blocks->TryReturn(block, owner, &status) !=
                     SideMetadataResult::Success) ||
                    (status != ImmixBlockOperationStatus::Updated))
                {
                    return false;
                }
                *checksum += block;
                return true;

            case BenchmarkMode::Copy:
                block = ReuseBlockAt(index);
                if (!AcquireUntilUpdated(
                        block,
                        owner,
                        ImmixBlockAcquireKind::GcCopyReusable) ||
                    (s_blocks->TryReturn(block, owner, &status) !=
                     SideMetadataResult::Success) ||
                    (status != ImmixBlockOperationStatus::Updated))
                {
                    return false;
                }
                *checksum += block;
                return true;

            case BenchmarkMode::Contended:
                block = DataStart;
                status = ImmixBlockOperationStatus::Contended;
                if (s_blocks->TryAcquire(
                        block,
                        owner,
                        ImmixBlockAcquireKind::MutatorFresh,
                        &status) != SideMetadataResult::Success)
                {
                    return false;
                }
                if ((status == ImmixBlockOperationStatus::Contended) ||
                    (status == ImmixBlockOperationStatus::Stale))
                {
                    *checksum += 1;
                    return true;
                }
                if (status != ImmixBlockOperationStatus::Updated)
                {
                    return false;
                }
                if (!ReleaseUntilUpdated(block, owner))
                {
                    return false;
                }
                *checksum += owner;
                return true;

            case BenchmarkMode::OwnerDelay:
                if (!AcquireUntilUpdated(
                        block,
                        owner,
                        ImmixBlockAcquireKind::MutatorFresh))
                {
                    return false;
                }
                for (size_t spin = 0; spin < 256; spin++)
                {
                    YieldProcessor();
                }
                if ((s_blocks->TryRelease(block, owner, &status) !=
                     SideMetadataResult::Success) ||
                    (status != ImmixBlockOperationStatus::Updated))
                {
                    return false;
                }
                *checksum += owner;
                return true;

            case BenchmarkMode::ExtraCas:
                if (!AcquireUntilUpdated(
                        block,
                        owner,
                        ImmixBlockAcquireKind::MutatorFresh))
                {
                    return false;
                }
                for (size_t update = 0; update < 16; update++)
                {
                    bool logged;
                    if ((s_blocks->TryLog(block, &logged) != SideMetadataResult::Success) ||
                        (s_blocks->Unlog(block) != SideMetadataResult::Success))
                    {
                        return false;
                    }
                    *checksum += logged ? 1 : 0;
                }
                if ((s_blocks->TryRelease(block, owner, &status) !=
                     SideMetadataResult::Success) ||
                    (status != ImmixBlockOperationStatus::Updated))
                {
                    return false;
                }
                return true;

            default:
                return false;
        }
    }

    void Worker(uint32_t worker)
    {
        uint32_t observedGeneration = 0;
        while (true)
        {
            std::unique_lock<std::mutex> lock(s_mutex);
            s_condition.wait(
                lock,
                [&]()
                {
                    return s_stopping || (s_generation != observedGeneration);
                });
            if (s_stopping)
            {
                return;
            }

            observedGeneration = s_generation;
            uint32_t activeWorkers = s_active_workers;
            size_t operationCount = s_operation_count;
            BenchmarkMode mode = s_mode;
            lock.unlock();
            if (worker >= activeWorkers)
            {
                continue;
            }

            size_t first = (operationCount * worker) / activeWorkers;
            size_t end = (operationCount * (worker + 1)) / activeWorkers;
            uintptr_t checksum = 0;
            bool failed = false;
            for (size_t index = first; index < end; index++)
            {
                if (!RunOne(mode, index, worker, &checksum))
                {
                    failed = true;
                    break;
                }
            }

            lock.lock();
            s_checksum ^= checksum;
            s_failed |= failed;
            s_completed++;
            s_condition.notify_all();
        }
    }

    void StartWorkers()
    {
        s_generation = 0;
        s_completed = 0;
        s_active_workers = 0;
        s_operation_count = 0;
        s_checksum = 0;
        s_failed = false;
        s_stopping = false;
        s_pool_cursor.store(0, std::memory_order_seq_cst);
        for (uint32_t worker = 0; worker < MaximumWorkers; worker++)
        {
            s_workers[worker] = std::thread(Worker, worker);
        }
    }

    void StopWorkers()
    {
        if (!s_workers[0].joinable())
        {
            return;
        }

        {
            std::lock_guard<std::mutex> lock(s_mutex);
            s_stopping = true;
            s_generation++;
        }
        s_condition.notify_all();
        for (std::thread& worker : s_workers)
        {
            worker.join();
        }
    }

    uintptr_t RunWorkers(
        BenchmarkMode mode,
        size_t operationCount,
        uint32_t workerCount)
    {
        if ((workerCount == 0) || (workerCount > MaximumWorkers))
        {
            return UINTPTR_MAX;
        }

        std::unique_lock<std::mutex> lock(s_mutex);
        s_mode = mode;
        s_operation_count = operationCount;
        s_active_workers = workerCount;
        s_completed = 0;
        s_checksum = 0;
        s_failed = false;
        s_generation++;
        s_condition.notify_all();
        s_condition.wait(lock, [&]() { return s_completed == workerCount; });
        return s_failed ? UINTPTR_MAX : s_checksum;
    }

    bool EnsureMutatorPhase()
    {
        if ((s_blocks->GetGlobalPhaseEpoch() & 1) != 0)
        {
            return true;
        }

        ImmixBlockOperationStatus status;
        return (s_blocks->ReleaseGcPause(&status) == SideMetadataResult::Success) &&
            (status == ImmixBlockOperationStatus::Updated);
    }

    bool AdvanceMutatorPhase()
    {
        ImmixBlockOperationStatus status;
        if ((s_blocks->GetGlobalPhaseEpoch() & 1) == 0)
        {
            return (s_blocks->ReleaseGcPause(&status) == SideMetadataResult::Success) &&
                (status == ImmixBlockOperationStatus::Updated);
        }

        return (s_blocks->StartGcPause(&status) == SideMetadataResult::Success) &&
            (status == ImmixBlockOperationStatus::Updated) &&
            (s_blocks->ReleaseGcPause(&status) == SideMetadataResult::Success) &&
            (status == ImmixBlockOperationStatus::Updated);
    }

    bool AdvanceGcPhase()
    {
        ImmixBlockOperationStatus status;
        if ((s_blocks->GetGlobalPhaseEpoch() & 1) == 0)
        {
            if ((s_blocks->ReleaseGcPause(&status) != SideMetadataResult::Success) ||
                (status != ImmixBlockOperationStatus::Updated))
            {
                return false;
            }
        }

        return (s_blocks->StartGcPause(&status) == SideMetadataResult::Success) &&
            (status == ImmixBlockOperationStatus::Updated);
    }

    void Shutdown()
    {
        StopWorkers();
        delete s_blocks;
        delete s_metadata;
        delete s_layout;
        s_blocks = nullptr;
        s_metadata = nullptr;
        s_layout = nullptr;
    }
}

P22_EXPORT int32_t P22_Initialize()
{
    if (s_metadata != nullptr)
    {
        return -1;
    }

    s_layout = new (nothrow) LxrSideMetadataLayout;
    s_metadata = new (nothrow) SideMetadataManager;
    s_blocks = new (nothrow) ImmixBlockManager;
    if ((s_layout == nullptr) || (s_metadata == nullptr) || (s_blocks == nullptr))
    {
        Shutdown();
        return -2;
    }
    if (LxrSideMetadataLayout::Create(1, s_layout) != SideMetadataResult::Success)
    {
        Shutdown();
        return -3;
    }
    if (s_metadata->Initialize(s_layout) != SideMetadataResult::Success)
    {
        Shutdown();
        return -4;
    }
    if ((s_blocks->Initialize(s_metadata) != SideMetadataResult::Success) ||
        (s_blocks->RegisterBlockRange(DataStart, DataSize) !=
         SideMetadataResult::Success))
    {
        Shutdown();
        return -5;
    }

    for (size_t index = BlocksPerPath; index < BlockCount; index++)
    {
        uintptr_t block = DataStart + index * ImmixBlockGeometry::BlockBytes;
        if (s_metadata->Store(
                LxrSideMetadataKind::BlockMark,
                block,
                UINT8_MAX,
                SideMetadataMemoryOrder::SequentiallyConsistent) !=
            SideMetadataResult::Success)
        {
            Shutdown();
            return -6;
        }
    }

    StartWorkers();
    return 0;
}

P22_EXPORT void P22_Shutdown()
{
    Shutdown();
}

P22_EXPORT P22_NOINLINE uintptr_t P22_RunBatch(
    uint32_t mode,
    size_t operationCount,
    uint32_t workerCount)
{
    BenchmarkMode benchmarkMode = static_cast<BenchmarkMode>(mode);
    bool phaseReady;
    if (benchmarkMode == BenchmarkMode::Copy)
    {
        phaseReady = AdvanceGcPhase();
    }
    else if (benchmarkMode == BenchmarkMode::Reuse)
    {
        phaseReady = AdvanceMutatorPhase();
    }
    else
    {
        phaseReady = EnsureMutatorPhase();
    }
    if (!phaseReady)
    {
        return UINTPTR_MAX;
    }
    return RunWorkers(benchmarkMode, operationCount, workerCount);
}

P22_EXPORT P22_NOINLINE uintptr_t P22_EpochBatch(size_t operationCount)
{
    uintptr_t checksum = 0;
    if (!EnsureMutatorPhase())
    {
        return UINTPTR_MAX;
    }

    for (size_t index = 0; index < operationCount; index++)
    {
        ImmixBlockOperationStatus status;
        if ((s_blocks->StartGcPause(&status) != SideMetadataResult::Success) ||
            (status != ImmixBlockOperationStatus::Updated) ||
            (s_blocks->ReleaseGcPause(&status) != SideMetadataResult::Success) ||
            (status != ImmixBlockOperationStatus::Updated))
        {
            return UINTPTR_MAX;
        }
        checksum += s_blocks->GetGlobalPhaseEpoch();
    }
    return checksum;
}
