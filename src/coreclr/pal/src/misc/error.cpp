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

