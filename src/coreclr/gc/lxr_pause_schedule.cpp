// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "lxr_pause_schedule.h"

#include <cstddef>

namespace
{
    constexpr uint16_t StageBit(LxrWorkBucketStage stage)
    {
        return static_cast<uint16_t>(UINT16_C(1) << static_cast<uint8_t>(stage));
    }

    constexpr uint16_t CommonMask =
        StageBit(LxrWorkBucketStage::FinishConcurrentWork) |
        StageBit(LxrWorkBucketStage::Initial) |
        StageBit(LxrWorkBucketStage::Release) |
        StageBit(LxrWorkBucketStage::Final);
    constexpr uint16_t PrepareMask = StageBit(LxrWorkBucketStage::Prepare);
    constexpr uint16_t ClosureMask = StageBit(LxrWorkBucketStage::Closure);
    constexpr uint16_t DecrementsMask = StageBit(LxrWorkBucketStage::STWRCDecsAndSweep);

    constexpr const char* PauseKindNames[] = {
        "Full",
        "RefCount",
        "InitialMark",
        "FinalMark",
    };

    constexpr LxrPauseSchedule PauseSchedules[] = {
        {LxrPauseKind::Full, CommonMask | PrepareMask | ClosureMask | DecrementsMask},
        {LxrPauseKind::RefCount, CommonMask},
        {LxrPauseKind::InitialMark, CommonMask | PrepareMask},
        {LxrPauseKind::FinalMark, CommonMask | PrepareMask | ClosureMask},
    };

    static_assert((sizeof(PauseKindNames) / sizeof(PauseKindNames[0])) ==
        static_cast<size_t>(LxrPauseKind::Count));
    static_assert((sizeof(PauseSchedules) / sizeof(PauseSchedules[0])) ==
        static_cast<size_t>(LxrPauseKind::Count));
}

const char* LxrGetPauseKindName(LxrPauseKind kind)
{
    size_t index = static_cast<size_t>(kind);
    return index < static_cast<size_t>(LxrPauseKind::Count) ?
        PauseKindNames[index] :
        "Invalid";
}

bool LxrTryGetPauseSchedule(LxrPauseKind kind, LxrPauseSchedule* schedule)
{
    size_t index = static_cast<size_t>(kind);
    if (schedule == nullptr || index >= static_cast<size_t>(LxrPauseKind::Count))
    {
        return false;
    }

    *schedule = PauseSchedules[index];
    return true;
}

bool LxrIsPauseStageEnabled(const LxrPauseSchedule& schedule, LxrWorkBucketStage stage)
{
    return LxrIsStwStage(stage) &&
        (schedule.enabledStageMask & StageBit(stage)) != 0;
}
