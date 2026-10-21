// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/lxr_work_scheduler.h"

#include <atomic>
#include <chrono>
#include <cstddef>
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <memory>
#include <string_view>
#include <vector>

namespace
{
    enum class BenchmarkArm : uint8_t
    {
        Lazy,
        Eager,
        Aa,
    };

    struct BenchmarkState;

    struct PacketSlot
    {
        PacketSlot(
            uint64_t id,
            LxrWorkBucketStage stage,
            size_t referenceCapacity,
            BenchmarkState* owner);

        std::vector<uintptr_t> references;
        LxrWorkPacket packet;
        BenchmarkState* owner;
    };

    struct BenchmarkState
    {
        BenchmarkState(size_t workers, size_t packetCount, size_t referenceCapacity)
            : callbackPasses(1),
              callbacks(0),
              checksum(0)
        {
            lazy.reserve(packetCount);
            eager.reserve(packetCount);
            for (size_t packetIndex = 0; packetIndex < packetCount; packetIndex++)
            {
                lazy.push_back(std::make_unique<PacketSlot>(
                    packetIndex + 1,
                    LxrWorkBucketStage::Unconstrained,
                    referenceCapacity,
                    this));
                eager.push_back(std::make_unique<PacketSlot>(
                    packetIndex + 1,
                    LxrRCProcessDecs,
                    referenceCapacity,
                    this));
                for (size_t index = 0; index < referenceCapacity; index++)
                {
                    uintptr_t value = static_cast<uintptr_t>(
                        (packetIndex + 1) * UINT64_C(1'315'423'911) + index * 17);
                    lazy.back()->references[index] = value;
                    eager.back()->references[index] = value;
                }
            }
            initialized = scheduler.Initialize(workers, true) ==
                LxrWorkSchedulerResult::Success;
        }

        ~BenchmarkState()
        {
            scheduler.Shutdown();
        }

        static void Execute(LxrWorkPacket* packet, size_t, void* context)
        {
            PacketSlot* slot = static_cast<PacketSlot*>(context);
            size_t passes = slot->owner->callbackPasses.load(std::memory_order_relaxed);
            uint64_t value = packet->GetId() ^ UINT64_C(0x9e3779b97f4a7c15);
            for (size_t pass = 0; pass < passes; pass++)
            {
                for (size_t index = 0; index < packet->GetReferenceCount(); index++)
                {
                    value ^= static_cast<uint64_t>(packet->GetReferences()[index]) +
                        UINT64_C(0x9e3779b97f4a7c15) +
                        (value << 6) +
                        (value >> 2);
                }
            }
            slot->owner->checksum.fetch_add(value, std::memory_order_relaxed);
            slot->owner->callbacks.fetch_add(1, std::memory_order_relaxed);
        }

        LxrWorkScheduler scheduler;
        std::vector<std::unique_ptr<PacketSlot>> lazy;
        std::vector<std::unique_ptr<PacketSlot>> eager;
        bool initialized = false;
        std::atomic<size_t> callbackPasses;
        std::atomic<uint64_t> callbacks;
        std::atomic<uint64_t> checksum;
    };

    PacketSlot::PacketSlot(
        uint64_t id,
        LxrWorkBucketStage stage,
        size_t referenceCapacity,
        BenchmarkState* ownerState)
        : references(referenceCapacity),
          packet(
              id,
              stage,
              references.data(),
              references.size(),
              BenchmarkState::Execute,
              this),
          owner(ownerState)
    {
    }

    struct CycleResult
    {
        uint64_t pauseNanoseconds;
        uint64_t totalNanoseconds;
    };

    bool PreparePacket(LxrWorkScheduler& scheduler, LxrWorkPacket* packet)
    {
        if (packet->GetState() == LxrWorkPacketState::Completed)
        {
            return scheduler.RecycleCompletedPacket(packet) ==
                LxrWorkSchedulerResult::Success;
        }
        return packet->GetState() == LxrWorkPacketState::CallerOwned;
    }

    bool RunCycle(
        BenchmarkState& state,
        bool lazy,
        size_t passes,
        CycleResult* result)
    {
        state.callbackPasses.store(passes, std::memory_order_relaxed);
        std::vector<std::unique_ptr<PacketSlot>>& packets =
            lazy ? state.lazy : state.eager;
        for (const std::unique_ptr<PacketSlot>& slot : packets)
        {
            if (!PreparePacket(state.scheduler, &slot->packet))
            {
                return false;
            }
            LxrWorkSchedulerResult submit = lazy ?
                state.scheduler.SubmitDeferredConcurrent(
                    &slot->packet,
                    LxrDeferredWorkPriority::Prioritized) :
                state.scheduler.Submit(&slot->packet);
            if (submit != LxrWorkSchedulerResult::Success)
            {
                return false;
            }
        }

        std::chrono::steady_clock::time_point totalStart =
            std::chrono::steady_clock::now();
        std::chrono::steady_clock::time_point pauseStart = totalStart;
        if (state.scheduler.BeginStwCycle() != LxrWorkSchedulerResult::Success ||
            state.scheduler.WaitForCycleCompletion(30'000) !=
                LxrWorkSchedulerResult::Success)
        {
            return false;
        }
        std::chrono::steady_clock::time_point pauseEnd =
            std::chrono::steady_clock::now();

        uint64_t generation = state.scheduler.GetGeneration();
        if (state.scheduler.ResumeConcurrentWork() !=
                LxrWorkSchedulerResult::Success ||
            state.scheduler.PublishDeferredConcurrentWork(generation) !=
                LxrWorkSchedulerResult::Success ||
            state.scheduler.WaitForIdle(30'000) !=
                LxrWorkSchedulerResult::Success ||
            state.scheduler.ResetCompletedCycle() !=
                LxrWorkSchedulerResult::Success)
        {
            return false;
        }
        std::chrono::steady_clock::time_point totalEnd =
            std::chrono::steady_clock::now();
        result->pauseNanoseconds = static_cast<uint64_t>(
            std::chrono::duration_cast<std::chrono::nanoseconds>(
                pauseEnd - pauseStart).count());
        result->totalNanoseconds = static_cast<uint64_t>(
            std::chrono::duration_cast<std::chrono::nanoseconds>(
                totalEnd - totalStart).count());
        return true;
    }

    bool ParseSize(const char* value, size_t* result, bool allowZero = false)
    {
        char* end = nullptr;
        unsigned long long parsed = std::strtoull(value, &end, 10);
        if (end == value || *end != '\0' || (!allowZero && parsed == 0))
        {
            return false;
        }
        *result = static_cast<size_t>(parsed);
        return true;
    }
}

int main(int argc, char** argv)
{
    if (argc != 7)
    {
        std::fprintf(
            stderr,
            "usage: benchmark <lazy|eager|aa> <workers> <packets> <capacity> <warmup-cycles> <measured-cycles>\n");
        return 2;
    }

    BenchmarkArm arm;
    if (std::string_view(argv[1]) == "lazy")
    {
        arm = BenchmarkArm::Lazy;
    }
    else if (std::string_view(argv[1]) == "eager")
    {
        arm = BenchmarkArm::Eager;
    }
    else if (std::string_view(argv[1]) == "aa")
    {
        arm = BenchmarkArm::Aa;
    }
    else
    {
        return 3;
    }

    size_t workers;
    size_t packets;
    size_t capacity;
    size_t warmupCycles;
    size_t measuredCycles;
    if (!ParseSize(argv[2], &workers) ||
        !ParseSize(argv[3], &packets) ||
        !ParseSize(argv[4], &capacity) ||
        !ParseSize(argv[5], &warmupCycles) ||
        !ParseSize(argv[6], &measuredCycles) ||
        !LxrIsSupportedReferencePacketCapacity(capacity))
    {
        return 4;
    }

    BenchmarkState state(workers, packets, capacity);
    if (!state.initialized)
    {
        return 5;
    }

    size_t passes = 1;
    const char* passesValue = std::getenv("P33_CALLBACK_PASSES");
    if (passesValue != nullptr && !ParseSize(passesValue, &passes, true))
    {
        return 6;
    }

    CycleResult result{};
    for (size_t cycle = 0; cycle < warmupCycles; cycle++)
    {
        bool lazy = arm != BenchmarkArm::Eager;
        if (!RunCycle(state, lazy, passes, &result))
        {
            return 7;
        }
    }

    state.callbacks.store(0, std::memory_order_relaxed);
    state.checksum.store(0, std::memory_order_relaxed);
    LxrWorkSchedulerMetrics before = state.scheduler.GetMetrics();
    uint64_t pauseA = 0;
    uint64_t pauseB = 0;
    uint64_t totalA = 0;
    uint64_t totalB = 0;
    for (size_t cycle = 0; cycle < measuredCycles; cycle++)
    {
        if (arm == BenchmarkArm::Aa)
        {
            CycleResult first{};
            CycleResult second{};
            if (!RunCycle(state, true, passes, &first) ||
                !RunCycle(state, true, passes, &second))
            {
                return 8;
            }
            if ((cycle & 1) == 0)
            {
                pauseA += first.pauseNanoseconds;
                totalA += first.totalNanoseconds;
                pauseB += second.pauseNanoseconds;
                totalB += second.totalNanoseconds;
            }
            else
            {
                pauseB += first.pauseNanoseconds;
                totalB += first.totalNanoseconds;
                pauseA += second.pauseNanoseconds;
                totalA += second.totalNanoseconds;
            }
        }
        else
        {
            bool lazy = arm == BenchmarkArm::Lazy;
            if (!RunCycle(state, lazy, passes, &result))
            {
                return 9;
            }
            pauseA += result.pauseNanoseconds;
            totalA += result.totalNanoseconds;
        }
    }

    LxrWorkSchedulerMetrics after = state.scheduler.GetMetrics();
    uint64_t multiplier = arm == BenchmarkArm::Aa ? 2 : 1;
    uint64_t expectedCallbacks = measuredCycles * packets * multiplier;
    uint64_t callbacks = state.callbacks.load(std::memory_order_relaxed);
    if (callbacks != expectedCallbacks ||
        after.executed - before.executed != expectedCallbacks ||
        after.ownershipViolations != before.ownershipViolations)
    {
        return 10;
    }

    std::printf(
        "RESULT arm=%s workers=%zu packets=%zu capacity=%zu passes=%zu cycles=%zu "
        "pause_a_ns=%llu pause_b_ns=%llu total_a_ns=%llu total_b_ns=%llu "
        "callbacks=%llu checksum=%llu submitted=%llu executed=%llu deferred_submitted=%llu "
        "deferred_published=%llu deferred_executed=%llu flips=%llu empty_flips=%llu "
        "ownership_violations=%llu\n",
        argv[1],
        workers,
        packets,
        capacity,
        passes,
        measuredCycles,
        static_cast<unsigned long long>(pauseA),
        static_cast<unsigned long long>(pauseB),
        static_cast<unsigned long long>(totalA),
        static_cast<unsigned long long>(totalB),
        static_cast<unsigned long long>(callbacks),
        static_cast<unsigned long long>(state.checksum.load(std::memory_order_relaxed)),
        static_cast<unsigned long long>(after.submitted - before.submitted),
        static_cast<unsigned long long>(after.executed - before.executed),
        static_cast<unsigned long long>(
            after.deferredSubmitted - before.deferredSubmitted),
        static_cast<unsigned long long>(
            after.deferredPublished - before.deferredPublished),
        static_cast<unsigned long long>(
            after.deferredExecuted - before.deferredExecuted),
        static_cast<unsigned long long>(after.deferredFlips - before.deferredFlips),
        static_cast<unsigned long long>(
            after.deferredEmptyFlips - before.deferredEmptyFlips),
        static_cast<unsigned long long>(
            after.ownershipViolations - before.ownershipViolations));
    return 0;
}
