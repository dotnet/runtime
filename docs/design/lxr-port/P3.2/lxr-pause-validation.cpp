// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/lxr_pause_coordinator.h"

#include <array>
#include <atomic>
#include <chrono>
#include <condition_variable>
#include <cstdint>
#include <cstdio>
#include <memory>
#include <mutex>
#include <string_view>
#include <thread>
#include <vector>

namespace
{
    constexpr uint32_t TimeoutMilliseconds = 30'000;
    constexpr size_t StageCount = static_cast<size_t>(LxrWorkBucketStage::Count);
    constexpr size_t PauseKindCount = static_cast<size_t>(LxrPauseKind::Count);

    int Checks = 0;
    int Failures = 0;

    bool Check(bool condition, const char* message)
    {
        Checks++;
        if (!condition)
        {
            Failures++;
            std::fprintf(stderr, "FAIL: %s\n", message);
        }
        return condition;
    }

    constexpr uint16_t StageBit(LxrWorkBucketStage stage)
    {
        return static_cast<uint16_t>(UINT16_C(1) << static_cast<uint8_t>(stage));
    }

    struct AdapterState
    {
        std::atomic<uint32_t> suspends;
        std::atomic<uint32_t> resumes;
        std::atomic<bool> lastFinishedGc;
    };

    void Suspend(void* context)
    {
        static_cast<AdapterState*>(context)->suspends.fetch_add(
            1,
            std::memory_order_relaxed);
    }

    void Resume(bool finishedGc, void* context)
    {
        AdapterState* state = static_cast<AdapterState*>(context);
        state->lastFinishedGc.store(finishedGc, std::memory_order_relaxed);
        state->resumes.fetch_add(1, std::memory_order_relaxed);
    }

    LxrPauseRuntimeAdapter NewAdapter(AdapterState* state)
    {
        return {Suspend, Resume, state, false};
    }

    struct ExecutionState
    {
        std::array<std::atomic<uint32_t>, StageCount> executions{};
        std::mutex lock;
        std::condition_variable condition;
        bool blockingStarted = false;
        bool releaseBlocking = false;
        std::atomic<bool> suspendCalled;
        std::atomic<bool> resumableExecutedBeforeSuspend;
    };

    void Execute(LxrWorkPacket* packet, size_t, void* context)
    {
        ExecutionState* state = static_cast<ExecutionState*>(context);
        state->executions[static_cast<size_t>(packet->GetStage())].fetch_add(
            1,
            std::memory_order_relaxed);
    }

    void ExecuteBlocking(LxrWorkPacket* packet, size_t workerIndex, void* context)
    {
        Execute(packet, workerIndex, context);
        ExecutionState* state = static_cast<ExecutionState*>(context);
        std::unique_lock<std::mutex> guard(state->lock);
        state->blockingStarted = true;
        state->condition.notify_all();
        state->condition.wait(guard, [state]() { return state->releaseBlocking; });
    }

    void ExecuteResumableQueued(LxrWorkPacket* packet, size_t workerIndex, void* context)
    {
        ExecutionState* state = static_cast<ExecutionState*>(context);
        if (!state->suspendCalled.load(std::memory_order_acquire))
        {
            state->resumableExecutedBeforeSuspend.store(
                true,
                std::memory_order_release);
        }
        Execute(packet, workerIndex, context);
    }

    bool WaitForBlockingStart(ExecutionState* state)
    {
        std::unique_lock<std::mutex> guard(state->lock);
        return state->condition.wait_for(
            guard,
            std::chrono::milliseconds(TimeoutMilliseconds),
            [state]() { return state->blockingStarted; });
    }

    void ReleaseBlocking(ExecutionState* state)
    {
        std::lock_guard<std::mutex> guard(state->lock);
        state->releaseBlocking = true;
        state->condition.notify_all();
    }

