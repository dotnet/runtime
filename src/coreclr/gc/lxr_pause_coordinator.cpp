// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "lxr_pause_coordinator.h"

#ifdef byte
#pragma push_macro("byte")
#define LXR_PAUSE_COORDINATOR_CPP_RESTORE_BYTE
#undef byte
#endif
#ifdef __valid
#pragma push_macro("__valid")
#define LXR_PAUSE_COORDINATOR_CPP_RESTORE_VALID
#undef __valid
#endif

#include <algorithm>

#ifdef LXR_PAUSE_COORDINATOR_CPP_RESTORE_BYTE
#pragma pop_macro("byte")
#undef LXR_PAUSE_COORDINATOR_CPP_RESTORE_BYTE
#endif
#ifdef LXR_PAUSE_COORDINATOR_CPP_RESTORE_VALID
#pragma pop_macro("__valid")
#undef LXR_PAUSE_COORDINATOR_CPP_RESTORE_VALID
#endif

LxrPauseCoordinator::LxrPauseCoordinator()
    : m_scheduler(nullptr),
      m_adapter{},
      m_state(LxrPauseCoordinatorState::Uninitialized),
      m_current_kind(LxrPauseKind::Count),
      m_pause_start{},
      m_metrics{}
{
}

LxrPauseCoordinatorResult LxrPauseCoordinator::Initialize(
    LxrWorkScheduler* scheduler,
    const LxrPauseRuntimeAdapter& adapter)
{
    if (scheduler == nullptr || adapter.suspend == nullptr || adapter.resume == nullptr)
    {
        return LxrPauseCoordinatorResult::InvalidArgument;
    }
    if (m_state != LxrPauseCoordinatorState::Uninitialized)
    {
        return LxrPauseCoordinatorResult::AlreadyInitialized;
    }

    m_scheduler = scheduler;
    m_adapter = adapter;
    m_state = LxrPauseCoordinatorState::Idle;
    return LxrPauseCoordinatorResult::Success;
}

LxrPauseCoordinatorResult LxrPauseCoordinator::PreparePause(
    LxrPauseKind kind, bool eagerDecrements, bool prepareRelocation)
{
    if (m_state == LxrPauseCoordinatorState::Uninitialized)
    {
        return LxrPauseCoordinatorResult::NotInitialized;
    }
    if (m_state != LxrPauseCoordinatorState::Idle)
    {
        return LxrPauseCoordinatorResult::InvalidState;
    }

    LxrPauseSchedule schedule{};
    if (!LxrTryGetPauseSchedule(kind, &schedule))
    {
        return LxrPauseCoordinatorResult::InvalidArgument;
    }
    if (eagerDecrements)
    {
        schedule.enabledStageMask |=
            UINT16_C(1) << static_cast<uint8_t>(LxrWorkBucketStage::STWRCDecsAndSweep);
    }
    if (prepareRelocation)
    {
        schedule.enabledStageMask |= UINT16_C(1) << static_cast<uint8_t>(LxrWorkBucketStage::Prepare);
    }

    if (m_scheduler->IsCycleCompleted())
    {
        LxrWorkSchedulerResult reset = m_scheduler->ResetCompletedCycle();
        if (reset != LxrWorkSchedulerResult::Success)
        {
            return FromSchedulerResult(reset);
        }
    }

    LxrWorkSchedulerResult configured =
        m_scheduler->ConfigureStwStages(schedule.enabledStageMask);
    if (configured != LxrWorkSchedulerResult::Success)
    {
        return FromSchedulerResult(configured);
    }

    m_current_kind = kind;
    m_state = LxrPauseCoordinatorState::Prepared;
    return LxrPauseCoordinatorResult::Success;
}

LxrPauseCoordinatorResult LxrPauseCoordinator::StartPause(uint32_t timeoutMilliseconds)
{
    if (m_state == LxrPauseCoordinatorState::Uninitialized)
    {
        return LxrPauseCoordinatorResult::NotInitialized;
    }
    if (m_state != LxrPauseCoordinatorState::Prepared)
    {
        return LxrPauseCoordinatorResult::InvalidState;
    }

    LxrWorkSchedulerResult interrupted =
        m_scheduler->InterruptConcurrentWork(timeoutMilliseconds);
    if (interrupted != LxrWorkSchedulerResult::Success)
    {
        RollBackInterruption();
        return FromSchedulerResult(interrupted);
    }

    m_pause_start = std::chrono::steady_clock::now();
    m_adapter.suspend(m_adapter.context);

    LxrWorkSchedulerResult started = m_scheduler->BeginStwCycle();
    if (started != LxrWorkSchedulerResult::Success)
    {
        m_adapter.resume(false, m_adapter.context);
        RollBackInterruption();
        return FromSchedulerResult(started);
    }

    m_state = LxrPauseCoordinatorState::Active;
    return LxrPauseCoordinatorResult::Success;
}

