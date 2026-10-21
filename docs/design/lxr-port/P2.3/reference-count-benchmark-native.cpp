// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This harness benchmarks the ReferenceCountTable API in isolation against
// synthetic, explicitly registered object starts. It does not allocate real
// CoreCLR objects and does not exercise LXR collection, tracing, or block
// reclamation; it only measures the side-metadata-backed reference-count
// operations documented in src/coreclr/gc/reference_count.h.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/reference_count.h"

#include <condition_variable>
#include <mutex>
#include <thread>

#ifdef _MSC_VER
#define P23_EXPORT extern "C" __declspec(dllexport)
#define P23_NOINLINE __declspec(noinline)
#else
#define P23_EXPORT extern "C" __attribute__((visibility("default")))
#define P23_NOINLINE __attribute__((noinline))
#endif

namespace
{
#ifdef HOST_64BIT
    constexpr uintptr_t DataStart = UINT64_C(0x0000000700000000);
    constexpr size_t BlockBytes = ImmixBlockGeometry::BlockBytes;
    constexpr size_t LineBytes = ImmixBlockGeometry::LineBytes;

    // Region layout, expressed in whole Immix blocks so that no synthetic
    // object ever straddles a block boundary. Every region is reserved with
    // one dedicated call to CommitDataRange.
    constexpr size_t WidthPoolBlocks = 4;
    constexpr size_t StraddleBlocks = 1;
    constexpr size_t Reset4KiBBlocks = 1;
    constexpr size_t Reset64KiBBlocks = 2;
    constexpr size_t ResetBlockBlocks = 1;
    constexpr size_t ChurnBlocks = 1;
    constexpr size_t ContentionBlocks = 2;
    constexpr size_t TotalBlocks =
        WidthPoolBlocks +
        StraddleBlocks +
        Reset4KiBBlocks +
        Reset64KiBBlocks +
        ResetBlockBlocks +
        ChurnBlocks +
        ContentionBlocks;
    constexpr size_t DataSize = TotalBlocks * BlockBytes;

    constexpr uintptr_t WidthPoolBase = DataStart;
    constexpr uintptr_t StraddleBase = WidthPoolBase + (WidthPoolBlocks * BlockBytes);
    constexpr uintptr_t Reset4KiBBase = StraddleBase + (StraddleBlocks * BlockBytes);
    constexpr uintptr_t Reset64KiBBase = Reset4KiBBase + (Reset4KiBBlocks * BlockBytes);
    constexpr uintptr_t ResetBlockAddress = Reset64KiBBase + (Reset64KiBBlocks * BlockBytes);
    constexpr uintptr_t ChurnPoolBase = ResetBlockAddress + (ResetBlockBlocks * BlockBytes);
    constexpr uintptr_t ContentionBase = ChurnPoolBase + (ChurnBlocks * BlockBytes);

    // Every synthetic object is at least ReferenceCountTable::MinimumObjectSize
    // (24 bytes). The width pool, the reset pools, and the contention pool use
    // a rounded 32-byte stride purely so that pool extents divide evenly into
    // block-sized regions; the churn pool uses the literal 24-byte minimum.
    constexpr size_t WidthPoolStride = 32;
    constexpr size_t WidthPoolCount = 4096;
    static_assert(WidthPoolStride * WidthPoolCount == WidthPoolBlocks * BlockBytes);

    constexpr size_t Reset4KiBStride = 32;
    constexpr size_t Reset4KiBCount = (4 * 1024) / Reset4KiBStride;

    constexpr size_t Reset64KiBStride = 32;
    constexpr size_t Reset64KiBCount = (64 * 1024) / Reset64KiBStride;
    static_assert(Reset64KiBStride * Reset64KiBCount == Reset64KiBBlocks * BlockBytes);

    constexpr size_t ChurnStride = ReferenceCountTable::MinimumObjectSize;
    constexpr size_t ChurnCount = 512;
    constexpr size_t ChurnCycleLength = 10;
    constexpr size_t ChurnIncrementSlots = 8;
    constexpr size_t ChurnDecrementSlots = 1;
    static_assert(ChurnIncrementSlots + ChurnDecrementSlots < ChurnCycleLength);
    static_assert((ChurnIncrementSlots * 100) / ChurnCycleLength >= 80);

