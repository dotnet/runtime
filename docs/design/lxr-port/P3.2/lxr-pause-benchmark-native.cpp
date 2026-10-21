// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/lxr_pause_coordinator.h"

#include <atomic>
#include <chrono>
#include <cstdint>
#include <memory>
#include <new>
#include <thread>
#include <vector>

#ifdef _MSC_VER
#define P32_EXPORT extern "C" __declspec(dllexport)
#else
#define P32_EXPORT extern "C" __attribute__((visibility("default")))
#endif

namespace
{
    enum class BenchmarkPolicy : uint32_t
    {
        Scheduled = 1,
        Null = 2,
        Injected = 3,
        SameCode = 4,
    };

    struct BenchmarkState;

    struct PacketContext
    {
        BenchmarkState* state;
        LxrWorkBucketStage stage;
    };

    struct P32BenchmarkMetrics
    {
        uint64_t schedules;
        uint64_t suspends;
        uint64_t resumes;
        uint64_t kindCounts[static_cast<size_t>(LxrPauseKind::Count)];
        uint64_t aaNanosecondsA;
        uint64_t aaNanosecondsB;
        uint64_t aaCountA;
        uint64_t aaCountB;
    };

    struct BenchmarkState
    {
        BenchmarkState(size_t workerCount, BenchmarkPolicy selectedPolicy, uint32_t injected)
            : policy(selectedPolicy),
              injectedMicroseconds(injected),
              nextPacketId(1),
              schedules(0),
              suspends(0),
              resumes(0),
              aaNanosecondsA(0),
              aaNanosecondsB(0),
              aaCountA(0),
              aaCountB(0),
              injectCurrent(false),
              initialized(false)
        {
            for (std::atomic<uint64_t>& count : kindCounts)
            {
                count.store(0, std::memory_order_relaxed);
            }

            if (scheduler.Initialize(workerCount) != LxrWorkSchedulerResult::Success)
            {
                return;
            }

            LxrPauseRuntimeAdapter adapter{
                Suspend,
                Resume,
                this,
                false,
            };
            initialized =
                coordinator.Initialize(&scheduler, adapter) ==
                LxrPauseCoordinatorResult::Success;
        }

        ~BenchmarkState()
        {
            scheduler.Shutdown();
        }

        static void Suspend(void* context)
        {
            static_cast<BenchmarkState*>(context)->suspends.fetch_add(
                1,
                std::memory_order_relaxed);
        }

        static void Resume(bool, void* context)
        {
            static_cast<BenchmarkState*>(context)->resumes.fetch_add(
                1,
                std::memory_order_relaxed);
        }

        static void ExecutePacket(LxrWorkPacket*, size_t, void* context)
        {
            PacketContext* packet = static_cast<PacketContext*>(context);
            uint32_t delayMicroseconds = 250;
            if (packet->state->injectCurrent.load(std::memory_order_relaxed) &&
                packet->stage == LxrWorkBucketStage::Initial)
            {
                delayMicroseconds += packet->state->injectedMicroseconds;
            }
            std::chrono::steady_clock::time_point deadline =
                std::chrono::steady_clock::now() +
                std::chrono::microseconds(delayMicroseconds);
            while (std::chrono::steady_clock::now() < deadline)
            {
                std::atomic_signal_fence(std::memory_order_seq_cst);
            }
        }