LxrPauseCoordinatorResult LxrPauseCoordinator::CompletePause(uint32_t timeoutMilliseconds)
{
    if (m_state == LxrPauseCoordinatorState::Uninitialized)
    {
        return LxrPauseCoordinatorResult::NotInitialized;
    }
    if (m_state != LxrPauseCoordinatorState::Active)
    {
        return LxrPauseCoordinatorResult::InvalidState;
    }

    LxrWorkSchedulerResult completed =
        m_scheduler->WaitForCycleCompletion(timeoutMilliseconds);
    if (completed != LxrWorkSchedulerResult::Success)
    {
        return FromSchedulerResult(completed);
    }

    bool deferred = m_scheduler->GetDeferredWorkState().enabled;
    if (!deferred)
    {
        LxrWorkSchedulerResult reset = m_scheduler->ResetCompletedCycle();
        if (reset != LxrWorkSchedulerResult::Success)
        {
            m_adapter.resume(false, m_adapter.context);
            SetFaulted();
            return FromSchedulerResult(reset);
        }
    }
    LxrWorkSchedulerResult resumed = m_scheduler->ResumeInterruptedConcurrentWork();
    if (resumed != LxrWorkSchedulerResult::Success)
    {
        m_adapter.resume(false, m_adapter.context);
        SetFaulted();
        return FromSchedulerResult(resumed);
    }

    m_adapter.resume(m_adapter.finishedGc, m_adapter.context);
    uint64_t elapsed = static_cast<uint64_t>(
        std::chrono::duration_cast<std::chrono::nanoseconds>(
            std::chrono::steady_clock::now() - m_pause_start).count());
    LxrPauseKindMetrics& metrics = m_metrics[static_cast<size_t>(m_current_kind)];
    metrics.count++;
    metrics.totalNanoseconds += elapsed;
    metrics.maximumNanoseconds = std::max(metrics.maximumNanoseconds, elapsed);

    if (deferred)
    {
        uint64_t generation = m_scheduler->GetGeneration();
        LxrWorkSchedulerResult recorded = m_scheduler->RecordMutatorResume(generation);
        if (recorded != LxrWorkSchedulerResult::Success)
        {
            SetFaulted();
            return FromSchedulerResult(recorded);
        }
        LxrWorkSchedulerResult published =
            m_scheduler->PublishDeferredConcurrentWork(generation);
        if (published != LxrWorkSchedulerResult::Success)
        {
            SetFaulted();
            return FromSchedulerResult(published);
        }
    }

    m_current_kind = LxrPauseKind::Count;
    m_state = LxrPauseCoordinatorState::Idle;
    return LxrPauseCoordinatorResult::Success;
}

LxrPauseCoordinatorResult LxrPauseCoordinator::AbortPause()
{
    if (m_state == LxrPauseCoordinatorState::Uninitialized)
    {
        return LxrPauseCoordinatorResult::NotInitialized;
    }
    if (m_state != LxrPauseCoordinatorState::Active)
    {
        return LxrPauseCoordinatorResult::InvalidState;
    }

    LxrWorkSchedulerResult shutdown = m_scheduler->Shutdown();
    m_adapter.resume(false, m_adapter.context);
    SetFaulted();
    return FromSchedulerResult(shutdown);
}

LxrPauseCoordinatorState LxrPauseCoordinator::GetState() const
{
    return m_state;
}

LxrPauseKind LxrPauseCoordinator::GetCurrentPauseKind() const
{
    return m_current_kind;
}

LxrPauseKindMetrics LxrPauseCoordinator::GetMetrics(LxrPauseKind kind) const
{
    size_t index = static_cast<size_t>(kind);
    return index < m_metrics.size() ? m_metrics[index] : LxrPauseKindMetrics{};
}

LxrPauseCoordinatorResult LxrPauseCoordinator::FromSchedulerResult(
    LxrWorkSchedulerResult result)
{
    switch (result)
    {
        case LxrWorkSchedulerResult::Success:
            return LxrPauseCoordinatorResult::Success;
        case LxrWorkSchedulerResult::InvalidArgument:
        case LxrWorkSchedulerResult::InvalidStage:
            return LxrPauseCoordinatorResult::InvalidArgument;
        case LxrWorkSchedulerResult::NotInitialized:
            return LxrPauseCoordinatorResult::NotInitialized;
        case LxrWorkSchedulerResult::Timeout:
            return LxrPauseCoordinatorResult::Timeout;
        case LxrWorkSchedulerResult::InvalidState:
        case LxrWorkSchedulerResult::AlreadyInitialized:
        case LxrWorkSchedulerResult::StaleGeneration:
        case LxrWorkSchedulerResult::PacketAlreadyOwned:
        case LxrWorkSchedulerResult::StageDisabled:
        case LxrWorkSchedulerResult::Busy:
        case LxrWorkSchedulerResult::Shutdown:
#ifdef LXR_ENABLE_LAZY_COMPLETION_COUNTER
        case LxrWorkSchedulerResult::CompletionDisabled:
        case LxrWorkSchedulerResult::CompletionOverflow:
        case LxrWorkSchedulerResult::CompletionUnderflow:
        case LxrWorkSchedulerResult::CompletionFailed:
#endif
            return LxrPauseCoordinatorResult::SchedulerFailure;
    }

    return LxrPauseCoordinatorResult::SchedulerFailure;
}

void LxrPauseCoordinator::RollBackInterruption()
{
    if (m_scheduler->CancelConcurrentInterruption() ==
        LxrWorkSchedulerResult::Success)
    {
        m_state = LxrPauseCoordinatorState::Prepared;
        return;
    }

    SetFaulted();
}

void LxrPauseCoordinator::SetFaulted()
{
    m_current_kind = LxrPauseKind::Count;
    m_state = LxrPauseCoordinatorState::Faulted;
}
