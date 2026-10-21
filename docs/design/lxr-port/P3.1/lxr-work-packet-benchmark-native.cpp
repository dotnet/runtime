// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/lxr_work_scheduler.h"

#include <atomic>
#include <chrono>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <mutex>
#include <new>
#include <vector>

#ifdef _MSC_VER
#define P31_EXPORT extern "C" __declspec(dllexport)
#else
#define P31_EXPORT extern "C" __attribute__((visibility("default")))
#endif

namespace
{
    enum class BenchmarkMode : uint32_t
    {
        Direct,
        Scheduled,
        ScheduledNull,
        ScheduledInjected,
        ScheduledReplicate,
    };

    struct BenchmarkState;

    struct BenchmarkSlot
    {
        BenchmarkSlot(uint64_t id, size_t capacity, BenchmarkState* owner);

        std::vector<uintptr_t> references;
        std::atomic<uint64_t> result;
        std::mutex gate;
        LxrWorkPacket packet;
        BenchmarkState* owner;
    };

    struct BenchmarkState
    {
        BenchmarkState(
            size_t workerCount,
            size_t producerCount,
            size_t capacity,
            BenchmarkMode benchmarkMode)
            : mode(benchmarkMode),
              referenceCapacity(capacity),
              submitted(0),
              completed(0),
              aaTicksA(0),
              aaTicksB(0),
              aaCountA(0),
              aaCountB(0)
        {
            slots.reserve(producerCount);
            for (size_t index = 0; index < producerCount; index++)
            {
                slots.push_back(std::make_unique<BenchmarkSlot>(index + 1, capacity, this));
            }
            initialized = scheduler.Initialize(workerCount) == LxrWorkSchedulerResult::Success;
        }

        ~BenchmarkState()
        {
            scheduler.Shutdown();
        }

        static uint64_t Consume(
            const uintptr_t* references,
            size_t count,
            uint64_t seed,
            size_t passes)
        {
            uint64_t result = seed ^ UINT64_C(0x9e3779b97f4a7c15);
            for (size_t pass = 0; pass < passes; pass++)
            {
                for (size_t index = 0; index < count; index++)
                {
                    result ^= static_cast<uint64_t>(references[index]) +
                        UINT64_C(0x9e3779b97f4a7c15) +
                        (result << 6) +
                        (result >> 2);
                }
            }
            return result;
        }

        static void Execute(LxrWorkPacket* packet, size_t, void* context)
        {
            BenchmarkSlot* slot = static_cast<BenchmarkSlot*>(context);
            size_t passes = slot->owner->mode == BenchmarkMode::ScheduledInjected ? 65 : 1;
            size_t count =
                slot->owner->mode == BenchmarkMode::ScheduledNull ?
                    0 :
                    packet->GetReferenceCount();
            uint64_t result = Consume(
                packet->GetReferences(),
                count,
                packet->GetId(),
                passes);
            slot->result.store(result, std::memory_order_release);
            slot->owner->completed.fetch_add(1, std::memory_order_relaxed);
        }

        LxrWorkScheduler scheduler;
        std::vector<std::unique_ptr<BenchmarkSlot>> slots;
        BenchmarkMode mode;
        size_t referenceCapacity;
        bool initialized = false;
        std::atomic<uint64_t> submitted;
        std::atomic<uint64_t> completed;
        std::atomic<uint64_t> aaTicksA;
        std::atomic<uint64_t> aaTicksB;
        std::atomic<uint64_t> aaCountA;
        std::atomic<uint64_t> aaCountB;
    };

    BenchmarkSlot::BenchmarkSlot(uint64_t id, size_t capacity, BenchmarkState* ownerState)
        : references(capacity),
          result(0),
          packet(
              id,
              LxrWorkBucketStage::Unconstrained,
              references.data(),
              capacity,
              BenchmarkState::Execute,
              this),
          owner(ownerState)
    {
    }
}

struct P31BenchmarkMetrics
{
    uint64_t submitted;
    uint64_t completed;
    uint64_t schedulerSubmitted;
    uint64_t schedulerExecuted;
    uint64_t stolen;
    uint64_t spilled;
    uint64_t ownershipViolations;
    uint64_t parks;
    uint64_t wakes;
    uint64_t aaTicksA;
    uint64_t aaTicksB;
    uint64_t aaCountA;
    uint64_t aaCountB;
};

