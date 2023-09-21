// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    error.c

Abstract:

    Implementation of Error management functions.

Revision History:



--*/

#include "pal/thread.hpp"
#include "pal/dbgmsg.h"

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(MISC);

/*++
Function:
  GetLastError

GetLastError

The GetLastError function retrieves the calling thread's last-error
code value. The last-error code is maintained on a per-thread
basis. Multiple threads do not overwrite each other's last-error code.

Parameters

This function has no parameters.

Return Values

The return value is the calling thread's last-error code
value. Functions set this value by calling the SetLastError
function. The Return Value section of each reference page notes the
conditions under which the function sets the last-error code.

--*/
DWORD
PALAPI
GetLastError(
         VOID)
{
    return CPalThread::GetLastError();
}



/*++
Function:
  SetLastError

SetLastError

The SetLastError function sets the last-error code for the calling thread.

Parameters

dwErrCode
       [in] Specifies the last-error code for the thread.

Return Values

This function does not return a value.

--*/
VOID
PALAPI
SetLastError(
         IN DWORD dwErrCode)
{
    CPalThread::SetLastError(dwErrCode);
}

/*++
Function:
  PAL_BeginTrackingSystemCallErrors

PAL_BeginTrackingSystemCallErrors

The PAL_BeginTrackingSystemCallErrors function begins tracking system call errors on the calling thread during PAL APIs. The
system call errors may include information about the system call made, relevant arguments, return values, and error codes. A
call to this function should be followed by a call to PAL_EndTrackingSystemCallErrors on the same thread, which returns the set
of system call errors that occurred on the thread in that period. This may not track all system call errors, it may only track
system call errors that directly or indirectly lead to PAL API failures.

Parameters

This function has no parameters.

Return Values

This function does not return a value.

--*/
VOID
PALAPI
PAL_BeginTrackingSystemCallErrors(
         VOID)
{
    CPalThread *thread = GetCurrentPalThread();
    if (thread != nullptr)
    {
        thread->BeginTrackingSystemCallErrors();
    }
}

/*++
Function:
  PAL_EndTrackingSystemCallErrors

PAL_EndTrackingSystemCallErrors

The PAL_EndTrackingSystemCallErrors function retrieves system call errors that occurred on the calling thread since
PAL_BeginTrackingSystemCallErrors was called.

Parameters

This function has no parameters.

Return Values

The return value is the system call errors that occurred on the calling thread since PAL_BeginTrackingSystemCallErrors was
called. Returns NULL if PAL_BeginTrackingSystemCallErrors has not been called, or if no system call errors occurred on the
calling thread since it was last called. If the returned pointer is not NULL, it is only safe to be used by the calling thread
and before the next call to PAL_BeginTrackingSystemCallErrors.

--*/
LPCSTR
PALAPI
PAL_EndTrackingSystemCallErrors(
         bool getSystemCallErrors)
{
    CPalThread *thread = GetCurrentPalThread();
    if (thread != nullptr)
    {
        return thread->EndTrackingSystemCallErrors(getSystemCallErrors);
    }

    return nullptr;
}
