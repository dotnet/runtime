// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "staticcontract.h"

#ifndef WRAPPER_NO_CONTRACT
#define WRAPPER_NO_CONTRACT             ANNOTATION_WRAPPER
#endif

#ifndef LIMITED_METHOD_CONTRACT
#define LIMITED_METHOD_CONTRACT                ANNOTATION_FN_LEAF
#endif

//*****************************************************************************
// Enum to track which version of the OS we are running
// Note that NT5 (Win2k) is the minimum supported platform. Any code using
// utilcode (which includes the CLR's execution engine) will fail to start 
// on a pre-Win2k platform. This is enforced by InitRunningOnVersionStatus.
// 
// Note: The value is used for data mining from links clicked by user in shim dialog - see code:FWLinkTemplateFromTextID
//   Please do not modify existing values, adding new ones is fine.
//*****************************************************************************
typedef enum {
    RUNNING_ON_STATUS_UNINITED = 0, 
    RUNNING_ON_WIN7            = 1, 
    RUNNING_ON_WIN8            = 2
} RunningOnStatusEnum;

extern RunningOnStatusEnum gRunningOnStatus;

void InitRunningOnVersionStatus();

#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORESYSTEM)
typedef enum
{
    WINRT_STATUS_UNINITED = 0,
    WINRT_STATUS_UNSUPPORTED,
    WINRT_STATUS_SUPPORTED
}
WinRTStatusEnum;

extern WinRTStatusEnum      gWinRTStatus;

void InitWinRTStatus();
#endif // FEATURE_COMINTEROP && !FEATURE_CORESYSTEM

//*****************************************************************************
// Returns true if you are running on Windows 8 or newer.
//*****************************************************************************
inline BOOL RunningOnWin8()
{
    WRAPPER_NO_CONTRACT;
#if (!defined(_X86_) && !defined(_AMD64_)) || defined(CROSSGEN_COMPILE)
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

#ifdef FEATURE_CORESYSTEM

inline BOOL WinRTSupported()
{
    return RunningOnWin8();
}
#else
inline BOOL WinRTSupported()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    
#ifdef CROSSGEN_COMPILE
    return TRUE;
#endif

    if (gWinRTStatus == WINRT_STATUS_UNINITED)
    {
        InitWinRTStatus();
    }

    return gWinRTStatus == WINRT_STATUS_SUPPORTED;
}
#endif // FEATURE_CORESYSTEM

#endif // FEATURE_COMINTEROP

#ifdef _WIN64
inline BOOL RunningInWow64()
{
    return FALSE;
}
#else
BOOL RunningInWow64();
#endif
