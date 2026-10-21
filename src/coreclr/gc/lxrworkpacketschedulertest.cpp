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
    struct SmokeContext
    {
        std::atomic<uint32_t> executions;
        std::atomic<uint64_t> stageMask;
    };

    void ExecuteSmokePacket(LxrWorkPacket* packet, size_t, void* context)
    {
        SmokeContext* smoke = static_cast<SmokeContext*>(context);
        smoke->executions.fetch_add(1, std::memory_order_relaxed);
        smoke->stageMask.fetch_or(
            UINT64_C(1) << static_cast<uint8_t>(packet->GetStage()),
            std::memory_order_relaxed);
    }
}

extern "C" DLLEXPORT int32_t GC_LxrWorkPacketSchedulerTest_Run()
{
    LxrWorkScheduler scheduler;
    if (scheduler.Initialize(4) != LxrWorkSchedulerResult::Success)
    {
        return 1;
    }

    uintptr_t references[] = {1, 2, 3, 4};
    SmokeContext context{};
    LxrWorkPacket finish(
        1,
        LxrWorkBucketStage::FinishConcurrentWork,
        references,
        ARRAY_SIZE(references),
        ExecuteSmokePacket,
        &context);
    LxrWorkPacket increments(
        2,
        LxrRCProcessIncs,
        references,
        ARRAY_SIZE(references),
        ExecuteSmokePacket,
        &context);
    LxrWorkPacket prepare(
        3,
        LxrWorkBucketStage::Prepare,
        references,
        ARRAY_SIZE(references),
        ExecuteSmokePacket,
        &context);
    LxrWorkPacket closure(
        4,
        LxrWorkBucketStage::Closure,
        references,
        ARRAY_SIZE(references),
        ExecuteSmokePacket,
        &context);
    LxrWorkPacket release(
        5,
        LxrWorkBucketStage::Release,
        references,
        ARRAY_SIZE(references),
        ExecuteSmokePacket,
        &context);
    LxrWorkPacket decrements(
        6,
        LxrRCProcessDecs,
        references,
        ARRAY_SIZE(references),
        ExecuteSmokePacket,
        &context);
    LxrWorkPacket final(
        7,
        LxrWorkBucketStage::Final,
        references,
        ARRAY_SIZE(references),
        ExecuteSmokePacket,
        &context);
    LxrWorkPacket* packets[] = {
        &finish,
        &increments,
        &prepare,
        &closure,
        &release,
        &decrements,
        &final,
    };

    for (LxrWorkPacket* packet : packets)
    {
        if (scheduler.Submit(packet) != LxrWorkSchedulerResult::Success)
        {
            scheduler.Shutdown();
            return 2;
        }
    }
    if (scheduler.BeginStwCycle() != LxrWorkSchedulerResult::Success)
    {
        scheduler.Shutdown();
        return 3;
    }
    if (scheduler.WaitForCycleCompletion(30'000) != LxrWorkSchedulerResult::Success)
    {
        scheduler.Shutdown();
        return 4;
    }

    LxrWorkBucketStage trace[ARRAY_SIZE(packets)];
    size_t traceCount = scheduler.CopyOpenTrace(trace, ARRAY_SIZE(trace));
    if (traceCount != ARRAY_SIZE(packets))
    {
        scheduler.Shutdown();
        return 5;
    }
    for (size_t index = 0; index < traceCount; index++)
    {
        if (trace[index] != packets[index]->GetStage())
        {
            scheduler.Shutdown();
            return 6;
        }
    }

    uint64_t expectedMask = 0;
    for (LxrWorkPacket* packet : packets)
    {
        expectedMask |= UINT64_C(1) << static_cast<uint8_t>(packet->GetStage());
        if (packet->GetState() != LxrWorkPacketState::Completed)
        {
            scheduler.Shutdown();
            return 7;
        }
    }
    LxrWorkSchedulerMetrics metrics = scheduler.GetMetrics();
    int32_t result =
        context.executions.load(std::memory_order_relaxed) == ARRAY_SIZE(packets) &&
        context.stageMask.load(std::memory_order_relaxed) == expectedMask &&
        metrics.submitted == ARRAY_SIZE(packets) &&
        metrics.executed == ARRAY_SIZE(packets) &&
        metrics.ownershipViolations == 0 ?
            0 :
            8;
    scheduler.Shutdown();
    return result;
}
