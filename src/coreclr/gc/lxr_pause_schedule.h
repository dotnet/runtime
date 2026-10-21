// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LXR_PAUSE_SCHEDULE_H
#define LXR_PAUSE_SCHEDULE_H

#include "lxr_work_packet.h"

#include <cstdint>

enum class LxrPauseKind : uint8_t
{
    Full,
    RefCount,
    InitialMark,
    FinalMark,
    Count,
};

static_assert(static_cast<uint8_t>(LxrPauseKind::Full) == 0);
static_assert(static_cast<uint8_t>(LxrPauseKind::RefCount) == 1);
static_assert(static_cast<uint8_t>(LxrPauseKind::InitialMark) == 2);
static_assert(static_cast<uint8_t>(LxrPauseKind::FinalMark) == 3);
static_assert(static_cast<uint8_t>(LxrPauseKind::Count) == 4);

struct LxrPauseSchedule
{
    LxrPauseKind kind;
    uint16_t enabledStageMask;
};

const char* LxrGetPauseKindName(LxrPauseKind kind);
bool LxrTryGetPauseSchedule(LxrPauseKind kind, LxrPauseSchedule* schedule);
bool LxrIsPauseStageEnabled(const LxrPauseSchedule& schedule, LxrWorkBucketStage stage);

#endif // LXR_PAUSE_SCHEDULE_H