    bool WaitForResumableInterruption(LxrWorkScheduler* scheduler)
    {
        std::chrono::steady_clock::time_point deadline =
            std::chrono::steady_clock::now() + std::chrono::seconds(1);
        while (std::chrono::steady_clock::now() < deadline)
        {
            if (!scheduler->IsStageEnabled(
                LxrWorkBucketStage::ConcurrentResumable))
            {
                return true;
            }
            std::this_thread::sleep_for(std::chrono::milliseconds(1));
        }
        return false;
    }

    std::unique_ptr<LxrWorkPacket> NewPacket(
        uint64_t id,
        LxrWorkBucketStage stage,
        LxrWorkPacket::ExecuteCallback callback,
        void* state)
    {
        static uintptr_t References[] = {1, 2, 3, 4};
        return std::make_unique<LxrWorkPacket>(
            id,
            stage,
            References,
            std::size(References),
            callback,
            state);
    }

    bool ValidateTaxonomyAndSchedules()
    {
        constexpr const char* ExpectedNames[] = {
            "Full",
            "RefCount",
            "InitialMark",
            "FinalMark",
        };
        constexpr uint16_t Common =
            StageBit(LxrWorkBucketStage::FinishConcurrentWork) |
            StageBit(LxrWorkBucketStage::Initial) |
            StageBit(LxrWorkBucketStage::Release) |
            StageBit(LxrWorkBucketStage::Final);
        constexpr uint16_t ExpectedMasks[] = {
            Common |
                StageBit(LxrWorkBucketStage::Prepare) |
                StageBit(LxrWorkBucketStage::Closure) |
                StageBit(LxrWorkBucketStage::STWRCDecsAndSweep),
            Common,
            Common | StageBit(LxrWorkBucketStage::Prepare),
            Common |
                StageBit(LxrWorkBucketStage::Prepare) |
                StageBit(LxrWorkBucketStage::Closure),
        };

        bool success = true;
        for (size_t index = 0; index < PauseKindCount; index++)
        {
            LxrPauseKind kind = static_cast<LxrPauseKind>(index);
            LxrPauseSchedule schedule{};
            success &= Check(
                std::string_view(LxrGetPauseKindName(kind)) == ExpectedNames[index],
                "pause name and ordinal");
            success &= Check(
                LxrTryGetPauseSchedule(kind, &schedule),
                "pause schedule exists");
            success &= Check(schedule.kind == kind, "schedule kind identity");
            success &= Check(
                schedule.enabledStageMask == ExpectedMasks[index],
                "exact per-kind stage mask");
        }

        LxrPauseSchedule invalid{};
        success &= Check(
            !LxrTryGetPauseSchedule(LxrPauseKind::Count, &invalid),
            "invalid pause rejected");
        success &= Check(
            !LxrTryGetPauseSchedule(LxrPauseKind::Full, nullptr),
            "null schedule rejected");
        success &= Check(
            std::string_view(LxrGetPauseKindName(LxrPauseKind::Count)) == "Invalid",
            "invalid pause name");
        return success;
    }

