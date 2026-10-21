// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "lxr_work_scheduler.h"

#include <atomic>

#ifndef DLLEXPORT
#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#endif

namespace
{
    struct CompletionSmokeContext
    {
        LxrWorkScheduler* scheduler;
        LxrWorkPacket* sweep;
        std::atomic<uint32_t> order;
        std::atomic<uint32_t> decrementOrder;
        std::atomic<uint32_t> sweepOrder;
        std::atomic<uint32_t> overallOrder;
        std::atomic<uint32_t> releases;
    };

    void ExecuteDecrement(LxrWorkPacket*, size_t, void*)
    {
    }

    void ExecuteSweep(LxrWorkPacket* packet, size_t, void* context)
    {
        CompletionSmokeContext* smoke =
            static_cast<CompletionSmokeContext*>(context);
        LxrBlockReleaseGuard guard;
        if (packet->CreateBlockReleaseGuard(&guard) ==
                LxrCompletionResult::Success &&
            guard.IsValid())
        {
            smoke->sweepOrder.store(
                smoke->order.fetch_add(1, std::memory_order_seq_cst) + 1,
                std::memory_order_seq_cst);
            smoke->releases.fetch_add(1, std::memory_order_seq_cst);
        }
    }

    LxrCompletionCallbackResult OnDecrementsDrained(
        LxrMatureSweepContinuation* continuation,
        void* context)
    {
        CompletionSmokeContext* smoke =
            static_cast<CompletionSmokeContext*>(context);
        smoke->decrementOrder.store(
            smoke->order.fetch_add(1, std::memory_order_seq_cst) + 1,
            std::memory_order_seq_cst);
        return smoke->scheduler->SubmitMatureSweep(
                   continuation,
                   smoke->sweep) == LxrWorkSchedulerResult::Success
            ? LxrCompletionCallbackResult::Success
            : LxrCompletionCallbackResult::Failure;
    }

    LxrCompletionCallbackResult OnOverallDrained(uint64_t, void* context)
    {
        CompletionSmokeContext* smoke =
            static_cast<CompletionSmokeContext*>(context);
        smoke->overallOrder.store(
            smoke->order.fetch_add(1, std::memory_order_seq_cst) + 1,
            std::memory_order_seq_cst);
        return LxrCompletionCallbackResult::Success;
    }
}

extern "C" DLLEXPORT int32_t GC_LxrCompletionCounterTest_Run()
{
    uintptr_t references[] = {1, 2, 3, 4};
    LxrWorkScheduler scheduler;
    CompletionSmokeContext context{};
    context.scheduler = &scheduler;
    LxrWorkPacket sweep(
        2,
        LxrWorkBucketStage::Unconstrained,
        references,
        ARRAY_SIZE(references),
        ExecuteSweep,
        &context);
    context.sweep = &sweep;
    if (scheduler.Initialize(
            2,
            true,
            true,
            OnDecrementsDrained,
            OnOverallDrained,
            &context) != LxrWorkSchedulerResult::Success)
    {
        return 1;
    }

    LxrWorkPacket decrement(
        1,
        LxrWorkBucketStage::Unconstrained,
        references,
        ARRAY_SIZE(references),
        ExecuteDecrement,
        &context);
    if (scheduler.SubmitDeferredConcurrentTracked(
            &decrement,
            LxrCompletionWorkKind::Decrement,
            LxrDeferredWorkPriority::Prioritized) !=
            LxrWorkSchedulerResult::Success ||
        scheduler.BeginStwCycle() != LxrWorkSchedulerResult::Success ||
        scheduler.WaitForCycleCompletion(30'000) !=
            LxrWorkSchedulerResult::Success ||
        scheduler.ResumeConcurrentWork() != LxrWorkSchedulerResult::Success ||
        scheduler.PublishDeferredConcurrentWork(1) !=
            LxrWorkSchedulerResult::Success ||
        scheduler.WaitForPacket(&decrement, 30'000) !=
            LxrWorkSchedulerResult::Success ||
        scheduler.WaitForPacket(&sweep, 30'000) !=
            LxrWorkSchedulerResult::Success ||
        scheduler.WaitForCompletion(1, 30'000) !=
            LxrWorkSchedulerResult::Success ||
        scheduler.WaitForIdle(30'000) != LxrWorkSchedulerResult::Success)
    {
        scheduler.Shutdown();
        return 2;
    }

    LxrCompletionSnapshot snapshot{};
    int32_t result =
        scheduler.GetCompletionSnapshot(1, &snapshot) ==
                LxrWorkSchedulerResult::Success &&
            snapshot.overallOutstanding == 0 &&
            snapshot.decrementOutstanding == 0 &&
            snapshot.decrementZeroTransitions == 1 &&
            snapshot.overallZeroTransitions == 1 &&
            snapshot.ownershipViolations == 0 &&
            context.decrementOrder.load(std::memory_order_seq_cst) == 1 &&
            context.sweepOrder.load(std::memory_order_seq_cst) == 2 &&
            context.overallOrder.load(std::memory_order_seq_cst) == 3 &&
            context.releases.load(std::memory_order_seq_cst) == 1 &&
            scheduler.ResetCompletedCycle() ==
                LxrWorkSchedulerResult::Success
        ? 0
        : 3;
    scheduler.Shutdown();
    return result;
}
