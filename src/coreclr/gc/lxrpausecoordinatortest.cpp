// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "env/gcenv.ee.h"
#include "lxr_pause_coordinator.h"

#ifdef byte
#pragma push_macro("byte")
#define LXR_PAUSE_COORDINATOR_TEST_RESTORE_BYTE
#undef byte
#endif
#ifdef __valid
#pragma push_macro("__valid")
#define LXR_PAUSE_COORDINATOR_TEST_RESTORE_VALID
#undef __valid
#endif

#include <atomic>
#include <memory>
#include <vector>

#ifdef LXR_PAUSE_COORDINATOR_TEST_RESTORE_BYTE
#pragma pop_macro("byte")
#undef LXR_PAUSE_COORDINATOR_TEST_RESTORE_BYTE
#endif
#ifdef LXR_PAUSE_COORDINATOR_TEST_RESTORE_VALID
#pragma pop_macro("__valid")
#undef LXR_PAUSE_COORDINATOR_TEST_RESTORE_VALID
#endif

#ifndef DLLEXPORT
#ifdef _MSC_VER
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __attribute__((visibility("default")))
#endif
#endif

namespace
{
    void SuspendRuntime(void*)
    {
        GCToEEInterface::SuspendEE(SUSPEND_FOR_GC);
    }

    void ResumeRuntime(bool finishedGc, void*)
    {
        GCToEEInterface::RestartEE(finishedGc);
    }

    void ExecutePacket(LxrWorkPacket*, size_t, void* context)
    {
        static_cast<std::atomic<uint32_t>*>(context)->fetch_add(
            1,
            std::memory_order_relaxed);
    }
}

extern "C" DLLEXPORT int32_t GC_LxrPauseCoordinatorTest_Run()
{
    LxrWorkScheduler scheduler;
    if (scheduler.Initialize(4) != LxrWorkSchedulerResult::Success)
    {
        return 1;
    }

    LxrPauseCoordinator coordinator;
    LxrPauseRuntimeAdapter adapter{
        SuspendRuntime,
        ResumeRuntime,
        nullptr,
        false,
    };
    if (coordinator.Initialize(&scheduler, adapter) !=
        LxrPauseCoordinatorResult::Success)
    {
        scheduler.Shutdown();
        return 2;
    }

    static uintptr_t References[] = {1, 2, 3, 4};
    std::atomic<uint32_t> executions(0);
    uint64_t nextId = 1;
    uint32_t expectedExecutions = 0;
    for (size_t pauseIndex = 0;
         pauseIndex < static_cast<size_t>(LxrPauseKind::Count);
         pauseIndex++)
    {
        LxrPauseKind kind = static_cast<LxrPauseKind>(pauseIndex);
        LxrPauseSchedule schedule{};
        if (!LxrTryGetPauseSchedule(kind, &schedule) ||
            coordinator.PreparePause(kind) != LxrPauseCoordinatorResult::Success)
        {
            scheduler.Shutdown();
            return 3;
        }

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

            packets.push_back(std::make_unique<LxrWorkPacket>(
                nextId++,
                stage,
                References,
                ARRAY_SIZE(References),
                ExecutePacket,
                &executions));
            if (scheduler.Submit(packets.back().get()) !=
                LxrWorkSchedulerResult::Success)
            {
                scheduler.Shutdown();
                return 4;
            }
            expectedExecutions++;
        }

        if (coordinator.StartPause(30'000) != LxrPauseCoordinatorResult::Success)
        {
            scheduler.Shutdown();
            return 5;
        }
        if (coordinator.CompletePause(30'000) !=
            LxrPauseCoordinatorResult::Success)
        {
            if (coordinator.GetState() == LxrPauseCoordinatorState::Active)
            {
                coordinator.AbortPause();
            }
            else
            {
                scheduler.Shutdown();
            }
            return 5;
        }
        if (coordinator.GetMetrics(kind).count != 1)
        {
            scheduler.Shutdown();
            return 6;
        }
    }

    int32_t result =
        executions.load(std::memory_order_relaxed) == expectedExecutions ?
            0 :
            7;
    scheduler.Shutdown();
    return result;
}
