// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    eventlog.cpp

Abstract:

    (Rudimentary) implementation of Event Log.
    
    Defaults to BSD syslog. On Mac OS X, uses the superior asl.
    
    Caviats:
      * Neither are real handles that you can lock. If that is necessary, the 
    PAL handle functionality can be used.
      * Neither are kept in a table so that if you ask for the same event source
    twice, you get the same handle.
      * The resource file is not consulted, so we just print out the replacement
    strings. Fortunately, for the CLR, there's the resources just have the single
    replacement string.

Revision History:

    5/21/09 -- initial



--*/

#include "pal/malloc.hpp"
#include "pal/dbgmsg.h"

#ifdef __APPLE__
#define USE_ASL
#endif // __APPLE__

#ifdef USE_ASL
#include <asl.h>
#else // USE_ASL
#include <syslog.h>
#endif // USE_ASL else

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(MISC);

HANDLE
PALAPI
RegisterEventSourceA (
    IN OPTIONAL LPCSTR lpUNCServerName,
    IN     LPCSTR lpSourceName
    )
{
    HANDLE hRet = INVALID_HANDLE_VALUE;

    PERF_ENTRY(RegisterEventSourceA);
    ENTRY("RegisterEventSourceA(lpUNCServerName=%p (%s), lpSourceName=%p (%s))\n",
        lpUNCServerName, lpUNCServerName?lpUNCServerName:"NULL",
        lpSourceName, lpSourceName?lpSourceName:"NULL");
    
    if (NULL != lpUNCServerName)
    {
        SetLastError(ERROR_NOT_SUPPORTED);
        return hRet;
    }
    
    if (NULL == lpSourceName)
    {
        ERROR("lpSourceName has to be a valid parameter\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        return hRet;
    }
    
#ifdef USE_ASL
    // In asl parlance, the EventSource handle is an aslclient; it's not
    // guaranteed to be the same as a different call to this with the same
    // source name.
    aslclient asl = asl_open(lpSourceName, NULL /* facility */, 0 /* opts */);
    hRet = (HANDLE)asl;
#else // USE_ASL
    // In syslog parlance, the EventSource handle is just a string name 
    // representing the source.
    size_t sizeSyslogHandle = strlen(lpSourceName) + 1;
    char *syslogHandle = (char *)PAL_malloc(sizeSyslogHandle);
    if (syslogHandle)
    {
        strcpy_s(syslogHandle, sizeSyslogHandle, lpSourceName);
        hRet = (HANDLE)syslogHandle;
    }
#endif // USE_ASL else

    if (INVALID_HANDLE_VALUE == hRet)
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
    
    LOGEXIT("RegisterEventSourceA returns %p\n", hRet);
    PERF_EXIT(RegisterEventSourceA);
    return hRet;
}

HANDLE
PALAPI
RegisterEventSourceW (
    IN OPTIONAL LPCWSTR lpUNCServerName,
    IN     LPCWSTR lpSourceName
    )
{
    HANDLE hRet = INVALID_HANDLE_VALUE;
    int     size;
    CHAR *inBuff = NULL;
    
    PERF_ENTRY(RegisterEventSourceW);
    ENTRY("RegisterEventSourceW(lpUNCServerName=%p (%S), lpSourceName=%p (%S))\n",
        lpUNCServerName, lpUNCServerName?lpUNCServerName:W16_NULLSTRING,
        lpSourceName, lpSourceName?lpSourceName:W16_NULLSTRING);
    
    if (NULL != lpUNCServerName)
    {
        SetLastError(ERROR_NOT_SUPPORTED);
        return hRet;
    }

    size = WideCharToMultiByte(CP_ACP, 0, lpSourceName, -1, NULL, 0, NULL, NULL);
    
    if (0 == size)
    {
        ERROR("lpSourceName has to be a valid parameter\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        return hRet;
    }
    inBuff = (CHAR *)PAL_malloc(size);
    if (NULL == inBuff)
    {
        ERROR("malloc failed\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return hRet;
    }
    
    if (0 == WideCharToMultiByte(CP_ACP, 0, lpSourceName, -1, inBuff, size, NULL, NULL))
    {
        ASSERT( "WideCharToMultiByte failed!\n" );
        SetLastError(ERROR_INTERNAL_ERROR);
        goto done;
    }
    
    hRet = RegisterEventSourceA(NULL, inBuff);
    
done:
    PAL_free(inBuff);
    
    LOGEXIT("RegisterEventSourceW returns %p\n", hRet);
    PERF_EXIT(RegisterEventSourceW);
    return hRet;
}

BOOL
PALAPI
DeregisterEventSource (
    IN HANDLE hEventLog
    )
{
    BOOL bRet = FALSE;

    PERF_ENTRY(DeregisterEventSource)
    ENTRY("DeregisterEventSource(hEventLog=%p)\n", hEventLog);
    
    if (INVALID_HANDLE_VALUE == hEventLog ||
        NULL == hEventLog)
    {
        SetLastError(ERROR_INVALID_HANDLE);
        goto done;
    }
    
#ifdef USE_ASL
    asl_close((aslclient)hEventLog);
#else // USE_ASL
    PAL_free(hEventLog);
#endif // USE_ASL else

    bRet = TRUE;

done:

    LOGEXIT("DeregisterEventSource returns BOOL %d\n", bRet);
    PERF_EXIT(DeregisterEventSource);
    return bRet;
}

BOOL
PALAPI
ReportEventA (
    IN     HANDLE     hEventLog,
    IN     WORD       wType,
    IN     WORD       wCategory,
    IN     DWORD      dwEventID,
    IN OPTIONAL PSID       lpUserSid,
    IN     WORD       wNumStrings,
    IN     DWORD      dwDataSize,
    IN OPTIONAL LPCSTR *lpStrings,
    IN OPTIONAL LPVOID lpRawData
    )
{
    BOOL bRet = FALSE;

    PERF_ENTRY(ReportEventA);
    ENTRY("ReportEventA(hEventLog=%p, wType=0x%hx, wCategory=0x%hx, dwEventID=0x%x, "
        "lpUserSid=%p, wNumStrings=%hu, dwDataSize=%u, lpStrings=%p, lpRawData=%p)\n",
        hEventLog, wType, wCategory, dwEventID, lpUserSid, wNumStrings, dwDataSize,
        lpStrings, lpRawData);

    if (INVALID_HANDLE_VALUE == hEventLog)
    {
        ERROR("hEventLog has to be a valid parameter\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        return bRet;
    }
    
    if (wNumStrings > 0 && NULL == lpStrings)
    {
        ERROR("lpStrings has to be a valid parameter if wNumStrings is non-zero\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        return bRet;
    }
    
    if (NULL != lpUserSid || 0 != dwDataSize || 1 != wNumStrings)
    {
        SetLastError(ERROR_NOT_SUPPORTED);
        return bRet;
    }
    
#ifdef USE_ASL
    int level;
    switch (wType)
    {
        case EVENTLOG_SUCCESS:
        case EVENTLOG_AUDIT_SUCCESS:
            level = ASL_LEVEL_NOTICE;
            break;
            
        case EVENTLOG_INFORMATION_TYPE:
            level = ASL_LEVEL_INFO;
            break;
        
        case EVENTLOG_ERROR_TYPE:
        case EVENTLOG_AUDIT_FAILURE:
            level = ASL_LEVEL_ERR;
            break;
            
        case EVENTLOG_WARNING_TYPE:
            level = ASL_LEVEL_WARNING;
            break;
            
        default:
            ERROR("Unknown RecordEvent type.\n");
            SetLastError(ERROR_INVALID_PARAMETER);
            return bRet;
    }

    aslmsg msg;
    msg = asl_new(ASL_TYPE_MSG);
    int aslRet;

    if (msg)
    {
        char szNumber[11];
        
        sprintf_s(szNumber, sizeof(szNumber) / sizeof(*szNumber), "%hu", wCategory);
        aslRet = asl_set(msg, "Category", szNumber);
        if (aslRet != 0)
            WARN("Could not set Category %s on aslmsg (%p)", szNumber, msg);
        sprintf_s(szNumber, sizeof(szNumber) / sizeof(*szNumber), "%u", dwEventID);
        aslRet = asl_set(msg, "EventID", szNumber);
        if (aslRet != 0)
            WARN("Could not set EventID %s on aslmsg (%p)", szNumber, msg);

        aslRet = asl_log((aslclient)hEventLog, msg, level, "%s", lpStrings[0]);
        
        asl_free(msg);
    }
    else
    {
        // Yikes, fall back to worse syslog behavior due to low mem or asl issue.
        aslRet = asl_log((aslclient)hEventLog, NULL, level, "[%hx:%x] %s", wCategory, dwEventID, lpStrings[0]);
    }
    
    if (aslRet != 0)
        SetLastError(ERROR_INTERNAL_ERROR);
    else
        bRet = TRUE;
#else // USE_ASL
    int priority;
    switch (wType)
    {
        case EVENTLOG_SUCCESS:
        case EVENTLOG_AUDIT_SUCCESS:
        case EVENTLOG_INFORMATION_TYPE:
            priority = LOG_INFO;
            break;
        
        case EVENTLOG_ERROR_TYPE:
        case EVENTLOG_AUDIT_FAILURE:
            priority = LOG_ERR;
            break;
            
        case EVENTLOG_WARNING_TYPE:
            priority = LOG_WARNING;
            break;
            
        default:
            ERROR("Unknown RecordEvent type.\n");
            SetLastError(ERROR_INVALID_PARAMETER);
            return bRet;
    }

    openlog((char *)hEventLog, LOG_CONS | LOG_PID, LOG_USER);
    
    syslog(priority, "[%hx:%x] %s", wCategory, dwEventID, lpStrings[0]);
    
    closelog();
    
    bRet = TRUE;
#endif // USE_ASL else

    LOGEXIT("ReportEventA returns BOOL %d\n", bRet);
    PERF_EXIT(ReportEventA);
    return bRet;
}

BOOL
PALAPI
ReportEventW (
    IN     HANDLE     hEventLog,
    IN     WORD       wType,
    IN     WORD       wCategory,
    IN     DWORD      dwEventID,
    IN OPTIONAL PSID       lpUserSid,
    IN     WORD       wNumStrings,
    IN     DWORD      dwDataSize,
    IN OPTIONAL LPCWSTR *lpStrings,
    IN OPTIONAL LPVOID lpRawData
    )
{
    BOOL bRet = FALSE;
    LPCSTR *lpMBStrings = NULL;
    
    PERF_ENTRY(ReportEventW);
    ENTRY("ReportEventW(hEventLog=%p, wType=0x%hx, wCategory=0x%hx, dwEventID=0x%x, "
        "lpUserSid=%p, wNumStrings=%hu, dwDataSize=%u, lpStrings=%p, lpRawData=%p)\n",
        hEventLog, wType, wCategory, dwEventID, lpUserSid, wNumStrings, dwDataSize,
        lpStrings, lpRawData);

    if (wNumStrings > 0 && NULL == lpStrings)
    {
        ERROR("lpStrings has to be a valid parameter if wNumStrings is non-zero\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        return bRet;
    }
    
    if (wNumStrings > 0)
    {
        lpMBStrings = (LPCSTR *)PAL_malloc(wNumStrings * sizeof(CHAR *));
        if (!lpMBStrings)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            return bRet;
        }
        
        for (WORD iString = 0; iString < wNumStrings; iString++)
        {
            int size;
            bool fConverted;
            CHAR *sz;
            
            size = WideCharToMultiByte(CP_ACP, 0, lpStrings[iString], -1, NULL, 0, NULL, NULL);
            if (0 == size)
            {
                ERROR("lpStrings[%d] has to be a valid parameter\n", iString);
                SetLastError(ERROR_INVALID_PARAMETER);
                wNumStrings = iString; // so that free only frees earlier converted lpStrings.
                goto done;
            }
            
            sz = (LPSTR)PAL_malloc(size);
            if (!sz)
            {
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                wNumStrings = iString; // so that free only frees earlier converted lpStrings.
                goto done;
            }
            fConverted = (0 != WideCharToMultiByte(CP_ACP, 0, lpStrings[iString], -1, 
                sz, size, NULL, NULL));
            lpMBStrings[iString] = sz; // no const-cast needed.
            if (!fConverted)
            {
                ASSERT("WideCharToMultiByte failed!\n");
                SetLastError(ERROR_INTERNAL_ERROR);
                wNumStrings = iString + 1; // so that free only frees earlier converted lpStrings.
                goto done;
            }
        }
    }
    
    bRet = ReportEventA(hEventLog, wType, wCategory, dwEventID, lpUserSid, wNumStrings,
        dwDataSize, lpMBStrings, lpRawData);

done:
    
    if (wNumStrings > 0)
    {
        for (WORD iString = 0; iString < wNumStrings; iString++)
            PAL_free((PVOID)lpMBStrings[iString]);
        PAL_free(lpMBStrings);
    }

    LOGEXIT("ReportEventW returns BOOL %d\n", bRet);
    PERF_EXIT(ReportEventW);
    return bRet;
}
