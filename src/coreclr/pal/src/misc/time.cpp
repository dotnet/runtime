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
  GetSystemTime

The GetSystemTime function retrieves the current system date and
time. The system time is expressed in Coordinated Universal Time
(UTC).

Parameters

lpSystemTime
       [out] Pointer to a SYSTEMTIME structure to receive the current system date and time.

Return Values

This function does not return a value.

--*/
VOID
PALAPI
GetSystemTime(
          OUT LPSYSTEMTIME lpSystemTime)
{
    time_t tt;
#if HAVE_GMTIME_R
    struct tm ut;
#endif  /* HAVE_GMTIME_R */
    struct tm *utPtr;
    struct timeval timeval;
    int timeofday_retval;

    PERF_ENTRY(GetSystemTime);
    ENTRY("GetSystemTime (lpSystemTime=%p)\n", lpSystemTime);

    tt = time(NULL);

    /* We can't get millisecond resolution from time(), so we get it from
       gettimeofday() */
    timeofday_retval = gettimeofday(&timeval,NULL);

#if HAVE_GMTIME_R
    utPtr = &ut;
    if (gmtime_r(&tt, utPtr) == NULL)
#else   /* HAVE_GMTIME_R */
    if ((utPtr = gmtime(&tt)) == NULL)
#endif  /* HAVE_GMTIME_R */
    {
        ASSERT("gmtime() failed; errno is %d (%s)\n", errno, strerror(errno));
        goto EXIT;
    }

    lpSystemTime->wYear = (WORD)(1900 + utPtr->tm_year);
    lpSystemTime->wMonth = (WORD)(utPtr->tm_mon + 1);
    lpSystemTime->wDayOfWeek = (WORD)utPtr->tm_wday;
    lpSystemTime->wDay = (WORD)utPtr->tm_mday;
    lpSystemTime->wHour = (WORD)utPtr->tm_hour;
    lpSystemTime->wMinute = (WORD)utPtr->tm_min;
    lpSystemTime->wSecond = (WORD)utPtr->tm_sec;

    if(-1 == timeofday_retval)
    {
        ASSERT("gettimeofday() failed; errno is %d (%s)\n",
               errno, strerror(errno));
        lpSystemTime->wMilliseconds = 0;
    }
    else
    {
        int old_seconds;
        int new_seconds;

        lpSystemTime->wMilliseconds = (WORD)(timeval.tv_usec/tccMilliSecondsToMicroSeconds);

        old_seconds = utPtr->tm_sec;
        new_seconds = timeval.tv_sec%60;

        /* just in case we reached the next second in the interval between
           time() and gettimeofday() */
        if( old_seconds!=new_seconds )
        {
            TRACE("crossed seconds boundary; setting milliseconds to 999\n");
            lpSystemTime->wMilliseconds = 999;
        }
    }
EXIT:
    LOGEXIT("GetSystemTime returns void\n");
    PERF_EXIT(GetSystemTime);
}

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
