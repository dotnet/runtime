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
    RUNNING_ON_WINNT5          = 1, 
    RUNNING_ON_WINXP           = 2, 
    RUNNING_ON_WIN2003         = 3, // _WIN64 can assume that all OSes that we're running on will be WIN2003+
    RUNNING_ON_VISTA           = 4, 
    RUNNING_ON_WIN7            = 5, 
    RUNNING_ON_WIN8            = 6
} RunningOnStatusEnum;

extern RunningOnStatusEnum gRunningOnStatus;
extern BOOL                gExInfoAvailable;
extern BOOL                gExInfoIsServer;

void InitRunningOnVersionStatus();


//*****************************************************************************
//
// List of currently supported platforms:
//
// Win2000 - not supported
// WinXP   - not supported
// Win2k3  - not supported
// Vista   - desktop, CoreCLR
// Win7    - desktop, CoreCLR
// Win8    - desktop, CoreCLR on CoreSystem, ARM
//
//*****************************************************************************

//*****************************************************************************
// Returns true if you are running on Windows 7 or newer.
//*****************************************************************************
inline BOOL RunningOnWin7()
{
    WRAPPER_NO_CONTRACT;
    return TRUE;
}

//*****************************************************************************
// Returns true if you are running on Windows 8 or newer.
//*****************************************************************************
inline BOOL RunningOnWin8()
{
    WRAPPER_NO_CONTRACT;
#if defined(_ARM_) || defined(CROSSGEN_COMPILE)
    return TRUE;
#else
    if (gRunningOnStatus == RUNNING_ON_STATUS_UNINITED)
    {
        InitRunningOnVersionStatus();
    }

    return (gRunningOnStatus >= RUNNING_ON_WIN8) ? TRUE : FALSE;
#endif
}

//*****************************************************************************
// Returns true if extra information is available
//*****************************************************************************
inline BOOL ExOSInfoAvailable()
{
    WRAPPER_NO_CONTRACT;
    if (gRunningOnStatus == RUNNING_ON_STATUS_UNINITED)
    {
        InitRunningOnVersionStatus();
    }

    return gExInfoAvailable;        
}

//*****************************************************************************
// Returns true if we're running on a server OS. Requires ExOSInfoAvailable()
// to be TRUE
//*****************************************************************************
inline BOOL ExOSInfoRunningOnServer()
{
    WRAPPER_NO_CONTRACT;
    /*
      @TODO: _ASSERTE not available here...
    _ASSERTE(ExOSInfoAvailable() && 
        "You should only call this after making sure ExOSInfoAvailable() returned TRUE");
        */
    if (gRunningOnStatus == RUNNING_ON_STATUS_UNINITED)
    {
        InitRunningOnVersionStatus();
    }

    return gExInfoIsServer;
}

#ifdef FEATURE_COMINTEROP


inline BOOL WinRTSupported()
{
    return RunningOnWin8();
}

#endif // FEATURE_COMINTEROP

#ifdef _WIN64
inline BOOL RunningInWow64()
{
    return FALSE;
}
#else
BOOL RunningInWow64();
#endif
