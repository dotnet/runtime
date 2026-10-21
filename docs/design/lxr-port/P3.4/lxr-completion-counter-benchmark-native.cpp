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
#include <thread>
#include <vector>

namespace
{
    struct BenchmarkState;

    struct PacketSlot
    {
        PacketSlot(
            uint64_t id,
            size_t index,
            size_t referenceCapacity,
            BenchmarkState* owner);

        std::vector<uintptr_t> references;
        LxrWorkPacket packet;
        size_t index;
        BenchmarkState* owner;
    };

    struct BenchmarkState
    {
        BenchmarkState(size_t workers, size_t packetCount, size_t referenceCapacity)
            : packetCount(packetCount)
        {
            packets.reserve(packetCount);
            for (size_t packetIndex = 0; packetIndex < packetCount; packetIndex++)
            {
                packets.push_back(std::make_unique<PacketSlot>(
                    packetIndex + 1,
                    packetIndex,
                    referenceCapacity,
                    this));
                for (size_t index = 0; index < referenceCapacity; index++)
                {
                    packets.back()->references[index] = static_cast<uintptr_t>(
                        (packetIndex + 1) * UINT64_C(1'315'423'911) +
                        index * 17);
                }
            }
            initialized = scheduler.Initialize(
                workers,
                true,
                true,
                DecrementsDrained,
                OverallDrained,
                this) == LxrWorkSchedulerResult::Success;
        }

        ~BenchmarkState()
        {
            scheduler.Shutdown();
        }

        static LxrCompletionCallbackResult DecrementsDrained(
            LxrMatureSweepContinuation*,
            void* context)
        {
            BenchmarkState* state = static_cast<BenchmarkState*>(context);
            state->decrementCallbacks.fetch_add(1, std::memory_order_relaxed);
            return LxrCompletionCallbackResult::Success;
        }

        static LxrCompletionCallbackResult OverallDrained(
            uint64_t,
            void* context)
        {
            BenchmarkState* state = static_cast<BenchmarkState*>(context);
            state->overallCallbacks.fetch_add(1, std::memory_order_relaxed);
            return LxrCompletionCallbackResult::Success;
        }

        static void Execute(LxrWorkPacket* packet, size_t, void* context)
        {
            PacketSlot* slot = static_cast<PacketSlot*>(context);
            BenchmarkState* state = slot->owner;
            size_t passes = state->callbackPasses.load(std::memory_order_relaxed);
            uint64_t value = packet->GetId() ^ UINT64_C(0x9e3779b97f4a7c15);
            for (size_t pass = 0; pass < passes; pass++)
            {
                for (size_t index = 0; index < packet->GetReferenceCount(); index++)
                {
                    value ^= static_cast<uint64_t>(
                        packet->GetReferences()[index]) +
                        UINT64_C(0x9e3779b97f4a7c15) +
                        (value << 6) +
                        (value >> 2);
                }
            }
            state->checksum.fetch_add(value, std::memory_order_relaxed);
            size_t arrival =
                state->arrivalIndex.fetch_add(1, std::memory_order_acq_rel);
            if (arrival >=
                state->allowCount.load(std::memory_order_acquire))
            {
                while (!state->releaseBacklog.load(std::memory_order_acquire))
                {
                    std::this_thread::yield();
                }
            }
            state->sampleCallbacks.fetch_add(1, std::memory_order_release);
            state->callbacks.fetch_add(1, std::memory_order_release);
        }

        LxrWorkScheduler scheduler;
        std::vector<std::unique_ptr<PacketSlot>> packets;
        size_t packetCount;
        bool initialized = false;
        std::atomic<size_t> callbackPasses{1};
        std::atomic<size_t> allowCount{0};
        std::atomic<size_t> arrivalIndex{0};
        std::atomic<bool> releaseBacklog{false};
        std::atomic<uint64_t> sampleCallbacks{0};
        std::atomic<uint64_t> callbacks{0};
        std::atomic<uint64_t> checksum{0};
        std::atomic<uint64_t> decrementCallbacks{0};
        std::atomic<uint64_t> overallCallbacks{0};
    };

