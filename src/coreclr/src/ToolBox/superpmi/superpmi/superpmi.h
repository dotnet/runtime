//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#ifndef _SuperPMI
#define _SuperPMI

#include "errorhandling.h"

enum SPMI_TARGET_ARCHITECTURE
{
    SPMI_TARGET_ARCHITECTURE_X86,
    SPMI_TARGET_ARCHITECTURE_AMD64,
    SPMI_TARGET_ARCHITECTURE_ARM64,
    SPMI_TARGET_ARCHITECTURE_ARM
};

extern SPMI_TARGET_ARCHITECTURE SpmiTargetArchitecture;
extern void SetSuperPmiTargetArchitecture(const char* targetArchitecture);

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
