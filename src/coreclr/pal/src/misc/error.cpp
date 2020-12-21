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
  SetErrorMode

The SetErrorMode function controls whether the system will handle the
specified types of serious errors, or whether the process will handle
them.

Parameters

uMode
       [in] Specifies the process error mode. This parameter can be one or more of the following values.

        Value                     Action
        0                         Use the system default, which is to display all error dialog boxes.
        SEM_FAILCRITICALERRORS    The system does not display the critical-error-handler message box. Instead,
                                  the system sends the error to the calling process.
        SEM_NOOPENFILEERRORBOX    The system does not display a message box when it fails to find a file. Instead,
                                  the error is returned to the calling process.

Return Values

The return value is the previous state of the error-mode bit flags.

--*/
UINT
PALAPI
SetErrorMode(
         IN UINT uMode)
{
  PERF_ENTRY(SetErrorMode);
  ENTRY("SetErrorMode (uMode=%#x)\n", uMode);

  LOGEXIT("SetErrorMode returns UINT 0\n");
  PERF_EXIT(SetErrorMode);
  return 0;
}


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

