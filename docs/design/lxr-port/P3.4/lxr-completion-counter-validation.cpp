// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/lxr_completion_counter.h"
#include "../../../../src/coreclr/gc/lxr_work_packet.h"
#include "../../../../src/coreclr/gc/lxr_work_scheduler.h"

#include <atomic>
#include <chrono>
#include <cstdint>
#include <cstdio>
#include <thread>
#include <vector>

namespace
{
    constexpr uint32_t TimeoutMilliseconds = 30'000;

    int s_checks;
    int s_passed;

    bool Check(bool condition, const char* name)
    {
        s_checks++;
        if (!condition)
        {
            std::printf("FAIL: %s\n", name);
            return false;
        }
        s_passed++;
        return true;
    }

    struct DirectContext
    {
        LxrLazyCompletionRegistration sweep;
        std::atomic<uint32_t> order{0};
        std::atomic<uint32_t> decrementOrder{0};
        std::atomic<uint32_t> overallOrder{0};
        std::atomic<uint32_t> decrementCallbacks{0};
        std::atomic<uint32_t> overallCallbacks{0};
        bool failDecrement = false;
        bool failOverall = false;
    };

    LxrCompletionCallbackResult DirectDecrementsDrained(
        LxrMatureSweepContinuation* continuation,
        void* context)
    {
        DirectContext* state = static_cast<DirectContext*>(context);
        state->decrementCallbacks.fetch_add(1, std::memory_order_seq_cst);
        state->decrementOrder.store(
            state->order.fetch_add(1, std::memory_order_seq_cst) + 1,
            std::memory_order_seq_cst);
        if (state->failDecrement)
        {
            return LxrCompletionCallbackResult::Failure;
        }
        return continuation->RegisterSweep(&state->sweep) ==
            LxrCompletionResult::Success
            ? LxrCompletionCallbackResult::Success
            : LxrCompletionCallbackResult::Failure;
    }

    LxrCompletionCallbackResult DirectOverallDrained(uint64_t, void* context)
    {
        DirectContext* state = static_cast<DirectContext*>(context);
        state->overallCallbacks.fetch_add(1, std::memory_order_seq_cst);
        state->overallOrder.store(
            state->order.fetch_add(1, std::memory_order_seq_cst) + 1,
            std::memory_order_seq_cst);
        return state->failOverall
            ? LxrCompletionCallbackResult::Failure
            : LxrCompletionCallbackResult::Success;
    }

