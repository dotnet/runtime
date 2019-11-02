// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

//-----------------------------------------------------------------------------
// Get the current directory.
// On success, returns true and sets 'Value' to unicode version of cur dir.
// Throws on all failures. This should mainly be oom.
//-----------------------------------------------------------------------------
void ClrGetCurrentDirectory(SString & value)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Get size needed
    DWORD len = WszGetCurrentDirectory(value);


    // An actual API failure in GetCurrentDirectory failure should be very rare, so we'll throw on those.
    if (len == 0)
    {
        ThrowLastError();
    }
}

//-----------------------------------------------------------------------------
// Reads an environment variable into the given SString.
// Returns true on success, false on failure (includes if the var does not exist).
// May throw on oom.
//-----------------------------------------------------------------------------
bool ClrGetEnvironmentVariable(LPCSTR szEnvVarName, SString & value)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;

        PRECONDITION(szEnvVarName != NULL);
    }
    CONTRACTL_END;

    // First read it to get the needed length.
    DWORD lenWithNull = GetEnvironmentVariableA(szEnvVarName, NULL, 0);
    if (lenWithNull == 0)
    {
        return false;
    }

    // Now read it for content.
    char * pCharBuf = value.OpenANSIBuffer(lenWithNull);
    DWORD lenWithoutNull = GetEnvironmentVariableA(szEnvVarName, pCharBuf, lenWithNull);
    value.CloseBuffer(lenWithoutNull);

    if (lenWithoutNull != (lenWithNull - 1))
    {
        // Env var must have changed underneath us.
        return false;
    }
    return true;
}

void ClrGetModuleFileName(HMODULE hModule, SString & value)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    WCHAR * pCharBuf = value.OpenUnicodeBuffer(_MAX_PATH);
    DWORD numChars = GetModuleFileNameW(hModule, pCharBuf, _MAX_PATH);
    value.CloseBuffer(numChars);
}

ClrDirectoryEnumerator::ClrDirectoryEnumerator(LPCWSTR pBaseDirectory, LPCWSTR pMask /*= W("*")*/)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    StackSString strMask(pBaseDirectory);
    SString s(SString::Literal, DIRECTORY_SEPARATOR_STR_W);
    if (!strMask.EndsWith(s))
    {
        strMask.Append(s);
    }
    strMask.Append(pMask);
    dirHandle = WszFindFirstFile(strMask, &data);

    if (dirHandle == INVALID_HANDLE_VALUE)
    {
        DWORD dwLastError = GetLastError();

        // We either ran out of files, or didnt encounter even a single file matching the
        // search mask. If it is neither of these conditions, then convert the error to an exception
        // and raise it.
        if ((dwLastError != ERROR_FILE_NOT_FOUND) && (dwLastError != ERROR_NO_MORE_FILES))
            ThrowLastError();
    }

    fFindNext = FALSE;
}

bool ClrDirectoryEnumerator::Next()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (dirHandle == INVALID_HANDLE_VALUE)
        return FALSE;

    for (;;)
    {
        if (fFindNext)
        {
            if (!WszFindNextFile(dirHandle, &data))
            {
                if (GetLastError() != ERROR_NO_MORE_FILES)
                    ThrowLastError();

                return FALSE;
            }
        }
        else
        {
            fFindNext  = TRUE;
        }

        // Skip junk
        if (wcscmp(data.cFileName, W(".")) != 0 && wcscmp(data.cFileName, W("..")) != 0)
            return TRUE;
    }
}

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

#ifndef FEATURE_PAL
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
#else // FEATURE_PAL
    // UNIXTODO: Report the event somewhere?
    return ERROR_SUCCESS;
#endif // FEATURE_PAL
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

#ifndef FEATURE_PAL
// Read a REG_SZ (null-terminated string) value from the registry.  Throws.
//
// Arguments:
//     hKey - key to registry hive.
//     szValueName - null-terminated string for value name to lookup.
//        If this is empty, this gets the (default) value in the registry hive.
//     value - out parameter to hold registry value string contents.
//
// Returns:
//    value is set on success. Throws on any failure, including if the value doesn't exist
//    or if the value exists but is not a REG_SZ.
//
// Notes:
//    REG_SZ is a single null-terminated string in the registry.
//    This is only support on Windows because the registry is windows specific.
void ClrRegReadString(HKEY hKey, const SString & szValueName, SString & value)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DWORD type;
    DWORD numBytesData;

    // Preemptively clear the string such that it's empty in any failure case.
    value.Clear();

    //
    // Step 1:  First call to find size of buffer and ensure data type is correct
    //
    LONG ret = WszRegQueryValueEx(
      hKey,
      szValueName.GetUnicode(), // NULL or "\0" represents the (default) key.
      NULL, // reserved
      &type, // should be REG_SZ
      NULL, // not requesting data yet
      &numBytesData
    );

    if (ret != ERROR_SUCCESS)
    {
        ThrowWin32(ret);
    }

    if (type != REG_SZ)
    {
        // The registry value is not a string.
        ThrowHR(E_INVALIDARG);
    }

    // REG_SZ includes the null terminator.
    DWORD numCharsIncludingNull = numBytesData / sizeof(WCHAR);

    //
    //  Step 2: Allocate buffer to hold final result
    //
    WCHAR * pData = value.OpenUnicodeBuffer(numCharsIncludingNull);
    DWORD numBytesData2 = numBytesData;


    //
    // Step 3: Requery to get actual contents
    //
    ret = WszRegQueryValueEx(
      hKey,
      szValueName.GetUnicode(),
      NULL, // reserved
      &type, // should still be REG_SZ
      (LPBYTE) pData,
      &numBytesData2
    );

    // This check should only fail if the registry was changed inbetween the first query
    // and the second. In practice, that should never actually happen.
    if ((numBytesData2 != numBytesData) || (type != REG_SZ))
    {
        // On error, leave string empty.
        value.CloseBuffer(0);

        ThrowHR(E_FAIL);
    }

    if (ret != ERROR_SUCCESS)
    {
        // On error, leave string empty.
        value.CloseBuffer(0);
        ThrowWin32(ret);
    }


    //
    // Step 4:  Close the string buffer
    //
    COUNT_T numCharsNoNull = numCharsIncludingNull - 1;
    value.CloseBuffer(numCharsNoNull);
}
#endif // FEATURE_PAL
