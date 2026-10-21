// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LXR_PAUSE_COORDINATOR_H
#define LXR_PAUSE_COORDINATOR_H

#include "lxr_pause_schedule.h"
#include "lxr_work_scheduler.h"

#ifdef byte
#pragma push_macro("byte")
#define LXR_PAUSE_COORDINATOR_RESTORE_BYTE
#undef byte
#endif
#ifdef __valid
#pragma push_macro("__valid")
#define LXR_PAUSE_COORDINATOR_RESTORE_VALID
#undef __valid
#endif

#include <array>
#include <chrono>
#include <cstdint>

#ifdef LXR_PAUSE_COORDINATOR_RESTORE_BYTE
#pragma pop_macro("byte")
#undef LXR_PAUSE_COORDINATOR_RESTORE_BYTE
#endif
#ifdef LXR_PAUSE_COORDINATOR_RESTORE_VALID
#pragma pop_macro("__valid")
#undef LXR_PAUSE_COORDINATOR_RESTORE_VALID
#endif

enum class LxrPauseCoordinatorResult : uint8_t
{
    Success,
    InvalidArgument,
    NotInitialized,
    AlreadyInitialized,
    InvalidState,
    SchedulerFailure,
    Timeout,
};

enum class LxrPauseCoordinatorState : uint8_t
{
    Uninitialized,
    Idle,
    Prepared,
    Active,
    Faulted,
};

using LxrPauseSuspendCallback = void (*)(void* context);
using LxrPauseResumeCallback = void (*)(bool finishedGc, void* context);

struct LxrPauseRuntimeAdapter
{
    LxrPauseSuspendCallback suspend;
    LxrPauseResumeCallback resume;
    void* context;
    bool finishedGc;
};

struct LxrPauseKindMetrics
{
    uint64_t count;
    uint64_t totalNanoseconds;
    uint64_t maximumNanoseconds;
};

class LxrPauseCoordinator final
{
public:
    LxrPauseCoordinator();
    LxrPauseCoordinator(const LxrPauseCoordinator&) = delete;
    LxrPauseCoordinator& operator=(const LxrPauseCoordinator&) = delete;

    LxrPauseCoordinatorResult Initialize(
        LxrWorkScheduler* scheduler,
        const LxrPauseRuntimeAdapter& adapter);
    LxrPauseCoordinatorResult PreparePause(
        LxrPauseKind kind, bool eagerDecrements = false, bool prepareRelocation = false);
    LxrPauseCoordinatorResult StartPause(uint32_t timeoutMilliseconds);
    LxrPauseCoordinatorResult CompletePause(uint32_t timeoutMilliseconds);
    LxrPauseCoordinatorResult AbortPause();

    LxrPauseCoordinatorState GetState() const;
    LxrPauseKind GetCurrentPauseKind() const;
    LxrPauseKindMetrics GetMetrics(LxrPauseKind kind) const;

private:
    static LxrPauseCoordinatorResult FromSchedulerResult(LxrWorkSchedulerResult result);
    void RollBackInterruption();
    void SetFaulted();

    LxrWorkScheduler* m_scheduler;
    LxrPauseRuntimeAdapter m_adapter;
    LxrPauseCoordinatorState m_state;
    LxrPauseKind m_current_kind;
    std::chrono::steady_clock::time_point m_pause_start;
    std::array<LxrPauseKindMetrics, static_cast<size_t>(LxrPauseKind::Count)> m_metrics;
};

#endif // LXR_PAUSE_COORDINATOR_H