    bool ValidateDirectCounter()
    {
        DirectContext context;
        LxrLazyCompletionGenerationState state;
        LxrLazyCompletionRegistration seed;
        bool success = Check(
            state.Initialize(
                7,
                DirectDecrementsDrained,
                DirectOverallDrained,
                &context,
                &seed) == LxrCompletionResult::Success,
            "direct generation initializes");
        LxrLazyCompletionRegistration decrement;
        LxrLazyCompletionRegistration overall;
        success &= Check(
            state.RegisterRoot(
                7,
                LxrCompletionWorkKind::Decrement,
                &decrement) == LxrCompletionResult::Success,
            "decrement root registers");
        success &= Check(
            state.RegisterRoot(
                7,
                LxrCompletionWorkKind::Overall,
                &overall) == LxrCompletionResult::Success,
            "overall root registers");
        LxrLazyCompletionRegistration forgedSweep;
        success &= Check(
            state.RegisterRoot(
                7,
                LxrCompletionWorkKind::MatureSweep,
                &forgedSweep) == LxrCompletionResult::InvalidArgument,
            "mature sweep cannot be forged as root work");
        LxrCompletionSnapshot initial = state.GetSnapshot();
        success &= Check(
            initial.overallOutstanding == 3 &&
                initial.decrementOutstanding == 2,
            "initial counter balance");
        LxrBlockReleaseGuard earlyGuard;
        success &= Check(
            decrement.CreateReleaseGuard(&earlyGuard) ==
                LxrCompletionResult::InvalidState &&
                !earlyGuard.IsValid(),
            "release guard denied before decrement zero");
        success &= Check(
            decrement.Complete() == LxrCompletionResult::Success &&
                context.decrementCallbacks.load(std::memory_order_seq_cst) == 0,
            "non-last decrement completes without callback");
        success &= Check(
            overall.Complete() == LxrCompletionResult::Success,
            "overall-only work completes");
        success &= Check(
            state.Seal(7) == LxrCompletionResult::Success &&
                state.MarkPublished(7) == LxrCompletionResult::Success,
            "generation seals and publishes");
        success &= Check(
            seed.Complete() == LxrCompletionResult::Success,
            "seed completes");
        LxrCompletionSnapshot afterDecrements = state.GetSnapshot();
        success &= Check(
            afterDecrements.decrementOutstanding == 0 &&
                afterDecrements.overallOutstanding == 1 &&
                afterDecrements.decrementZeroTransitions == 1 &&
                afterDecrements.overallZeroTransitions == 0,
            "decrement zero schedules overall-counted sweep");
        success &= Check(
            context.decrementOrder.load(std::memory_order_seq_cst) == 1 &&
                context.overallCallbacks.load(std::memory_order_seq_cst) == 0,
            "decrement callback precedes overall callback");
        LxrBlockReleaseGuard guard;
        success &= Check(
            context.sweep.CreateReleaseGuard(&guard) ==
                LxrCompletionResult::Success &&
                guard.IsValid() &&
                guard.GetGeneration() == 7,
            "mature sweep owns release guard");
        success &= Check(
            context.sweep.Complete() == LxrCompletionResult::Success,
            "mature sweep completes");
        LxrCompletionSnapshot drained = state.GetSnapshot();
        success &= Check(
            drained.overallOutstanding == 0 &&
                drained.decrementOutstanding == 0 &&
                drained.decrementZeroTransitions == 1 &&
                drained.overallZeroTransitions == 1 &&
                drained.decrementCallbacks == 1 &&
                drained.overallCallbacks == 1 &&
                drained.lifecycle == LxrCompletionLifecycle::Drained,
            "direct generation drains exactly once");
        success &= Check(
            context.overallOrder.load(std::memory_order_seq_cst) == 2,
            "overall callback follows sweep");
        success &= Check(
            seed.Complete() == LxrCompletionResult::AlreadyCompleted &&
                context.sweep.Complete() ==
                    LxrCompletionResult::AlreadyCompleted,
            "double completion rejected");
        success &= Check(
            state.PrepareForReuse(6) ==
                    LxrCompletionResult::StaleGeneration &&
                state.PrepareForReuse(7) == LxrCompletionResult::Success,
            "state reuse requires exact drained generation");
        success &= Check(
            !guard.IsValid(),
            "old release guard invalid after state reset");

        LxrLazyCompletionRegistration secondSeed;
        success &= Check(
            state.Initialize(
                8,
                DirectDecrementsDrained,
                DirectOverallDrained,
                &context,
                &secondSeed) == LxrCompletionResult::Success,
            "embedded state reinitializes after drain");
        success &= Check(
            state.RegisterRoot(
                7,
                LxrCompletionWorkKind::Decrement,
                &decrement) == LxrCompletionResult::StaleGeneration,
            "stale root registration rejected");
        success &= Check(
            state.Cancel(8) == LxrCompletionResult::Success &&
                secondSeed.Complete() == LxrCompletionResult::Cancelled &&
                state.FinalizeCancellation(8) ==
                    LxrCompletionResult::Success,
            "cancelled generation drains without callbacks");
        return success;
    }

