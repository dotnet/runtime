// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    time.c

Abstract:

    Implementation of time related WIN API functions.



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"

#include <time.h>
#include <sys/time.h>
#include <errno.h>
#include <string.h>
#include <sched.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(MISC);

/*++
Function:
  QueryThreadCycleTime

Puts the execution time (in nanoseconds) for the thread pointed to by ThreadHandle, into the unsigned long
pointed to by CycleTime. ThreadHandle must refer to the current thread. Returns TRUE on success, FALSE on
failure.
--*/

BOOL
PALAPI
QueryThreadCycleTime(
    IN HANDLE ThreadHandle,
    OUT PULONG64 CycleTime
    )
{

    ULONG64 calcTime;
    FILETIME kernelTime, userTime;
    BOOL retval = TRUE;

    if(!GetThreadTimesInternal(ThreadHandle, &kernelTime, &userTime))
    {
        ASSERT("Could not get cycle time for current thread");
        retval = FALSE;
        goto EXIT;
    }

    calcTime = ((ULONG64)kernelTime.dwHighDateTime << 32);
    calcTime += (ULONG64)kernelTime.dwLowDateTime;
    calcTime += ((ULONG64)userTime.dwHighDateTime << 32);
    calcTime += (ULONG64)userTime.dwLowDateTime;
    *CycleTime = calcTime;

EXIT:
    return retval;
}

/*++
Function:
  PAL_nanosleep

Sleeps for the time specified in timeInNs.
Returns 0 on successful completion of the operation.
--*/
PALAPI
INT
PAL_nanosleep(
    IN long timeInNs
    )
{
    struct timespec req;
    struct timespec rem;
    int result;

    req.tv_sec = 0;
    req.tv_nsec = timeInNs;

    do
    {
        // Sleep for the requested time.
        result = nanosleep(&req, &rem);

        // Save the remaining time (used if the loop runs another iteration).
        req = rem;
    }
    while(result == -1 && errno == EINTR);

    return result;
}