    // Straddle objects are placed line-aligned within one dedicated block so
    // their continuation-line counts are exact and independent of the other
    // regions.
    constexpr size_t StraddleZeroSize = 64;
    constexpr size_t StraddleOneSize = 2 * LineBytes;
    constexpr size_t StraddleManySize = 16 * LineBytes;
    constexpr uintptr_t StraddleZeroObject = StraddleBase;
    constexpr uintptr_t StraddleOneObject = StraddleBase + (4 * LineBytes);
    constexpr uintptr_t StraddleManyObject = StraddleBase + (16 * LineBytes);
    constexpr size_t StraddleZeroContinuations = 0;
    constexpr size_t StraddleOneContinuations = 1;
    constexpr size_t StraddleManyContinuations = 15;

    constexpr uint32_t MaximumWorkers = 16;
    constexpr size_t AdjacentWordObjectCount = 3;
    constexpr size_t AdjacentWordDistance = ReferenceCountTable::MinimumObjectSize;
    constexpr size_t SeparateLineDistance = 4096;
    constexpr size_t ExtraCasRepeats = 8;
    constexpr size_t DelaySpinCount = 512;
    static_assert(
        (AdjacentWordDistance * (MaximumWorkers - 1)) + ReferenceCountTable::MinimumObjectSize <=
        BlockBytes);
    static_assert(
        (SeparateLineDistance * (MaximumWorkers - 1)) + ReferenceCountTable::MinimumObjectSize <=
        ContentionBlocks * BlockBytes);

    enum class ContentionMode : uint32_t
    {
        SameSlot,
        AdjacentWord,
        SeparateLine,
        ExtraCas,
        Delay,
    };

    LxrSideMetadataLayout* s_layout;
    SideMetadataManager* s_metadata;
    ReferenceCountTable* s_table;
    std::thread s_workers[MaximumWorkers];
    std::mutex s_mutex;
    std::condition_variable s_condition;
    uint32_t s_generation;
    uint32_t s_completed;
    uint32_t s_active_workers;
    size_t s_operation_count;
    ContentionMode s_mode;
    uintptr_t s_checksum;
    bool s_failed;
    bool s_stopping;

    size_t DistanceFor(ContentionMode mode)
    {
        switch (mode)
        {
            case ContentionMode::AdjacentWord:
                return AdjacentWordDistance;
            case ContentionMode::SeparateLine:
                return SeparateLineDistance;
            default:
                return 0;
        }
    }

    bool CasSwap(uintptr_t object, uintptr_t* checksum)
    {
        uintptr_t observed;
        if (s_table->Load(object, &observed) != SideMetadataResult::Success)
        {
            return false;
        }

        while (true)
        {
            uintptr_t newObserved;
            bool exchanged;
            if (s_table->CompareExchange(object, observed, observed, &newObserved, &exchanged) !=
                SideMetadataResult::Success)
            {
                return false;
            }
            if (exchanged)
            {
                *checksum += observed;
                return true;
            }
            observed = newObserved;
        }
    }