    bool ValidateOverflowAndFailure()
    {
        bool success = true;
        DirectContext overflowContext;
        LxrLazyCompletionGenerationState overflowState;
        LxrLazyCompletionRegistration overflowSeed;
        success &= Check(
            overflowState.Initialize(
                11,
                DirectDecrementsDrained,
                DirectOverallDrained,
                &overflowContext,
                &overflowSeed,
                1) == LxrCompletionResult::Success,
            "bounded counter initializes");
        LxrLazyCompletionRegistration overflow;
        success &= Check(
            overflowState.RegisterRoot(
                11,
                LxrCompletionWorkKind::Decrement,
                &overflow) == LxrCompletionResult::Overflow,
            "counter overflow rejected");
        success &= Check(
            overflowState.Cancel(11) == LxrCompletionResult::Success &&
                overflowSeed.Complete() == LxrCompletionResult::Cancelled &&
                overflowState.GetSnapshot().overallOutstanding == 0,
            "overflow leaves balanced seed");

        DirectContext failureContext;
        failureContext.failDecrement = true;
        LxrLazyCompletionGenerationState failureState;
        LxrLazyCompletionRegistration failureSeed;
        success &= Check(
            failureState.Initialize(
                12,
                DirectDecrementsDrained,
                DirectOverallDrained,
                &failureContext,
                &failureSeed) == LxrCompletionResult::Success &&
                failureState.Seal(12) == LxrCompletionResult::Success &&
                failureState.MarkPublished(12) ==
                    LxrCompletionResult::Success,
            "failure generation publishes");
        success &= Check(
            failureSeed.Complete() == LxrCompletionResult::CallbackFailed,
            "decrement callback failure propagates");
        LxrCompletionSnapshot failed = failureState.GetSnapshot();
        success &= Check(
            failed.lifecycle == LxrCompletionLifecycle::Failed &&
                failed.overallOutstanding == 0 &&
                failed.decrementOutstanding == 0 &&
                failed.decrementCallbacks == 1 &&
                failed.overallCallbacks == 0,
            "failure suppresses successful next decision");
        success &= Check(
            failureState.PrepareForReuse(12) ==
                LxrCompletionResult::InvalidState,
            "failed generation cannot be reused as success");
        return success;
    }

    struct SchedulerContext
    {
        LxrWorkScheduler* scheduler = nullptr;
        LxrWorkPacket* child = nullptr;
        LxrWorkPacket* sweep = nullptr;
        std::atomic<bool> childStarted{false};
        std::atomic<bool> releaseChild{false};
        std::atomic<uint32_t> order{0};
        std::atomic<uint32_t> promotionOrder{0};
        std::atomic<uint32_t> decrementOrder{0};
        std::atomic<uint32_t> sweepOrder{0};
        std::atomic<uint32_t> overallOrder{0};
        std::atomic<uint32_t> releases{0};
        std::atomic<uint32_t> reentrantQueries{0};
        bool scheduleSweep = true;
    };

    void ExecuteChild(LxrWorkPacket*, size_t, void* context)
    {
        SchedulerContext* state = static_cast<SchedulerContext*>(context);
        state->childStarted.store(true, std::memory_order_release);
        while (!state->releaseChild.load(std::memory_order_acquire))
        {
            std::this_thread::yield();
        }
        state->promotionOrder.store(
            state->order.fetch_add(1, std::memory_order_seq_cst) + 1,
            std::memory_order_seq_cst);
    }

    void ExecuteParent(LxrWorkPacket* packet, size_t, void* context)
    {
        SchedulerContext* state = static_cast<SchedulerContext*>(context);
        if (state->scheduler->SubmitTrackedChild(
                packet,
                state->child,
                LxrCompletionWorkKind::Decrement) !=
            LxrWorkSchedulerResult::Success)
        {
            state->releases.store(UINT32_MAX, std::memory_order_release);
        }
    }

    void ExecuteSweep(LxrWorkPacket* packet, size_t, void* context)
    {
        SchedulerContext* state = static_cast<SchedulerContext*>(context);
        LxrBlockReleaseGuard guard;
        if (packet->CreateBlockReleaseGuard(&guard) !=
                LxrCompletionResult::Success ||
            !guard.IsValid())
        {
            state->releases.store(UINT32_MAX, std::memory_order_release);
            return;
        }
        state->sweepOrder.store(
            state->order.fetch_add(1, std::memory_order_seq_cst) + 1,
            std::memory_order_seq_cst);
        state->releases.fetch_add(1, std::memory_order_seq_cst);
    }

    LxrCompletionCallbackResult SchedulerDecrementsDrained(
        LxrMatureSweepContinuation* continuation,
        void* context)
    {
        SchedulerContext* state = static_cast<SchedulerContext*>(context);
        state->decrementOrder.store(
            state->order.fetch_add(1, std::memory_order_seq_cst) + 1,
            std::memory_order_seq_cst);
        LxrCompletionSnapshot snapshot{};
        if (state->scheduler->GetCompletionSnapshot(
                continuation->GetGeneration(),
                &snapshot) == LxrWorkSchedulerResult::Success)
        {
            state->reentrantQueries.fetch_add(1, std::memory_order_seq_cst);
        }
        if (!state->scheduleSweep)
        {
            return LxrCompletionCallbackResult::Success;
        }
        return state->scheduler->SubmitMatureSweep(
                   continuation,
                   state->sweep) == LxrWorkSchedulerResult::Success
            ? LxrCompletionCallbackResult::Success
            : LxrCompletionCallbackResult::Failure;
    }