        bool RunOnce(
            LxrPauseKind kind,
            bool injectDuration,
            uint64_t* durationNanoseconds)
        {
            injectCurrent.store(injectDuration, std::memory_order_relaxed);
            LxrPauseKindMetrics before = coordinator.GetMetrics(kind);
            if (coordinator.PreparePause(kind) != LxrPauseCoordinatorResult::Success)
            {
                return Fail();
            }

            LxrPauseSchedule schedule{};
            if (!LxrTryGetPauseSchedule(kind, &schedule))
            {
                return Fail();
            }

            static uintptr_t References[] = {1, 2, 3, 4};
            std::vector<std::unique_ptr<PacketContext>> contexts;
            std::vector<std::unique_ptr<LxrWorkPacket>> packets;
            for (size_t stageIndex = static_cast<size_t>(LxrFirstStwStage);
                 stageIndex < static_cast<size_t>(LxrWorkBucketStage::Count);
                 stageIndex++)
            {
                LxrWorkBucketStage stage = static_cast<LxrWorkBucketStage>(stageIndex);
                if (!LxrIsPauseStageEnabled(schedule, stage))
                {
                    continue;
                }

                contexts.push_back(std::make_unique<PacketContext>(
                    PacketContext{this, stage}));
                packets.push_back(std::make_unique<LxrWorkPacket>(
                    nextPacketId++,
                    stage,
                    References,
                    std::size(References),
                    ExecutePacket,
                    contexts.back().get()));
                if (scheduler.Submit(packets.back().get()) !=
                    LxrWorkSchedulerResult::Success)
                {
                    return Fail();
                }
            }

            if (coordinator.StartPause(30'000) != LxrPauseCoordinatorResult::Success)
            {
                return Fail();
            }
            if (coordinator.CompletePause(30'000) !=
                LxrPauseCoordinatorResult::Success)
            {
                return Fail();
            }

            LxrPauseKindMetrics after = coordinator.GetMetrics(kind);
            *durationNanoseconds = after.totalNanoseconds - before.totalNanoseconds;
            schedules.fetch_add(1, std::memory_order_relaxed);
            kindCounts[static_cast<size_t>(kind)].fetch_add(
                1,
                std::memory_order_relaxed);
            return true;
        }

        bool Fail()
        {
            if (coordinator.GetState() == LxrPauseCoordinatorState::Active)
            {
                coordinator.AbortPause();
            }
            else
            {
                scheduler.Shutdown();
            }
            initialized = false;
            return false;
        }

        LxrWorkScheduler scheduler;
        LxrPauseCoordinator coordinator;
        BenchmarkPolicy policy;
        uint32_t injectedMicroseconds;
        uint64_t nextPacketId;
        std::atomic<uint64_t> schedules;
        std::atomic<uint64_t> suspends;
        std::atomic<uint64_t> resumes;
        std::array<std::atomic<uint64_t>, static_cast<size_t>(LxrPauseKind::Count)> kindCounts;
        std::atomic<uint64_t> aaNanosecondsA;
        std::atomic<uint64_t> aaNanosecondsB;
        std::atomic<uint64_t> aaCountA;
        std::atomic<uint64_t> aaCountB;
        std::atomic<bool> injectCurrent;
        bool initialized;
    };
}

P32_EXPORT void* P32_Benchmark_Create(
    size_t workerCount,
    uint32_t policy,
    uint32_t injectedMicroseconds)
{
    if (workerCount == 0 ||
        policy < static_cast<uint32_t>(BenchmarkPolicy::Scheduled) ||
        policy > static_cast<uint32_t>(BenchmarkPolicy::SameCode))
    {
        return nullptr;
    }

    BenchmarkState* state = new (std::nothrow) BenchmarkState(
        workerCount,
        static_cast<BenchmarkPolicy>(policy),
        injectedMicroseconds);
    if (state == nullptr || !state->initialized)
    {
        delete state;
        return nullptr;
    }
    return state;
}

P32_EXPORT uint64_t P32_Benchmark_Run(
    void* handle,
    uint32_t pauseKind,
    uint64_t sequence,
    int32_t* status)
{
    if (status == nullptr)
    {
        return 0;
    }
    *status = 0;
    BenchmarkState* state = static_cast<BenchmarkState*>(handle);
    if (state == nullptr ||
        !state->initialized ||
        pauseKind >= static_cast<uint32_t>(LxrPauseKind::Count))
    {
        *status = 1;
        return 0;
    }

    if (state->policy == BenchmarkPolicy::Null)
    {
        return 0;
    }

    LxrPauseKind kind = static_cast<LxrPauseKind>(pauseKind);
    bool injectedPolicy = state->policy == BenchmarkPolicy::Injected;
    bool firstInjected = injectedPolicy && ((sequence & 1) != 0);
    uint64_t first = 0;
    if (!state->RunOnce(kind, firstInjected, &first))
    {
        *status = 2;
        return 0;
    }

    if (state->policy == BenchmarkPolicy::Scheduled)
    {
        return first;
    }

    uint64_t second = 0;
    if (!state->RunOnce(
        kind,
        injectedPolicy && !firstInjected,
        &second))
    {
        *status = 3;
        return 0;
    }

    if (state->policy == BenchmarkPolicy::Injected)
    {
        uint64_t baseline = firstInjected ? second : first;
        uint64_t injected = firstInjected ? first : second;
        return injected > baseline ? injected - baseline : 0;
    }

    bool firstIsA = (sequence & 1) == 0;
    state->aaNanosecondsA.fetch_add(
        firstIsA ? first : second,
        std::memory_order_relaxed);
    state->aaNanosecondsB.fetch_add(
        firstIsA ? second : first,
        std::memory_order_relaxed);
    state->aaCountA.fetch_add(1, std::memory_order_relaxed);
    state->aaCountB.fetch_add(1, std::memory_order_relaxed);
    return (first + second) / 2;
}

P32_EXPORT int32_t P32_Benchmark_GetMetrics(
    void* handle,
    P32BenchmarkMetrics* metrics)
{
    BenchmarkState* state = static_cast<BenchmarkState*>(handle);
    if (state == nullptr || metrics == nullptr)
    {
        return 1;
    }

    metrics->schedules = state->schedules.load(std::memory_order_relaxed);
    metrics->suspends = state->suspends.load(std::memory_order_relaxed);
    metrics->resumes = state->resumes.load(std::memory_order_relaxed);
    for (size_t index = 0; index < static_cast<size_t>(LxrPauseKind::Count); index++)
    {
        metrics->kindCounts[index] =
            state->kindCounts[index].load(std::memory_order_relaxed);
    }
    metrics->aaNanosecondsA =
        state->aaNanosecondsA.load(std::memory_order_relaxed);
    metrics->aaNanosecondsB =
        state->aaNanosecondsB.load(std::memory_order_relaxed);
    metrics->aaCountA = state->aaCountA.load(std::memory_order_relaxed);
    metrics->aaCountB = state->aaCountB.load(std::memory_order_relaxed);
    return 0;
}

P32_EXPORT void P32_Benchmark_Destroy(void* handle)
{
    delete static_cast<BenchmarkState*>(handle);
}