    PacketSlot::PacketSlot(
        uint64_t id,
        size_t packetIndex,
        size_t referenceCapacity,
        BenchmarkState* ownerState)
        : references(referenceCapacity),
          packet(
              id,
              LxrWorkBucketStage::Unconstrained,
              references.data(),
              references.size(),
              BenchmarkState::Execute,
              this),
          index(packetIndex),
          owner(ownerState)
    {
    }

    struct Sample
    {
        uint64_t drainNanoseconds;
        uint64_t nextPauseNanoseconds;
        uint64_t observedBacklog;
    };

    bool WaitForBacklog(
        BenchmarkState& state,
        bool tracked,
        uint64_t generation,
        size_t expectedBacklog,
        uint64_t* observed)
    {
        std::chrono::steady_clock::time_point deadline =
            std::chrono::steady_clock::now() + std::chrono::seconds(30);
        while (std::chrono::steady_clock::now() < deadline)
        {
            if (tracked)
            {
                LxrCompletionSnapshot snapshot{};
                if (state.scheduler.GetCompletionSnapshot(
                        generation,
                        &snapshot) == LxrWorkSchedulerResult::Success)
                {
                    *observed = snapshot.decrementOutstanding;
                    if (*observed == expectedBacklog)
                    {
                        return true;
                    }
                }
            }
            else
            {
                uint64_t completed =
                    state.sampleCallbacks.load(std::memory_order_acquire);
                *observed = state.packetCount - completed;
                if (*observed == expectedBacklog)
                {
                    return true;
                }
            }
            std::this_thread::yield();
        }
        return false;
    }

    bool PreparePackets(BenchmarkState& state)
    {
        for (const std::unique_ptr<PacketSlot>& slot : state.packets)
        {
            if (slot->packet.GetState() == LxrWorkPacketState::Completed &&
                state.scheduler.RecycleCompletedPacket(&slot->packet) !=
                    LxrWorkSchedulerResult::Success)
            {
                return false;
            }
            if (slot->packet.GetState() != LxrWorkPacketState::CallerOwned)
            {
                return false;
            }
        }
        return true;
    }

