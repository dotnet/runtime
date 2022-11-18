// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "staticcontract.h"

#ifndef WRAPPER_NO_CONTRACT
#define WRAPPER_NO_CONTRACT             ANNOTATION_WRAPPER
#endif

#ifndef LIMITED_METHOD_CONTRACT
#define LIMITED_METHOD_CONTRACT                ANNOTATION_FN_LEAF
#endif

//*****************************************************************************
// Enum to track which version of the OS we are running
// Note that Win7 is the minimum supported platform. Any code using
// utilcode (which includes the CLR's execution engine) will fail to start
// on a pre-Win7 platform. This is enforced by InitRunningOnVersionStatus.
//*****************************************************************************
typedef enum {
    RUNNING_ON_STATUS_UNINITED = 0,
    RUNNING_ON_WIN7            = 1,
    RUNNING_ON_WIN8            = 2
} RunningOnStatusEnum;

extern RunningOnStatusEnum gRunningOnStatus;

void InitRunningOnVersionStatus();

//*****************************************************************************
// Returns true if you are running on Windows 8 or newer.
//*****************************************************************************
inline BOOL RunningOnWin8()
{
    WRAPPER_NO_CONTRACT;
#if (!defined(HOST_X86) && !defined(HOST_AMD64))
    return TRUE;
#else
    if (gRunningOnStatus == RUNNING_ON_STATUS_UNINITED)
    {
        InitRunningOnVersionStatus();
    }

    return (gRunningOnStatus >= RUNNING_ON_WIN8) ? TRUE : FALSE;
#endif
}

#ifdef FEATURE_COMINTEROP

inline BOOL WinRTSupported()
{
    return RunningOnWin8();
}

#endif // FEATURE_COMINTEROP

#ifdef HOST_64BIT
inline BOOL RunningInWow64()
{
    return FALSE;
}
#else
BOOL RunningInWow64();
#endif
