// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _SuperPMI
#define _SuperPMI

#include "errorhandling.h"

extern const char* const g_SuperPMIUsageFirstLine;

extern const char* const g_AllFormatStringFixedPrefix;
extern const char* const g_SummaryFormatString;
extern const char* const g_AsmDiffsSummaryFormatString;

enum class SpmiResult
{
    JitFailedToInit = -2,
    GeneralFailure  = -1,
    Success         = 0,
    Error           = 1,
    Diffs           = 2,
    Misses          = 3
};

#endif