namespace
{
    uint64_t RunScheduled(
        BenchmarkState* state,
        BenchmarkSlot& slot,
        size_t producerIndex,
        int32_t* status)
    {
        if (slot.packet.GetState() == LxrWorkPacketState::Completed)
        {
            if (state->scheduler.RecycleCompletedPacket(&slot.packet) !=
                LxrWorkSchedulerResult::Success)
            {
                *status = 2;
                return 0;
            }
        }

        slot.result.store(0, std::memory_order_release);
        state->submitted.fetch_add(1, std::memory_order_relaxed);
        LxrWorkSchedulerResult submit = state->scheduler.Submit(&slot.packet, producerIndex);
        if (submit != LxrWorkSchedulerResult::Success)
        {
            *status = 3;
            return 0;
        }
        if (state->scheduler.WaitForPacket(&slot.packet, 30'000) !=
            LxrWorkSchedulerResult::Success)
        {
            *status = 4;
            return 0;
        }

        *status = 0;
        return slot.result.load(std::memory_order_acquire);
    }
}

P31_EXPORT void* P31_Benchmark_Create(
    size_t workerCount,
    size_t producerCount,
    size_t referenceCapacity,
    uint32_t mode)
{
    if (workerCount == 0 ||
        producerCount == 0 ||
        !LxrIsSupportedReferencePacketCapacity(referenceCapacity) ||
        mode > static_cast<uint32_t>(BenchmarkMode::ScheduledReplicate))
    {
        return nullptr;
    }

    BenchmarkState* state = new (std::nothrow) BenchmarkState(
        workerCount,
        producerCount,
        referenceCapacity,
        static_cast<BenchmarkMode>(mode));
    if (state == nullptr || !state->initialized)
    {
        delete state;
        return nullptr;
    }
    return state;
}

P31_EXPORT uint64_t P31_Benchmark_Run(
    void* handle,
    size_t producerIndex,
    uint64_t sequence,
    int32_t* status)
{
    BenchmarkState* state = static_cast<BenchmarkState*>(handle);
    if (status == nullptr)
    {
        return 0;
    }
    if (state == nullptr || producerIndex >= state->slots.size())
    {
        *status = 1;
        return 0;
    }

    BenchmarkSlot& slot = *state->slots[producerIndex];
    std::lock_guard<std::mutex> guard(slot.gate);
    for (size_t index = 0; index < slot.references.size(); index++)
    {
        slot.references[index] =
            static_cast<uintptr_t>(sequence * 1'315'423'911ULL + index * 17 + producerIndex);
    }

    if (state->mode == BenchmarkMode::Direct)
    {
        *status = 0;
        return BenchmarkState::Consume(
            slot.references.data(),
            slot.references.size(),
            sequence,
            1);
    }

    if (state->mode != BenchmarkMode::ScheduledReplicate)
    {
        return RunScheduled(state, slot, producerIndex, status);
    }

    std::chrono::steady_clock::time_point start = std::chrono::steady_clock::now();
    uint64_t first = RunScheduled(state, slot, producerIndex, status);
    if (*status != 0)
    {
        return 0;
    }
    std::chrono::steady_clock::time_point middle = std::chrono::steady_clock::now();
    uint64_t second = RunScheduled(state, slot, producerIndex, status);
    if (*status != 0)
    {
        return 0;
    }
    std::chrono::steady_clock::time_point end = std::chrono::steady_clock::now();

    uint64_t firstTicks = static_cast<uint64_t>(
        std::chrono::duration_cast<std::chrono::nanoseconds>(middle - start).count());
    uint64_t secondTicks = static_cast<uint64_t>(
        std::chrono::duration_cast<std::chrono::nanoseconds>(end - middle).count());
    if ((sequence & 1) == 0)
    {
        state->aaTicksA.fetch_add(firstTicks, std::memory_order_relaxed);
        state->aaTicksB.fetch_add(secondTicks, std::memory_order_relaxed);
    }
    else
    {
        state->aaTicksB.fetch_add(firstTicks, std::memory_order_relaxed);
        state->aaTicksA.fetch_add(secondTicks, std::memory_order_relaxed);
    }
    state->aaCountA.fetch_add(1, std::memory_order_relaxed);
    state->aaCountB.fetch_add(1, std::memory_order_relaxed);
    return first + (second ^ UINT64_C(0x9e3779b97f4a7c15));
}

P31_EXPORT int32_t P31_Benchmark_GetMetrics(void* handle, P31BenchmarkMetrics* metrics)
{
    BenchmarkState* state = static_cast<BenchmarkState*>(handle);
    if (state == nullptr || metrics == nullptr)
    {
        return 1;
    }
    LxrWorkSchedulerMetrics schedulerMetrics = state->scheduler.GetMetrics();
    metrics->submitted = state->submitted.load(std::memory_order_relaxed);
    metrics->completed = state->completed.load(std::memory_order_relaxed);
    metrics->schedulerSubmitted = schedulerMetrics.submitted;
    metrics->schedulerExecuted = schedulerMetrics.executed;
    metrics->stolen = schedulerMetrics.stolen;
    metrics->spilled = schedulerMetrics.spilled;
    metrics->ownershipViolations = schedulerMetrics.ownershipViolations;
    metrics->parks = schedulerMetrics.parks;
    metrics->wakes = schedulerMetrics.wakes;
    metrics->aaTicksA = state->aaTicksA.load(std::memory_order_relaxed);
    metrics->aaTicksB = state->aaTicksB.load(std::memory_order_relaxed);
    metrics->aaCountA = state->aaCountA.load(std::memory_order_relaxed);
    metrics->aaCountB = state->aaCountB.load(std::memory_order_relaxed);
    return 0;
}

P31_EXPORT void P31_Benchmark_Destroy(void* handle)
{
    delete static_cast<BenchmarkState*>(handle);
}