    bool DrainEmptyGeneration(BenchmarkState& state, uint64_t generation)
    {
        return state.scheduler.ResumeConcurrentWork() ==
                LxrWorkSchedulerResult::Success &&
            state.scheduler.PublishDeferredConcurrentWork(generation) ==
                LxrWorkSchedulerResult::Success &&
            state.scheduler.WaitForCompletion(generation, 30'000) ==
                LxrWorkSchedulerResult::Success &&
            state.scheduler.WaitForIdle(30'000) ==
                LxrWorkSchedulerResult::Success &&
            state.scheduler.ResetCompletedCycle() ==
                LxrWorkSchedulerResult::Success;
    }

    bool RunSample(
        BenchmarkState& state,
        bool tracked,
        size_t callbackPasses,
        size_t backlog,
        Sample* sample)
    {
        if (!PreparePackets(state))
        {
            return false;
        }

        state.callbackPasses.store(callbackPasses, std::memory_order_relaxed);
        state.allowCount.store(
            state.packetCount - backlog,
            std::memory_order_release);
        state.releaseBacklog.store(false, std::memory_order_release);
        state.arrivalIndex.store(0, std::memory_order_release);
        state.sampleCallbacks.store(0, std::memory_order_release);
        for (const std::unique_ptr<PacketSlot>& slot : state.packets)
        {
            LxrWorkSchedulerResult result = tracked
                ? state.scheduler.SubmitDeferredConcurrentTracked(
                    &slot->packet,
                    LxrCompletionWorkKind::Decrement,
                    LxrDeferredWorkPriority::Prioritized)
                : state.scheduler.SubmitDeferredConcurrent(
                    &slot->packet,
                    LxrDeferredWorkPriority::Prioritized);
            if (result != LxrWorkSchedulerResult::Success)
            {
                return false;
            }
        }

        if (state.scheduler.BeginStwCycle() != LxrWorkSchedulerResult::Success ||
            state.scheduler.WaitForCycleCompletion(30'000) !=
                LxrWorkSchedulerResult::Success)
        {
            return false;
        }
        uint64_t generation = state.scheduler.GetGeneration();
        if (state.scheduler.ResumeConcurrentWork() !=
                LxrWorkSchedulerResult::Success ||
            state.scheduler.PublishDeferredConcurrentWork(generation) !=
                LxrWorkSchedulerResult::Success)
        {
            return false;
        }

        uint64_t observed = 0;
        if (!WaitForBacklog(
                state,
                tracked,
                generation,
                backlog,
                &observed))
        {
            return false;
        }

        std::chrono::steady_clock::time_point drainStart =
            std::chrono::steady_clock::now();
        state.releaseBacklog.store(true, std::memory_order_release);
        if ((tracked &&
                state.scheduler.WaitForCompletion(generation, 30'000) !=
                    LxrWorkSchedulerResult::Success) ||
            state.scheduler.WaitForIdle(30'000) !=
                LxrWorkSchedulerResult::Success)
        {
            return false;
        }
        std::chrono::steady_clock::time_point drainEnd =
            std::chrono::steady_clock::now();
        if (state.scheduler.ResetCompletedCycle() !=
            LxrWorkSchedulerResult::Success)
        {
            return false;
        }

        std::chrono::steady_clock::time_point pauseStart =
            std::chrono::steady_clock::now();
        if (state.scheduler.BeginStwCycle() != LxrWorkSchedulerResult::Success ||
            state.scheduler.WaitForCycleCompletion(30'000) !=
                LxrWorkSchedulerResult::Success)
        {
            return false;
        }
        std::chrono::steady_clock::time_point pauseEnd =
            std::chrono::steady_clock::now();
        uint64_t emptyGeneration = state.scheduler.GetGeneration();
        if (!DrainEmptyGeneration(state, emptyGeneration))
        {
            return false;
        }

        sample->drainNanoseconds = static_cast<uint64_t>(
            std::chrono::duration_cast<std::chrono::nanoseconds>(
                drainEnd - drainStart).count());
        sample->nextPauseNanoseconds = static_cast<uint64_t>(
            std::chrono::duration_cast<std::chrono::nanoseconds>(
                pauseEnd - pauseStart).count());
        sample->observedBacklog = observed;
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
    if (argc != 9)
    {
        std::fprintf(
            stderr,
            "usage: benchmark <tracked|untracked|aa> <workers> <packets> "
            "<capacity> <backlog> <passes> <warmup> <measured>\n");
        return 2;
    }

    std::string_view policy(argv[1]);
    bool tracked = policy != "untracked";
    bool aa = policy == "aa";
    if (policy != "tracked" && policy != "untracked" && !aa)
    {
        return 3;
    }

    size_t workers;
    size_t packetCount;
    size_t capacity;
    size_t backlog;
    size_t passes;
    size_t warmup;
    size_t measured;
    if (!ParseSize(argv[2], &workers) ||
        !ParseSize(argv[3], &packetCount) ||
        !ParseSize(argv[4], &capacity) ||
        !ParseSize(argv[5], &backlog, true) ||
        !ParseSize(argv[6], &passes, true) ||
        !ParseSize(argv[7], &warmup, true) ||
        !ParseSize(argv[8], &measured) ||
        backlog > packetCount ||
        !LxrIsSupportedReferencePacketCapacity(capacity))
    {
        return 4;
    }

    BenchmarkState state(workers, packetCount, capacity);
    if (!state.initialized)
    {
        return 5;
    }

    Sample sample{};
    for (size_t index = 0; index < warmup; index++)
    {
        if (!RunSample(state, tracked, passes, backlog, &sample))
        {
            return 6;
        }
    }

    state.callbacks.store(0, std::memory_order_relaxed);
    state.checksum.store(0, std::memory_order_relaxed);
    state.decrementCallbacks.store(0, std::memory_order_relaxed);
    state.overallCallbacks.store(0, std::memory_order_relaxed);
    LxrWorkSchedulerMetrics before = state.scheduler.GetMetrics();
    uint64_t drainA = 0;
    uint64_t drainB = 0;
    uint64_t pauseA = 0;
    uint64_t pauseB = 0;
    uint64_t observedMinimum = UINT64_MAX;
    uint64_t observedMaximum = 0;
    for (size_t index = 0; index < measured; index++)
    {
        if (aa)
        {
            Sample first{};
            Sample second{};
            if (!RunSample(state, true, passes, backlog, &first) ||
                !RunSample(state, true, passes, backlog, &second))
            {
                return 7;
            }
            if ((index & 1) == 0)
            {
                drainA += first.drainNanoseconds;
                pauseA += first.nextPauseNanoseconds;
                drainB += second.drainNanoseconds;
                pauseB += second.nextPauseNanoseconds;
            }
            else
            {
                drainB += first.drainNanoseconds;
                pauseB += first.nextPauseNanoseconds;
                drainA += second.drainNanoseconds;
                pauseA += second.nextPauseNanoseconds;
            }
            observedMinimum = std::min(
                observedMinimum,
                std::min(first.observedBacklog, second.observedBacklog));
            observedMaximum = std::max(
                observedMaximum,
                std::max(first.observedBacklog, second.observedBacklog));
        }
        else
        {
            if (!RunSample(state, tracked, passes, backlog, &sample))
            {
                return 8;
            }
            drainA += sample.drainNanoseconds;
            pauseA += sample.nextPauseNanoseconds;
            observedMinimum =
                std::min(observedMinimum, sample.observedBacklog);
            observedMaximum =
                std::max(observedMaximum, sample.observedBacklog);
        }
    }

    LxrWorkSchedulerMetrics after = state.scheduler.GetMetrics();
    uint64_t multiplier = aa ? 2 : 1;
    uint64_t expectedCallbacks =
        measured * packetCount * multiplier;
    uint64_t callbacks = state.callbacks.load(std::memory_order_relaxed);
    if (callbacks != expectedCallbacks ||
        after.executed - before.executed != expectedCallbacks ||
        after.ownershipViolations != before.ownershipViolations ||
        observedMinimum != backlog ||
        observedMaximum != backlog)
    {
        return 9;
    }

    std::printf(
        "RESULT policy=%s workers=%zu packets=%zu capacity=%zu backlog=%zu "
        "passes=%zu measured=%zu drain_a_ns=%llu drain_b_ns=%llu "
        "pause_a_ns=%llu pause_b_ns=%llu observed_min=%llu observed_max=%llu "
        "callbacks=%llu checksum=%llu submitted=%llu executed=%llu stolen=%llu "
        "decrement_callbacks=%llu overall_callbacks=%llu "
        "ownership_violations=%llu\n",
        argv[1],
        workers,
        packetCount,
        capacity,
        backlog,
        passes,
        measured,
        static_cast<unsigned long long>(drainA),
        static_cast<unsigned long long>(drainB),
        static_cast<unsigned long long>(pauseA),
        static_cast<unsigned long long>(pauseB),
        static_cast<unsigned long long>(observedMinimum),
        static_cast<unsigned long long>(observedMaximum),
        static_cast<unsigned long long>(callbacks),
        static_cast<unsigned long long>(
            state.checksum.load(std::memory_order_relaxed)),
        static_cast<unsigned long long>(after.submitted - before.submitted),
        static_cast<unsigned long long>(after.executed - before.executed),
        static_cast<unsigned long long>(after.stolen - before.stolen),
        static_cast<unsigned long long>(
            state.decrementCallbacks.load(std::memory_order_relaxed)),
        static_cast<unsigned long long>(
            state.overallCallbacks.load(std::memory_order_relaxed)),
        static_cast<unsigned long long>(
            after.ownershipViolations - before.ownershipViolations));
    return 0;
}