    LxrCompletionCallbackResult SchedulerOverallDrained(
        uint64_t generation,
        void* context)
    {
        SchedulerContext* state = static_cast<SchedulerContext*>(context);
        LxrCompletionSnapshot snapshot{};
        if (state->scheduler->GetCompletionSnapshot(generation, &snapshot) ==
            LxrWorkSchedulerResult::Success)
        {
            state->reentrantQueries.fetch_add(1, std::memory_order_seq_cst);
        }
        state->overallOrder.store(
            state->order.fetch_add(1, std::memory_order_seq_cst) + 1,
            std::memory_order_seq_cst);
        return LxrCompletionCallbackResult::Success;
    }

    bool WaitUntil(const std::atomic<bool>& value)
    {
        std::chrono::steady_clock::time_point deadline =
            std::chrono::steady_clock::now() +
            std::chrono::milliseconds(TimeoutMilliseconds);
        while (!value.load(std::memory_order_acquire))
        {
            if (std::chrono::steady_clock::now() >= deadline)
            {
                return false;
            }
            std::this_thread::yield();
        }
        return true;
    }

    bool ValidateSchedulerIntegration()
    {
        bool success = true;
        uintptr_t references[] = {1, 2, 3, 4};
        LxrWorkScheduler scheduler;
        SchedulerContext context;
        context.scheduler = &scheduler;
        LxrWorkPacket child(
            2,
            LxrWorkBucketStage::Unconstrained,
            references,
            4,
            ExecuteChild,
            &context);
        LxrWorkPacket sweep(
            3,
            LxrWorkBucketStage::Unconstrained,
            references,
            4,
            ExecuteSweep,
            &context);
        context.child = &child;
        context.sweep = &sweep;
        success &= Check(
            scheduler.Initialize(
                3,
                true,
                true,
                SchedulerDecrementsDrained,
                SchedulerOverallDrained,
                &context) == LxrWorkSchedulerResult::Success,
            "tracked scheduler initializes");
        LxrWorkPacket parent(
            1,
            LxrWorkBucketStage::Unconstrained,
            references,
            4,
            ExecuteParent,
            &context);
        success &= Check(
            scheduler.SubmitDeferredConcurrentTracked(
                &parent,
                LxrCompletionWorkKind::Decrement,
                LxrDeferredWorkPriority::Prioritized) ==
                LxrWorkSchedulerResult::Success,
            "tracked deferred root submits");
        success &= Check(
            scheduler.BeginStwCycle() == LxrWorkSchedulerResult::Success &&
                scheduler.WaitForCycleCompletion(TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.ResumeConcurrentWork() ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.PublishDeferredConcurrentWork(1) ==
                    LxrWorkSchedulerResult::Success,
            "tracked generation publishes after resume");
        success &= Check(WaitUntil(context.childStarted), "child work starts");
        // The parent registers the child inside its own Execute, and its own registration ends after Execute returns. The
        // child can start on another worker before that, so the snapshot is taken only when the parent has completed.
        success &= Check(
            scheduler.WaitForPacket(&parent, TimeoutMilliseconds) == LxrWorkSchedulerResult::Success,
            "parent completes while the child still runs");
        LxrCompletionSnapshot blocked{};
        success &= Check(
            scheduler.GetCompletionSnapshot(1, &blocked) ==
                    LxrWorkSchedulerResult::Success &&
                blocked.decrementOutstanding == 1 &&
                blocked.overallOutstanding == 1 &&
                blocked.decrementZeroTransitions == 0 &&
                context.releases.load(std::memory_order_acquire) == 0,
            "child registration blocks decrement zero and release");
        success &= Check(
            scheduler.ResetCompletedCycle() == LxrWorkSchedulerResult::Busy,
            "cycle reset blocked by lazy work");
        context.releaseChild.store(true, std::memory_order_release);
        success &= Check(
            scheduler.WaitForPacket(&parent, TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.WaitForPacket(&child, TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.WaitForPacket(&sweep, TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.WaitForCompletion(1, TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.WaitForIdle(TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success,
            "child sweep and decision drain");
        LxrCompletionSnapshot drained{};
        success &= Check(
            scheduler.GetCompletionSnapshot(1, &drained) ==
                    LxrWorkSchedulerResult::Success &&
                drained.overallOutstanding == 0 &&
                drained.decrementOutstanding == 0 &&
                drained.decrementZeroTransitions == 1 &&
                drained.overallZeroTransitions == 1 &&
                drained.ownershipViolations == 0,
            "scheduler counter drains exactly once");
        success &= Check(
            context.promotionOrder.load(std::memory_order_seq_cst) == 1 &&
                context.decrementOrder.load(std::memory_order_seq_cst) == 2 &&
                context.sweepOrder.load(std::memory_order_seq_cst) == 3 &&
                context.overallOrder.load(std::memory_order_seq_cst) == 4 &&
                context.releases.load(std::memory_order_seq_cst) == 1,
            "promotion precedes release and decision");
        success &= Check(
            context.reentrantQueries.load(std::memory_order_seq_cst) == 2,
            "zero callbacks execute outside scheduler lock");
        success &= Check(
            scheduler.ResetCompletedCycle() == LxrWorkSchedulerResult::Success,
            "drained generation resets");

        context.scheduleSweep = false;
        success &= Check(
            scheduler.BeginStwCycle() == LxrWorkSchedulerResult::Success &&
                scheduler.WaitForCycleCompletion(TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.ResumeConcurrentWork() ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.PublishDeferredConcurrentWork(2) ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.WaitForCompletion(2, TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success,
            "empty second generation drains");
        success &= Check(
            scheduler.GetCompletionSnapshot(1, &drained) ==
                LxrWorkSchedulerResult::StaleGeneration,
            "reused embedded side rejects stale generation");
        success &= Check(
            scheduler.ResetCompletedCycle() == LxrWorkSchedulerResult::Success,
            "second generation resets");
        scheduler.Shutdown();
        return success;
    }

    bool ValidateDisabledAndShutdown()
    {
        uintptr_t references[] = {9};
        bool success = true;
        LxrWorkScheduler disabled;
        success &= Check(
            disabled.Initialize(1, true) == LxrWorkSchedulerResult::Success,
            "untracked scheduler initializes");
        LxrWorkPacket disabledPacket(
            20,
            LxrWorkBucketStage::Unconstrained,
            references,
            1,
            ExecuteChild,
            nullptr);
        success &= Check(
            disabled.SubmitDeferredConcurrentTracked(
                &disabledPacket,
                LxrCompletionWorkKind::Decrement,
                LxrDeferredWorkPriority::Normal) ==
                LxrWorkSchedulerResult::CompletionDisabled,
            "tracked API fails closed when disabled");
        disabled.Shutdown();

        LxrWorkScheduler shutdown;
        SchedulerContext context;
        context.scheduler = &shutdown;
        context.scheduleSweep = false;
        success &= Check(
            shutdown.Initialize(
                1,
                true,
                true,
                SchedulerDecrementsDrained,
                SchedulerOverallDrained,
                &context) == LxrWorkSchedulerResult::Success,
            "shutdown scheduler initializes");
        LxrWorkPacket queued(
            21,
            LxrWorkBucketStage::Unconstrained,
            references,
            1,
            ExecuteChild,
            &context);
        success &= Check(
            shutdown.SubmitDeferredConcurrentTracked(
                &queued,
                LxrCompletionWorkKind::Decrement,
                LxrDeferredWorkPriority::Normal) ==
                LxrWorkSchedulerResult::Success,
            "producer-side tracked packet queues");
        shutdown.Shutdown();
        success &= Check(
            queued.GetState() == LxrWorkPacketState::CallerOwned &&
                queued.GetGeneration() == 0,
            "shutdown returns tracked packet ownership");
        success &= Check(
            context.decrementOrder.load(std::memory_order_seq_cst) == 0 &&
                context.overallOrder.load(std::memory_order_seq_cst) == 0,
            "shutdown cancellation suppresses zero callbacks");

        LxrWorkScheduler executing;
        SchedulerContext executingContext;
        executingContext.scheduler = &executing;
        executingContext.scheduleSweep = false;
        success &= Check(
            executing.Initialize(
                1,
                true,
                true,
                SchedulerDecrementsDrained,
                SchedulerOverallDrained,
                &executingContext) == LxrWorkSchedulerResult::Success,
            "executing shutdown scheduler initializes");
        LxrWorkPacket executingPacket(
            22,
            LxrWorkBucketStage::Unconstrained,
            references,
            1,
            ExecuteChild,
            &executingContext);
        success &= Check(
            executing.SubmitDeferredConcurrentTracked(
                &executingPacket,
                LxrCompletionWorkKind::Decrement,
                LxrDeferredWorkPriority::Normal) ==
                    LxrWorkSchedulerResult::Success &&
                executing.BeginStwCycle() ==
                    LxrWorkSchedulerResult::Success &&
                executing.WaitForCycleCompletion(TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success &&
                executing.ResumeConcurrentWork() ==
                    LxrWorkSchedulerResult::Success &&
                executing.PublishDeferredConcurrentWork(1) ==
                    LxrWorkSchedulerResult::Success &&
                WaitUntil(executingContext.childStarted),
            "tracked callback executes before shutdown");
        std::atomic<bool> shutdownReturned{false};
        std::thread shutdownThread([&]()
        {
            executing.Shutdown();
            shutdownReturned.store(true, std::memory_order_release);
        });
        std::this_thread::sleep_for(std::chrono::milliseconds(20));
        success &= Check(
            !shutdownReturned.load(std::memory_order_acquire),
            "shutdown waits for executing callback");
        executingContext.releaseChild.store(true, std::memory_order_release);
        shutdownThread.join();
        success &= Check(
            shutdownReturned.load(std::memory_order_acquire) &&
                executingPacket.GetState() == LxrWorkPacketState::Completed,
            "worker joins before embedded state destruction");
        return success;
    }

    bool ValidateTrackedEagerSeam()
    {
        uintptr_t references[] = {5, 6};
        LxrWorkScheduler scheduler;
        SchedulerContext context;
        context.scheduler = &scheduler;
        context.scheduleSweep = false;
        context.releaseChild.store(true, std::memory_order_release);
        bool success = Check(
            scheduler.Initialize(
                1,
                true,
                true,
                SchedulerDecrementsDrained,
                SchedulerOverallDrained,
                &context) == LxrWorkSchedulerResult::Success,
            "eager seam scheduler initializes");
        LxrWorkPacket eager(
            30,
            LxrRCProcessDecs,
            references,
            2,
            ExecuteChild,
            &context);
        success &= Check(
            scheduler.SubmitTracked(
                &eager,
                LxrCompletionWorkKind::Decrement) ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.BeginStwCycle() ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.WaitForCycleCompletion(TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success,
            "tracked eager decrement executes in STW bucket");
        LxrCompletionSnapshot beforePublish{};
        success &= Check(
            scheduler.GetCompletionSnapshot(1, &beforePublish) ==
                    LxrWorkSchedulerResult::Success &&
                beforePublish.decrementOutstanding == 1 &&
                beforePublish.overallOutstanding == 1,
            "eager decrement leaves only generation seed");
        success &= Check(
            scheduler.ResumeConcurrentWork() ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.PublishDeferredConcurrentWork(1) ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.WaitForCompletion(1, TimeoutMilliseconds) ==
                    LxrWorkSchedulerResult::Success &&
                scheduler.ResetCompletedCycle() ==
                    LxrWorkSchedulerResult::Success,
            "empty publication completes eager generation");
        scheduler.Shutdown();
        return success;
    }
}

int main()
{
    bool success = true;
    success &= ValidateDirectCounter();
    success &= ValidateOverflowAndFailure();
    success &= ValidateSchedulerIntegration();
    success &= ValidateDisabledAndShutdown();
    success &= ValidateTrackedEagerSeam();

    if (!success || s_passed != s_checks)
    {
        std::printf(
            "FAIL: %d/%d P3.4 validation checks passed\n",
            s_passed,
            s_checks);
        return 1;
    }

    std::printf(
        "PASS: %d/%d P3.4 validation checks\n",
        s_passed,
        s_checks);
    return 0;
}
