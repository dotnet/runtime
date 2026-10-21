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
    struct DeferredSmokeContext
    {
        std::atomic<uint32_t> executions;
        std::atomic<uint32_t> nextOrder;
        std::atomic<uint32_t> prioritizedOrder;
        std::atomic<uint32_t> normalOrder;
    };

    void ExecuteDeferredSmokePacket(LxrWorkPacket* packet, size_t, void* context)
    {
        DeferredSmokeContext* smoke = static_cast<DeferredSmokeContext*>(context);
        uint32_t order = smoke->nextOrder.fetch_add(1, std::memory_order_relaxed);
        if (packet->GetId() == 1)
        {
            smoke->prioritizedOrder.store(order, std::memory_order_relaxed);
        }
        else
        {
            smoke->normalOrder.store(order, std::memory_order_relaxed);
        }
        smoke->executions.fetch_add(1, std::memory_order_relaxed);
    }
}

extern "C" DLLEXPORT int32_t GC_LxrDeferredBucketTest_Run()
{
    LxrWorkScheduler scheduler;
    if (scheduler.Initialize(1, true) != LxrWorkSchedulerResult::Success)
    {
        return 1;
    }

    uintptr_t references[] = {1, 2, 3, 4};
    DeferredSmokeContext context{};
    LxrWorkPacket prioritized(
        1,
        LxrWorkBucketStage::Unconstrained,
        references,
        ARRAY_SIZE(references),
        ExecuteDeferredSmokePacket,
        &context);
    LxrWorkPacket normal(
        2,
        LxrWorkBucketStage::Unconstrained,
        references,
        ARRAY_SIZE(references),
        ExecuteDeferredSmokePacket,
        &context);
    if (scheduler.SubmitDeferredConcurrent(
            &normal,
            LxrDeferredWorkPriority::Normal) !=
            LxrWorkSchedulerResult::Success ||
        scheduler.SubmitDeferredConcurrent(
            &prioritized,
            LxrDeferredWorkPriority::Prioritized) !=
            LxrWorkSchedulerResult::Success)
    {
        scheduler.Shutdown();
        return 2;
    }
    if (scheduler.BeginStwCycle() != LxrWorkSchedulerResult::Success ||
        scheduler.WaitForCycleCompletion(30'000) !=
            LxrWorkSchedulerResult::Success)
    {
        scheduler.Shutdown();
        return 3;
    }
    if (context.executions.load(std::memory_order_relaxed) != 0)
    {
        scheduler.Shutdown();
        return 4;
    }
    if (scheduler.ResumeConcurrentWork() != LxrWorkSchedulerResult::Success ||
        scheduler.PublishDeferredConcurrentWork(1) !=
            LxrWorkSchedulerResult::Success)
    {
        scheduler.Shutdown();
        return 5;
    }
    if (scheduler.WaitForPacket(&prioritized, 30'000) !=
            LxrWorkSchedulerResult::Success ||
        scheduler.WaitForPacket(&normal, 30'000) !=
            LxrWorkSchedulerResult::Success ||
        scheduler.WaitForIdle(30'000) != LxrWorkSchedulerResult::Success)
    {
        scheduler.Shutdown();
        return 6;
    }

    LxrWorkSchedulerMetrics metrics = scheduler.GetMetrics();
    LxrDeferredWorkState state = scheduler.GetDeferredWorkState();
    int32_t result =
        context.executions.load(std::memory_order_relaxed) == 2 &&
        context.prioritizedOrder.load(std::memory_order_relaxed) == 0 &&
        context.normalOrder.load(std::memory_order_relaxed) == 1 &&
        metrics.deferredSubmitted == 2 &&
        metrics.deferredPublished == 2 &&
        metrics.deferredExecuted == 2 &&
        metrics.deferredFlips == 1 &&
        metrics.ownershipViolations == 0 &&
        state.publishedGeneration == 1 &&
        state.sealedIndex == LxrNoDeferredQueue ?
            0 :
            7;
    scheduler.Shutdown();
    return result;
}
