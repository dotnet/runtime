// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// SafeWrap.h
//

//
// This file contains wrapper functions for Win32 API's that take SStrings
// and use CLR-safe holders.
//*****************************************************************************


/*
    Guidelines for SafeWrapper APIs:
Most of these are 'common-sense', plus a few arbitrary decisions thrown in for
consistency's sake.

- THROWING: Throw on oom, but return all other failure codes.
    The rationale here is that SString operations already throw, so to make these APIs
    non-throwing would require an extra EX_TRY/EX_CATCH. Most callees will want to throw
    on OOM anyways. So making these APIs non-throwing would mean an extra try/catch in
    the caller + an extra check at the callee. We can eliminate that overhead and just make
    it throwing.

    Return non-oom failure codes because callees actually freqeuntly expect an API to fail.
    For example, the callee will have special handling for file-not-found.

    For convenience, you could add a no-throwing wrapper version of the API:
        ClrGetEnvironmentVariable   <-- default throws on oom.
        ClrGetEnvironmentVariableNoThrow <-- never throws.

- NAMING: Prefix the name with 'Clr', just like we do for win32 APIs going through hosting.

- DON'T FORGET CONTRACTS: Most of these APIs will likely be Throws/GC_Notrigger.
    Also use PRECONDITIONs + POSTCONDITIONS when possible.

- SIGNATURES: Keep the method signture as close the the original win32 API as possible.
    - Preserve the return type + value. (except allow it to throw on oom). If the return value
        should be a holder, then use that as an out-parameter at the end of the argument list.
        We don't want to return holders because that will cause the dtors to be called.
    - For input strings use 'const SString &' instead of 'LPCWSTR'.
    - Change ('out' string, length) pairs to 'SString &' (this is the convention the rest of the CLR uses for SStrings)
    - Use Holders where appropriate.
    - Preserve other parameters.

- USE SSTRINGS TO AVOID BUFFER OVERRUN ISSUES: Repeated here for emphasis. Use SStrings when
    applicable to make it very easy to verify the code does not have buffer overruns.
    This will also simplify callsites from having to figure out the length of the output string.

- USE YOUR BEST JUDGEMENT: The primary goal of these API wrappers is to embrace 'security-safe' practices.
    Certainly take any additional steps to that goal. For example, it may make sense to eliminate
    corner case inputs for a given API or to break a single confusing API up into several discrete and
    move obvious APIs.

*/
#ifndef _safewrap_h_
#define _safewrap_h_

#include "holder.h"

class SString;
bool ClrGetEnvironmentVariable(LPCSTR szEnvVarName, SString & value);
bool ClrGetEnvironmentVariableNoThrow(LPCSTR szEnvVarName, SString & value);
void ClrGetModuleFileName(HMODULE hModule, SString & value);

void ClrGetCurrentDirectory(SString & value);


/* --------------------------------------------------------------------------- *
 * Simple wrapper around WszFindFirstFile/WszFindNextFile
 * --------------------------------------------------------------------------- */
class ClrDirectoryEnumerator
{
    WIN32_FIND_DATAW    data;
    FindHandleHolder    dirHandle;
    BOOL                fFindNext; // Skip FindNextFile first time around

public:
    ClrDirectoryEnumerator(LPCWSTR pBaseDirectory, LPCWSTR pMask = W("*"));
    bool Next();

    LPCWSTR GetFileName()
    {
        return data.cFileName;
    }

    DWORD GetFileAttributes()
    {
        return data.dwFileAttributes;
    }

    void Close()
    {
        dirHandle.Clear();
    }
};


/* --------------------------------------------------------------------------- *
 * Simple wrapper around RegisterEventSource/ReportEvent/DeregisterEventSource
 * --------------------------------------------------------------------------- */
// Returns ERROR_SUCCESS if succeessful in reporting to event log, or
// Windows error code to indicate the specific error.
DWORD ClrReportEvent(
    LPCWSTR     pEventSource,
    WORD        wType,
    WORD        wCategory,
    DWORD       dwEventID,
    PSID        lpUserSid,
    WORD        wNumStrings,
    LPCWSTR     *lpStrings,
    DWORD       dwDataSize = 0,
    LPVOID      lpRawData = NULL);

DWORD ClrReportEvent(
    LPCWSTR     pEventSource,
    WORD        wType,
    WORD        wCategory,
    DWORD       dwEventID,
    PSID        lpUserSid,
    LPCWSTR     pMessage);

//*****************************************************************************
// This provides a wrapper around GetFileSize() that forces it to fail
// if the file is >4g and pdwHigh is NULL. Other than that, it acts like
// the genuine GetFileSize().
//
//
//*****************************************************************************
DWORD inline SafeGetFileSize(HANDLE hFile, DWORD *pdwHigh)
{
    if (pdwHigh != NULL)
    {
        return ::GetFileSize(hFile, pdwHigh);
    }
    else
    {
        DWORD hi;
        DWORD lo = ::GetFileSize(hFile, &hi);
        if (lo == 0xffffffff && GetLastError() != NO_ERROR)
        {
            return lo;
        }
        // api succeeded. is the file too large?
        if (hi != 0)
        {
            // there isn't really a good error to set here...
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            return 0xffffffff;
        }

        if (lo == 0xffffffff)
        {
            // note that a success return of (hi=0,lo=0xffffffff) will be
            // treated as an error by the caller. Again, that's part of the
            // price of being a slacker and not handling the high dword.
            // We'll set a lasterror for them to pick up.
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        }

        return lo;
    }

}

#endif // _safewrap_h_
