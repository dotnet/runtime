// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// SafeWrap.cpp
//

//
// This file contains wrapper functions for Win32 API's that take SStrings
// and use CLR-safe holders.
//
// See guidelines in SafeWrap.h for writing these APIs.
//*****************************************************************************

#include "stdafx.h"                     // Precompiled header key.
#include "safewrap.h"
#include "winwrap.h"                    // Header for macros and functions.
#include "utilcode.h"
#include "holder.h"
#include "sstring.h"
#include "ex.h"

DWORD ClrReportEvent(
    LPCWSTR     pEventSource,
    WORD        wType,
    WORD        wCategory,
    DWORD       dwEventID,
    PSID        lpUserSid,
    WORD        wNumStrings,
    LPCWSTR     *lpStrings,
    DWORD       dwDataSize /*=0*/,
    LPVOID      lpRawData /*=NULL*/)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX
    HANDLE h = ::RegisterEventSourceW(
        NULL, // uses local computer
        pEventSource);
    if (h == NULL)
    {
        // Return the error code to the caller so that
        // appropriate asserts/logging can be done
        // incase of errors like event log being full
        return GetLastError();
    }

    // Every event id should have matching description in dlls\shim\eventmsg.mc.  This allows
    // event view to know how to display message.
    _ASSERTE (dwEventID != 0);

    // Attempt to report the event to the event log. Note that if the operation fails
    // (for example because of low memory conditions) it isn't fatal so we can safely ignore
    // the return code from ReportEventW.
    BOOL ret = ::ReportEventW(
        h,                 // event log handle
        wType,
        wCategory,
        dwEventID,
        lpUserSid,
        wNumStrings,
        dwDataSize,
        lpStrings,
        lpRawData);

    DWORD dwRetStatus = GetLastError();

    ::DeregisterEventSource(h);

    return (ret == TRUE)?ERROR_SUCCESS:dwRetStatus;
#else // TARGET_UNIX
    // UNIXTODO: Report the event somewhere?
    return ERROR_SUCCESS;
#endif // TARGET_UNIX
}

// Returns ERROR_SUCCESS if succeessful in reporting to event log, or
// Windows error code to indicate the specific error.
DWORD ClrReportEvent(
    LPCWSTR     pEventSource,
    WORD        wType,
    WORD        wCategory,
    DWORD       dwEventID,
    PSID        lpUserSid,
    LPCWSTR     pMessage)
{
    return ClrReportEvent(pEventSource, wType, wCategory, dwEventID, lpUserSid, 1, &pMessage);
}