    bool ValidatePause(LxrPauseKind kind)
    {
        LxrWorkScheduler scheduler;
        AdapterState adapter{};
        LxrPauseCoordinator coordinator;
        ExecutionState execution{};
        bool success = Check(
            scheduler.Initialize(3) == LxrWorkSchedulerResult::Success,
            "pause scheduler initializes");
        success &= Check(
            coordinator.Initialize(&scheduler, NewAdapter(&adapter)) ==
                LxrPauseCoordinatorResult::Success,
            "pause coordinator initializes");
        success &= Check(
            coordinator.PreparePause(kind) == LxrPauseCoordinatorResult::Success,
            "pause prepares");

        LxrPauseSchedule schedule{};
        LxrTryGetPauseSchedule(kind, &schedule);
        std::vector<std::unique_ptr<LxrWorkPacket>> packets;
        uint64_t id = 1;
        for (size_t index = static_cast<size_t>(LxrFirstStwStage);
             index < StageCount;
             index++)
        {
            LxrWorkBucketStage stage = static_cast<LxrWorkBucketStage>(index);
            bool expectedEnabled = LxrIsPauseStageEnabled(schedule, stage);
            success &= Check(
                scheduler.IsStageEnabled(stage) == expectedEnabled,
                "stage enabled state matches schedule");
            std::unique_ptr<LxrWorkPacket> packet =
                NewPacket(id++, stage, Execute, &execution);
            LxrWorkSchedulerResult submitted = scheduler.Submit(packet.get());
            success &= Check(
                submitted == (expectedEnabled ?
                    LxrWorkSchedulerResult::Success :
                    LxrWorkSchedulerResult::StageDisabled),
                "enabled stage accepts and disabled stage rejects");
            if (expectedEnabled)
            {
                packets.push_back(std::move(packet));
            }
        }

        success &= Check(
            coordinator.StartPause(TimeoutMilliseconds) ==
                LxrPauseCoordinatorResult::Success,
            "pause starts");
        success &= Check(
            scheduler.WaitForCycleCompletion(TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "pause schedule completes");

        constexpr size_t StwStageCount =
            StageCount - static_cast<size_t>(LxrFirstStwStage);
        std::array<LxrWorkBucketStage, StwStageCount> opens{};
        std::array<LxrWorkBucketStage, StwStageCount> completions{};
        success &= Check(
            scheduler.CopyOpenTrace(opens.data(), opens.size()) == StwStageCount,
            "one open per STW stage");
        success &= Check(
            scheduler.CopyCompletionTrace(completions.data(), completions.size()) ==
                StwStageCount,
            "one completion per STW stage");
        for (size_t index = 0; index < StwStageCount; index++)
        {
            LxrWorkBucketStage expected = static_cast<LxrWorkBucketStage>(
                static_cast<size_t>(LxrFirstStwStage) + index);
            success &= Check(opens[index] == expected, "exact open order");
            success &= Check(completions[index] == expected, "exact completion order");
            success &= Check(scheduler.IsStageCompleted(expected), "stage completed");
            uint32_t executions =
                execution.executions[static_cast<size_t>(expected)].load(
                    std::memory_order_relaxed);
            success &= Check(
                executions == (LxrIsPauseStageEnabled(schedule, expected) ? 1U : 0U),
                "only enabled stage executes");
        }

        success &= Check(
            coordinator.CompletePause(TimeoutMilliseconds) ==
                LxrPauseCoordinatorResult::Success,
            "pause completes and resets");
        success &= Check(
            coordinator.GetState() == LxrPauseCoordinatorState::Idle,
            "coordinator returns idle");
        success &= Check(
            coordinator.GetCurrentPauseKind() == LxrPauseKind::Count,
            "pause kind clears on reset");
        success &= Check(
            adapter.suspends.load(std::memory_order_relaxed) == 1 &&
                adapter.resumes.load(std::memory_order_relaxed) == 1,
            "one suspend and one resume");
        success &= Check(
            coordinator.GetMetrics(kind).count == 1,
            "per-kind count recorded");
        for (size_t index = static_cast<size_t>(LxrFirstStwStage);
             index < StageCount;
             index++)
        {
            LxrWorkBucketStage stage = static_cast<LxrWorkBucketStage>(index);
            success &= Check(scheduler.IsStageEnabled(stage), "reset enables STW stage");
            success &= Check(!scheduler.IsStageOpen(stage), "reset closes STW stage");
            success &= Check(!scheduler.IsStageCompleted(stage), "reset clears completion");
        }

        scheduler.Shutdown();
        return success;
    }

    bool ValidateIllegalTransitions()
    {
        LxrWorkScheduler scheduler;
        AdapterState adapter{};
        LxrPauseCoordinator coordinator;
        bool success = Check(
            coordinator.PreparePause(LxrPauseKind::Full) ==
                LxrPauseCoordinatorResult::NotInitialized,
            "prepare before initialization rejected");
        success &= Check(
            coordinator.StartPause(1) == LxrPauseCoordinatorResult::NotInitialized,
            "start before initialization rejected");
        success &= Check(
            coordinator.CompletePause(1) == LxrPauseCoordinatorResult::NotInitialized,
            "complete before initialization rejected");
        success &= Check(
            scheduler.Initialize(2) == LxrWorkSchedulerResult::Success,
            "illegal transition scheduler initializes");
        success &= Check(
            coordinator.Initialize(&scheduler, NewAdapter(&adapter)) ==
                LxrPauseCoordinatorResult::Success,
            "illegal transition coordinator initializes");
        success &= Check(
            coordinator.Initialize(&scheduler, NewAdapter(&adapter)) ==
                LxrPauseCoordinatorResult::AlreadyInitialized,
            "duplicate initialization rejected");
        success &= Check(
            coordinator.StartPause(1) == LxrPauseCoordinatorResult::InvalidState,
            "start without prepare rejected");
        success &= Check(
            coordinator.PreparePause(LxrPauseKind::Count) ==
                LxrPauseCoordinatorResult::InvalidArgument,
            "invalid kind rejected");
        success &= Check(
            coordinator.PreparePause(LxrPauseKind::RefCount) ==
                LxrPauseCoordinatorResult::Success,
            "valid prepare accepted");
        success &= Check(
            coordinator.PreparePause(LxrPauseKind::Full) ==
                LxrPauseCoordinatorResult::InvalidState,
            "duplicate prepare rejected");
        success &= Check(
            coordinator.CompletePause(1) ==
                LxrPauseCoordinatorResult::InvalidState,
            "complete before start rejected");
        success &= Check(
            !scheduler.IsStageEnabled(LxrWorkBucketStage::Prepare) &&
                !scheduler.IsStageEnabled(LxrWorkBucketStage::Closure) &&
                !scheduler.IsStageEnabled(LxrWorkBucketStage::STWRCDecsAndSweep),
            "RefCount disables all unnecessary stages");
        scheduler.Shutdown();
        return success;
    }

    bool ValidateInterruptionAndResume()
    {
        struct InterruptionAdapterState
        {
            AdapterState adapter;
            ExecutionState* execution;
        };
        auto suspend = [](void* context)
        {
            InterruptionAdapterState* state =
                static_cast<InterruptionAdapterState*>(context);
            state->adapter.suspends.fetch_add(1, std::memory_order_relaxed);
            state->execution->suspendCalled.store(true, std::memory_order_release);
        };
        auto resume = [](bool finishedGc, void* context)
        {
            InterruptionAdapterState* state =
                static_cast<InterruptionAdapterState*>(context);
            state->adapter.lastFinishedGc.store(
                finishedGc,
                std::memory_order_relaxed);
            state->adapter.resumes.fetch_add(1, std::memory_order_relaxed);
        };

        LxrWorkScheduler scheduler;
        LxrPauseCoordinator coordinator;
        ExecutionState execution{};
        InterruptionAdapterState adapter{{}, &execution};
        LxrPauseRuntimeAdapter runtimeAdapter{
            suspend,
            resume,
            &adapter,
            false,
        };
        bool success = Check(
            scheduler.Initialize(1) == LxrWorkSchedulerResult::Success,
            "interruption scheduler initializes");
        success &= Check(
            scheduler.EnableConcurrentStage(LxrWorkBucketStage::ConcurrentResumable) ==
                LxrWorkSchedulerResult::Success,
            "resumable stage enables");

        std::unique_ptr<LxrWorkPacket> blocking = NewPacket(
            100,
            LxrWorkBucketStage::ConcurrentResumable,
            ExecuteBlocking,
            &execution);
        std::unique_ptr<LxrWorkPacket> queued = NewPacket(
            101,
            LxrWorkBucketStage::ConcurrentResumable,
            ExecuteResumableQueued,
            &execution);
        success &= Check(
            scheduler.Submit(blocking.get(), 0) == LxrWorkSchedulerResult::Success,
            "blocking resumable packet submits");
        success &= Check(WaitForBlockingStart(&execution), "resumable packet starts");
        success &= Check(
            scheduler.Submit(queued.get(), 0) == LxrWorkSchedulerResult::Success,
            "queued resumable packet submits");
        success &= Check(
            coordinator.Initialize(&scheduler, runtimeAdapter) ==
                LxrPauseCoordinatorResult::Success &&
                coordinator.PreparePause(LxrPauseKind::Full) ==
                    LxrPauseCoordinatorResult::Success,
            "interruption pause prepares");

        std::atomic<bool> startFinished(false);
        LxrPauseCoordinatorResult startResult = LxrPauseCoordinatorResult::InvalidState;
        std::thread starter(
            [&]()
            {
                startResult = coordinator.StartPause(TimeoutMilliseconds);
                startFinished.store(true, std::memory_order_release);
            });

        success &= Check(
            WaitForResumableInterruption(&scheduler),
            "resumable bucket closes before interruption waits");
        success &= Check(
            !startFinished.load(std::memory_order_acquire),
            "pause waits for executing resumable packet boundary");
        LxrPauseSchedule fullSchedule{};
        LxrTryGetPauseSchedule(LxrPauseKind::Full, &fullSchedule);
        success &= Check(
            scheduler.EnableConcurrentStage(
                LxrWorkBucketStage::ConcurrentResumable) ==
                LxrWorkSchedulerResult::Busy,
            "interruption rejects concurrent re-enable");
        success &= Check(
            scheduler.ResumeConcurrentWork() == LxrWorkSchedulerResult::Busy,
            "interruption rejects broad concurrent resume");
        success &= Check(
            scheduler.ConfigureStwStages(fullSchedule.enabledStageMask) ==
                LxrWorkSchedulerResult::InvalidState,
            "interruption rejects schedule replacement");
        success &= Check(
            execution.executions[
                static_cast<size_t>(LxrWorkBucketStage::ConcurrentResumable)].load(
                    std::memory_order_relaxed) == 1,
            "queued resumable packet does not execute during interruption");
        ReleaseBlocking(&execution);
        starter.join();
        success &= Check(
            startResult == LxrPauseCoordinatorResult::Success,
            "pause starts after packet boundary");
        success &= Check(
            coordinator.CompletePause(TimeoutMilliseconds) ==
                LxrPauseCoordinatorResult::Success,
            "interruption pause completes");
        success &= Check(
            scheduler.WaitForPacket(queued.get(), TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "queued resumable packet executes after resume");
        success &= Check(
            execution.executions[
                static_cast<size_t>(LxrWorkBucketStage::ConcurrentResumable)].load(
                    std::memory_order_relaxed) == 2,
            "resumable packets execute exactly once");
        success &= Check(
            !execution.resumableExecutedBeforeSuspend.load(
                std::memory_order_acquire),
            "queued resumable packet cannot execute before suspension");
        scheduler.Shutdown();
        return success;
    }

    struct DeferredPauseState
    {
        AdapterState adapter{};
        ExecutionState execution{};
        LxrWorkScheduler* scheduler;
        std::atomic<bool> executedBeforeResume{false};
        std::atomic<uint32_t> rejectedEarlyPublications{0};
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        std::atomic<uint32_t> decrementCallbacks{0};
        std::atomic<uint32_t> overallCallbacks{0};
#endif
    };

    void SuspendDeferred(void* context)
    {
        Suspend(&static_cast<DeferredPauseState*>(context)->adapter);
    }

    void ResumeDeferred(bool finishedGc, void* context)
    {
        DeferredPauseState* state = static_cast<DeferredPauseState*>(context);
        if (state->scheduler->PublishDeferredConcurrentWork(
                state->scheduler->GetGeneration()) == LxrWorkSchedulerResult::InvalidState)
        {
            state->rejectedEarlyPublications.fetch_add(1, std::memory_order_relaxed);
        }
        Resume(finishedGc, &state->adapter);
    }

    void ExecuteDeferred(LxrWorkPacket* packet, size_t workerIndex, void* context)
    {
        DeferredPauseState* state = static_cast<DeferredPauseState*>(context);
        if (state->adapter.resumes.load(std::memory_order_acquire) != packet->GetGeneration())
        {
            state->executedBeforeResume.store(true, std::memory_order_release);
        }
        ExecuteBlocking(packet, workerIndex, &state->execution);
    }

#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
    LxrCompletionCallbackResult DecrementsDrained(
        LxrMatureSweepContinuation*, void* context)
    {
        static_cast<DeferredPauseState*>(context)->decrementCallbacks.fetch_add(
            1, std::memory_order_relaxed);
        return LxrCompletionCallbackResult::Success;
    }

    LxrCompletionCallbackResult OverallDrained(uint64_t, void* context)
    {
        static_cast<DeferredPauseState*>(context)->overallCallbacks.fetch_add(
            1, std::memory_order_relaxed);
        return LxrCompletionCallbackResult::Success;
    }
#endif

    bool ValidateDeferredPause()
    {
        LxrWorkScheduler scheduler;
        DeferredPauseState state{};
        state.scheduler = &scheduler;
        bool success = Check(
            scheduler.Initialize(
                2, true
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
                , true, DecrementsDrained, OverallDrained, &state
#endif
                ) == LxrWorkSchedulerResult::Success,
            "combined deferred scheduler initializes");
        LxrPauseCoordinator coordinator;
        LxrPauseRuntimeAdapter adapter{
            SuspendDeferred, ResumeDeferred, &state, false,
        };
        success &= Check(
            coordinator.Initialize(&scheduler, adapter) == LxrPauseCoordinatorResult::Success,
            "combined deferred coordinator initializes");

        for (uint64_t generation = 1; generation <= 3; generation++)
        {
            std::unique_ptr<LxrWorkPacket> packet = NewPacket(
                300 + generation, LxrWorkBucketStage::Unconstrained,
                ExecuteDeferred, &state);
            state.execution.blockingStarted = false;
            state.execution.releaseBlocking = false;
            success &= Check(
                coordinator.PreparePause(LxrPauseKind::RefCount) ==
                    LxrPauseCoordinatorResult::Success,
                "combined pause retires prior generation and prepares");
            success &= Check(
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
                scheduler.SubmitDeferredConcurrentTracked(
                    packet.get(), LxrCompletionWorkKind::Decrement,
                    LxrDeferredWorkPriority::Prioritized)
#else
                scheduler.SubmitDeferredConcurrent(
                    packet.get(), LxrDeferredWorkPriority::Prioritized)
#endif
                    == LxrWorkSchedulerResult::Success,
                "combined pause owns deferred decrement");
            success &= Check(
                coordinator.StartPause(TimeoutMilliseconds) ==
                    LxrPauseCoordinatorResult::Success,
                "combined pause starts");
            success &= Check(
                coordinator.CompletePause(TimeoutMilliseconds) ==
                    LxrPauseCoordinatorResult::Success,
                "combined pause resumes and publishes without waiting for lazy work");
            bool started = WaitForBlockingStart(&state.execution);
            success &= Check(started, "combined decrement executes after publication");
            success &= Check(
                coordinator.PreparePause(LxrPauseKind::RefCount) ==
                    LxrPauseCoordinatorResult::SchedulerFailure,
                "next pause cannot retire executing lazy ownership");
            ReleaseBlocking(&state.execution);
            success &= Check(
                scheduler.WaitForPacket(packet.get(), TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success &&
                    scheduler.WaitForIdle(TimeoutMilliseconds) ==
                        LxrWorkSchedulerResult::Success,
                "combined lazy packet and scheduler drain");
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
            LxrCompletionSnapshot snapshot{};
            success &= Check(
                scheduler.GetCompletionSnapshot(generation, &snapshot) ==
                    LxrWorkSchedulerResult::Success &&
                    snapshot.overallOutstanding == 0 &&
                    snapshot.decrementOutstanding == 0 &&
                    snapshot.lifecycle == LxrCompletionLifecycle::Drained,
                "combined completion ownership drains before reset");
#endif
        }
        success &= Check(
            state.rejectedEarlyPublications.load(std::memory_order_relaxed) == 3 &&
                !state.executedBeforeResume.load(std::memory_order_acquire),
            "all combined publications occur after actual resume");
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        success &= Check(
            state.decrementCallbacks.load(std::memory_order_relaxed) == 3 &&
                state.overallCallbacks.load(std::memory_order_relaxed) == 3,
            "combined decrement and overall transitions occur once per generation");
#endif
        success &= Check(
            scheduler.ResetCompletedCycle() == LxrWorkSchedulerResult::Success &&
                scheduler.Shutdown() == LxrWorkSchedulerResult::Success,
            "combined generation retires and shuts down");
        return success;
    }

    struct RollbackState
    {
        AdapterState adapter;
        LxrWorkScheduler* scheduler;
        LxrWorkPacket* packet;
        ExecutionState* execution;
    };

    void SuspendAndRaceUnconstrained(void* context)
    {
        RollbackState* state = static_cast<RollbackState*>(context);
        state->adapter.suspends.fetch_add(1, std::memory_order_relaxed);
        if (state->scheduler->Submit(state->packet) ==
            LxrWorkSchedulerResult::Success)
        {
            WaitForBlockingStart(state->execution);
        }
    }

    void ResumeRollback(bool finishedGc, void* context)
    {
        RollbackState* state = static_cast<RollbackState*>(context);
        state->adapter.lastFinishedGc.store(finishedGc, std::memory_order_relaxed);
        state->adapter.resumes.fetch_add(1, std::memory_order_relaxed);
    }

    bool ValidateDrainAndRollback()
    {
        LxrWorkScheduler scheduler;
        ExecutionState execution{};
        bool success = Check(
            scheduler.Initialize(2) == LxrWorkSchedulerResult::Success,
            "rollback scheduler initializes");
        std::unique_ptr<LxrWorkPacket> blocking = NewPacket(
            200,
            LxrWorkBucketStage::Unconstrained,
            ExecuteBlocking,
            &execution);
        RollbackState state{{}, &scheduler, blocking.get(), &execution};
        LxrPauseRuntimeAdapter adapter{
            SuspendAndRaceUnconstrained,
            ResumeRollback,
            &state,
            false,
        };
        LxrPauseCoordinator coordinator;
        success &= Check(
            coordinator.Initialize(&scheduler, adapter) ==
                LxrPauseCoordinatorResult::Success &&
                coordinator.PreparePause(LxrPauseKind::Full) ==
                    LxrPauseCoordinatorResult::Success,
            "rollback pause prepares");
        success &= Check(
            coordinator.StartPause(TimeoutMilliseconds) ==
                LxrPauseCoordinatorResult::SchedulerFailure,
            "post-suspension drain race fails start");
        success &= Check(WaitForBlockingStart(&execution), "rollback packet started");
        success &= Check(
            state.adapter.suspends.load(std::memory_order_relaxed) == 1 &&
                state.adapter.resumes.load(std::memory_order_relaxed) == 1,
            "failed start resumes runtime exactly once");
        success &= Check(
            coordinator.GetState() == LxrPauseCoordinatorState::Prepared,
            "failed start restores prepared state");
        ReleaseBlocking(&execution);
        success &= Check(
            scheduler.WaitForPacket(blocking.get(), TimeoutMilliseconds) ==
                LxrWorkSchedulerResult::Success,
            "rollback packet drains");
        scheduler.Shutdown();
        return success;
    }
}

int main()
{
    bool success = ValidateTaxonomyAndSchedules();
    for (size_t index = 0; index < PauseKindCount; index++)
    {
        success &= ValidatePause(static_cast<LxrPauseKind>(index));
    }
    success &= ValidateIllegalTransitions();
    success &= ValidateInterruptionAndResume();
    success &= ValidateDrainAndRollback();
    success &= ValidateDeferredPause();

    if (!success || Failures != 0)
    {
        std::fprintf(stderr, "FAIL: %d/%d checks failed\n", Failures, Checks);
        return 1;
    }

    std::printf("PASS: %d/%d P3.2 pause checks\n", Checks, Checks);
    return 0;
}