    bool RunContentionOne(ContentionMode mode, uint32_t worker, uintptr_t* checksum)
    {
        uintptr_t object = mode == ContentionMode::AdjacentWord
            ? ContentionBase +
                ((static_cast<uintptr_t>(worker) % AdjacentWordObjectCount) *
                 AdjacentWordDistance)
            : ContentionBase + (static_cast<uintptr_t>(worker) * DistanceFor(mode));
        if (mode == ContentionMode::Delay)
        {
            for (size_t spin = 0; spin < DelaySpinCount; spin++)
            {
                YieldProcessor();
            }
        }

        if (!CasSwap(object, checksum))
        {
            return false;
        }

        if (mode == ContentionMode::ExtraCas)
        {
            for (size_t extra = 0; extra < ExtraCasRepeats; extra++)
            {
                if (!CasSwap(object, checksum))
                {
                    return false;
                }
            }
        }
        return true;
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
            ContentionMode mode = s_mode;
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
                if (!RunContentionOne(mode, worker, &checksum))
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

    uintptr_t RunContentionWorkers(
        ContentionMode mode,
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

    uintptr_t RunStraddleCycle(
        uintptr_t object,
        size_t size,
        size_t continuationCount,
        size_t operationCount)
    {
        uintptr_t checksum = 0;
        uintptr_t lineBase = object & ~static_cast<uintptr_t>(ImmixBlockGeometry::LineMask);
        for (size_t index = 0; index < operationCount; index++)
        {
            if (s_table->MarkStraddleLinesQuiescent(object, size) != SideMetadataResult::Success)
            {
                return UINTPTR_MAX;
            }

            // Query every expected continuation line, then query one line
            // past the straddle range and require it to *not* be a
            // continuation, so the zero-continuation case still exercises
            // IsContinuationLine.
            for (size_t line = 0; line <= continuationCount; line++)
            {
                bool continuation = false;
                uintptr_t lineAddress = lineBase + ((line + 1) * LineBytes);
                if (s_table->IsContinuationLine(lineAddress, &continuation) !=
                    SideMetadataResult::Success)
                {
                    return UINTPTR_MAX;
                }
                bool expected = line < continuationCount;
                if (continuation != expected)
                {
                    return UINTPTR_MAX;
                }
                checksum += lineAddress;
            }

            if (s_table->ClearStraddleLinesQuiescent(object, size) != SideMetadataResult::Success)
            {
                return UINTPTR_MAX;
            }
            checksum += size;
        }
        return checksum;
    }

    bool RegisterObject(uintptr_t object, size_t size)
    {
        return s_table->InitializeObjectQuiescent(object, size) == SideMetadataResult::Success;
    }

    void Shutdown()
    {
        StopWorkers();
        delete s_table;
        delete s_metadata;
        delete s_layout;
        s_table = nullptr;
        s_metadata = nullptr;
        s_layout = nullptr;
    }
#endif // HOST_64BIT
}

P23_EXPORT int32_t P23_Initialize(uint8_t logReferenceCountBits)
{
#ifndef HOST_64BIT
    (void)logReferenceCountBits;
    return -1;
#else
    if (s_metadata != nullptr)
    {
        return -1;
    }

    s_layout = new (nothrow) LxrSideMetadataLayout;
    s_metadata = new (nothrow) SideMetadataManager;
    s_table = new (nothrow) ReferenceCountTable;
    if ((s_layout == nullptr) || (s_metadata == nullptr) || (s_table == nullptr))
    {
        Shutdown();
        return -2;
    }
    if (LxrSideMetadataLayout::Create(logReferenceCountBits, s_layout) !=
        SideMetadataResult::Success)
    {
        Shutdown();
        return -3;
    }
    if (s_metadata->Initialize(s_layout) != SideMetadataResult::Success)
    {
        Shutdown();
        return -4;
    }
    if (s_metadata->CommitDataRange(DataStart, DataSize) != SideMetadataResult::Success)
    {
        Shutdown();
        return -5;
    }
    if (s_table->Initialize(s_metadata, s_layout) != SideMetadataResult::Success)
    {
        Shutdown();
        return -6;
    }

    for (size_t index = 0; index < WidthPoolCount; index++)
    {
        if (!RegisterObject(WidthPoolBase + (index * WidthPoolStride), WidthPoolStride))
        {
            Shutdown();
            return -7;
        }
    }

    if (!RegisterObject(StraddleZeroObject, StraddleZeroSize) ||
        !RegisterObject(StraddleOneObject, StraddleOneSize) ||
        !RegisterObject(StraddleManyObject, StraddleManySize))
    {
        Shutdown();
        return -8;
    }

    for (size_t index = 0; index < Reset4KiBCount; index++)
    {
        if (!RegisterObject(Reset4KiBBase + (index * Reset4KiBStride), Reset4KiBStride))
        {
            Shutdown();
            return -9;
        }
    }
    for (size_t index = 0; index < Reset64KiBCount; index++)
    {
        if (!RegisterObject(Reset64KiBBase + (index * Reset64KiBStride), Reset64KiBStride))
        {
            Shutdown();
            return -10;
        }
    }
    for (size_t index = 0; index < ChurnCount; index++)
    {
        if (!RegisterObject(ChurnPoolBase + (index * ChurnStride), ChurnStride))
        {
            Shutdown();
            return -11;
        }
    }

    if (!RegisterObject(ContentionBase, ReferenceCountTable::MinimumObjectSize))
    {
        Shutdown();
        return -12;
    }
    for (uint32_t worker = 1; worker < MaximumWorkers; worker++)
    {
        uintptr_t separate = ContentionBase + (worker * SeparateLineDistance);
        if (((worker < AdjacentWordObjectCount) &&
             !RegisterObject(
                 ContentionBase + (worker * AdjacentWordDistance),
                 ReferenceCountTable::MinimumObjectSize)) ||
            !RegisterObject(separate, ReferenceCountTable::MinimumObjectSize))
        {
            Shutdown();
            return -13;
        }
    }

    StartWorkers();
    return 0;
#endif // HOST_64BIT
}

P23_EXPORT void P23_Shutdown()
{
#ifdef HOST_64BIT
    Shutdown();
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_GetMaximumCount()
{
#ifndef HOST_64BIT
    return UINTPTR_MAX;
#else
    return s_table->GetMaximumCount();
#endif
}

// Resets and re-registers every width-pool slot, then (when value != 0)
// compare-exchanges each slot from zero to the requested starting value. This
// is intended to be called from an untimed BenchmarkDotNet [IterationSetup]
// so that the timed increment/decrement batches always start from a known,
// non-saturated (or intentionally saturated) reference count.
P23_EXPORT P23_NOINLINE uintptr_t P23_PreseedWidthPoolBatch(uintptr_t value)
{
#ifndef HOST_64BIT
    (void)value;
    return UINTPTR_MAX;
#else
    for (size_t index = 0; index < WidthPoolCount; index++)
    {
        uintptr_t object = WidthPoolBase + (index * WidthPoolStride);
        if (s_table->ResetObjectQuiescent(object, WidthPoolStride) != SideMetadataResult::Success)
        {
            return UINTPTR_MAX;
        }
        if (s_table->InitializeObjectQuiescent(object, WidthPoolStride) !=
            SideMetadataResult::Success)
        {
            return UINTPTR_MAX;
        }
        if (value != 0)
        {
            uintptr_t observed;
            bool exchanged;
            if ((s_table->CompareExchange(object, value, 0, &observed, &exchanged) !=
                 SideMetadataResult::Success) ||
                !exchanged)
            {
                return UINTPTR_MAX;
            }
        }
    }
    return 0;
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_LoadBatch(size_t operationCount)
{
#ifndef HOST_64BIT
    (void)operationCount;
    return UINTPTR_MAX;
#else
    uintptr_t checksum = 0;
    for (size_t index = 0; index < operationCount; index++)
    {
        uintptr_t object = WidthPoolBase + ((index % WidthPoolCount) * WidthPoolStride);
        uintptr_t value;
        if (s_table->Load(object, &value) != SideMetadataResult::Success)
        {
            return UINTPTR_MAX;
        }
        checksum += value + index;
    }
    return checksum;
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_IncrementBatch(size_t operationCount)
{
#ifndef HOST_64BIT
    (void)operationCount;
    return UINTPTR_MAX;
#else
    uintptr_t checksum = 0;
    for (size_t index = 0; index < operationCount; index++)
    {
        uintptr_t object = WidthPoolBase + ((index % WidthPoolCount) * WidthPoolStride);
        uintptr_t previous;
        SideMetadataUpdateStatus status;
        if (s_table->Increment(object, &previous, &status) != SideMetadataResult::Success)
        {
            return UINTPTR_MAX;
        }
        if (status != SideMetadataUpdateStatus::Updated)
        {
            return UINTPTR_MAX;
        }
        checksum += previous + static_cast<uintptr_t>(status);
    }
    return checksum;
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_DecrementBatch(size_t operationCount)
{
#ifndef HOST_64BIT
    (void)operationCount;
    return UINTPTR_MAX;
#else
    uintptr_t checksum = 0;
    for (size_t index = 0; index < operationCount; index++)
    {
        uintptr_t object = WidthPoolBase + ((index % WidthPoolCount) * WidthPoolStride);
        uintptr_t previous;
        SideMetadataUpdateStatus status;
        if (s_table->Decrement(object, &previous, &status) != SideMetadataResult::Success)
        {
            return UINTPTR_MAX;
        }
        if (status != SideMetadataUpdateStatus::Updated)
        {
            return UINTPTR_MAX;
        }
        checksum += previous + static_cast<uintptr_t>(status);
    }
    return checksum;
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_StickyIncrementBatch(size_t operationCount)
{
#ifndef HOST_64BIT
    (void)operationCount;
    return UINTPTR_MAX;
#else
    uintptr_t checksum = 0;
    for (size_t index = 0; index < operationCount; index++)
    {
        uintptr_t object = WidthPoolBase + ((index % WidthPoolCount) * WidthPoolStride);
        uintptr_t previous;
        SideMetadataUpdateStatus status;
        if ((s_table->Increment(object, &previous, &status) !=
             SideMetadataResult::Success) ||
            (status != SideMetadataUpdateStatus::Saturated))
        {
            return UINTPTR_MAX;
        }
        checksum += previous;
    }
    return checksum;
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_StickyDecrementBatch(size_t operationCount)
{
#ifndef HOST_64BIT
    (void)operationCount;
    return UINTPTR_MAX;
#else
    uintptr_t checksum = 0;
    for (size_t index = 0; index < operationCount; index++)
    {
        uintptr_t object = WidthPoolBase + ((index % WidthPoolCount) * WidthPoolStride);
        uintptr_t previous;
        SideMetadataUpdateStatus status;
        if ((s_table->Decrement(object, &previous, &status) !=
             SideMetadataResult::Success) ||
            (status != SideMetadataUpdateStatus::Saturated))
        {
            return UINTPTR_MAX;
        }
        checksum += previous;
    }
    return checksum;
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_StraddleZeroBatch(size_t operationCount)
{
#ifndef HOST_64BIT
    (void)operationCount;
    return UINTPTR_MAX;
#else
    return RunStraddleCycle(
        StraddleZeroObject,
        StraddleZeroSize,
        StraddleZeroContinuations,
        operationCount);
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_StraddleOneBatch(size_t operationCount)
{
#ifndef HOST_64BIT
    (void)operationCount;
    return UINTPTR_MAX;
#else
    return RunStraddleCycle(
        StraddleOneObject,
        StraddleOneSize,
        StraddleOneContinuations,
        operationCount);
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_StraddleManyBatch(size_t operationCount)
{
#ifndef HOST_64BIT
    (void)operationCount;
    return UINTPTR_MAX;
#else
    return RunStraddleCycle(
        StraddleManyObject,
        StraddleManySize,
        StraddleManyContinuations,
        operationCount);
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_QuiescentReset4KiBBatch(size_t operationCount)
{
#ifndef HOST_64BIT
    (void)operationCount;
    return UINTPTR_MAX;
#else
    for (size_t index = 0; index < operationCount; index++)
    {
        if (s_table->ResetRangeQuiescent(
                Reset4KiBBase,
                4 * 1024) != SideMetadataResult::Success)
        {
            return UINTPTR_MAX;
        }
    }
    return operationCount;
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_QuiescentReset64KiBBatch(size_t operationCount)
{
#ifndef HOST_64BIT
    (void)operationCount;
    return UINTPTR_MAX;
#else
    for (size_t index = 0; index < operationCount; index++)
    {
        if (s_table->ResetRangeQuiescent(
                Reset64KiBBase,
                64 * 1024) != SideMetadataResult::Success)
        {
            return UINTPTR_MAX;
        }
    }
    return operationCount;
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_QuiescentResetBlockBatch(size_t operationCount)
{
#ifndef HOST_64BIT
    (void)operationCount;
    return UINTPTR_MAX;
#else
    uintptr_t checksum = 0;
    for (size_t index = 0; index < operationCount; index++)
    {
        if (s_table->ResetBlockQuiescent(ResetBlockAddress) != SideMetadataResult::Success)
        {
            return UINTPTR_MAX;
        }
        checksum += index;
    }
    return checksum;
#endif
}

// Simulates allocation churn over a pool of pre-registered, minimum-size
// (24-byte) synthetic object starts: every 10-operation cycle issues 8
// increments (80%), 1 decrement, and 1 reset+re-initialize ("free and
// reallocate"). This does not perform real CoreCLR allocation; it only
// exercises the ReferenceCountTable transitions an allocator would drive.
// The return value is the exact number of Increment calls issued, not a
// mixed checksum, so callers can verify the >=80% increment volume exactly.
P23_EXPORT P23_NOINLINE uintptr_t P23_AllocationChurnBatch(size_t operationCount)
{
#ifndef HOST_64BIT
    (void)operationCount;
    return UINTPTR_MAX;
#else
    uintptr_t incrementCount = 0;
    for (size_t index = 0; index < operationCount; index++)
    {
        uintptr_t object = ChurnPoolBase + ((index % ChurnCount) * ChurnStride);
        size_t phase = index % ChurnCycleLength;
        if (phase < ChurnIncrementSlots)
        {
            uintptr_t previous;
            SideMetadataUpdateStatus status;
            if (s_table->Increment(object, &previous, &status) != SideMetadataResult::Success)
            {
                return UINTPTR_MAX;
            }
            incrementCount++;
        }
        else if (phase < (ChurnIncrementSlots + ChurnDecrementSlots))
        {
            uintptr_t previous;
            SideMetadataUpdateStatus status;
            if (s_table->Decrement(object, &previous, &status) != SideMetadataResult::Success)
            {
                return UINTPTR_MAX;
            }
        }
        else
        {
            if (s_table->ResetObjectQuiescent(object, ChurnStride) != SideMetadataResult::Success)
            {
                return UINTPTR_MAX;
            }
            if (s_table->InitializeObjectQuiescent(object, ChurnStride) !=
                SideMetadataResult::Success)
            {
                return UINTPTR_MAX;
            }
        }
    }
    return incrementCount;
#endif
}

P23_EXPORT P23_NOINLINE uintptr_t P23_RunContentionBatch(
    uint32_t mode,
    size_t operationCount,
    uint32_t workerCount)
{
#ifndef HOST_64BIT
    (void)mode;
    (void)operationCount;
    (void)workerCount;
    return UINTPTR_MAX;
#else
    return RunContentionWorkers(static_cast<ContentionMode>(mode), operationCount, workerCount);
#endif
}
