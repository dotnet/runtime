// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    time.c

Abstract:

    Implementation of time related WIN API functions.



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/misc.h"

#include <time.h>
#include <sys/time.h>
#include <errno.h>
#include <string.h>
#include <sched.h>

#if HAVE_MACH_ABSOLUTE_TIME
#include <mach/mach_time.h>
static mach_timebase_info_data_t s_TimebaseInfo;
#endif

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(MISC);

/*++
Function :
TIMEInitialize

Initialize all Time-related stuff related

(no parameters)

Return value :
TRUE  if Time support initialization succeeded
FALSE otherwise
--*/
BOOL TIMEInitialize(void)
{
    BOOL retval = TRUE;

#if HAVE_MACH_ABSOLUTE_TIME
    kern_return_t result = mach_timebase_info(&s_TimebaseInfo);

    if (result != KERN_SUCCESS)
    {
        ASSERT("mach_timebase_info() failed: %s\n", mach_error_string(result));
        retval = FALSE;
    }
#endif

    return retval;
}


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

    lpSystemTime->wYear = 1900 + utPtr->tm_year;
    lpSystemTime->wMonth = utPtr->tm_mon + 1;
    lpSystemTime->wDayOfWeek = utPtr->tm_wday;
    lpSystemTime->wDay = utPtr->tm_mday;
    lpSystemTime->wHour = utPtr->tm_hour;
    lpSystemTime->wMinute = utPtr->tm_min;
    lpSystemTime->wSecond = utPtr->tm_sec;

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
    
        lpSystemTime->wMilliseconds = timeval.tv_usec/tccMillieSecondsToMicroSeconds;
    
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
  GetTickCount

The GetTickCount function retrieves the number of milliseconds that
have elapsed since the system was started. It is limited to the
resolution of the system timer. To obtain the system timer resolution,
use the GetSystemTimeAdjustment function.

Parameters

This function has no parameters. 

Return Values

The return value is the number of milliseconds that have elapsed since
the system was started.

In the PAL implementation the return value is the elapsed time since
the start of the epoch.

--*/
DWORD
PALAPI
GetTickCount(
         VOID)
{
    DWORD retval = 0;
    PERF_ENTRY(GetTickCount);
    ENTRY("GetTickCount ()\n");

    // Get the 64-bit count from GetTickCount64 and truncate the results.
    retval = (DWORD) GetTickCount64();

    LOGEXIT("GetTickCount returns DWORD %u\n", retval);
    PERF_EXIT(GetTickCount);
    return retval;
}

BOOL
PALAPI
QueryPerformanceCounter(
    OUT LARGE_INTEGER *lpPerformanceCount
    )
{
    BOOL retval = TRUE;
    PERF_ENTRY(QueryPerformanceCounter);
    ENTRY("QueryPerformanceCounter()\n");

#if HAVE_MACH_ABSOLUTE_TIME
    lpPerformanceCount->QuadPart = (LONGLONG)mach_absolute_time();
#elif HAVE_CLOCK_MONOTONIC
    struct timespec ts;
    int result = clock_gettime(CLOCK_MONOTONIC, &ts);

    if (result != 0)
    {
        ASSERT("clock_gettime(CLOCK_MONOTONIC) failed: %d\n", result);
        retval = FALSE;     
    }
    else
    {
        lpPerformanceCount->QuadPart =
                ((LONGLONG)(ts.tv_sec) * (LONGLONG)(tccSecondsToNanoSeconds)) + (LONGLONG)(ts.tv_nsec);
    }
#else
    #error "The PAL requires either mach_absolute_time() or clock_gettime(CLOCK_MONOTONIC) to be supported."
#endif

    LOGEXIT("QueryPerformanceCounter\n");
    PERF_EXIT(QueryPerformanceCounter);
    return retval;
}

BOOL
PALAPI
QueryPerformanceFrequency(
    OUT LARGE_INTEGER *lpFrequency
    )
{
    BOOL retval = TRUE;
    PERF_ENTRY(QueryPerformanceFrequency);
    ENTRY("QueryPerformanceFrequency()\n");

#if HAVE_MACH_ABSOLUTE_TIME
    // use denom == 0 to indicate that s_TimebaseInfo is uninitialised.
    if (s_TimebaseInfo.denom == 0)
    {
        ASSERT("s_TimebaseInfo is uninitialized.\n");
        retval = FALSE;
    }
    else
    {
        // (numer / denom) gives you the nanoseconds per tick, so the below code
        // computes the number of ticks per second. We explicitly do the multiplication
        // first in order to help minimize the error that is produced by integer division.

        lpFrequency->QuadPart = ((LONGLONG)(tccSecondsToNanoSeconds) * (LONGLONG)(s_TimebaseInfo.denom)) / (LONGLONG)(s_TimebaseInfo.numer);
    }
#elif HAVE_CLOCK_MONOTONIC
    // clock_gettime() returns a result in terms of nanoseconds rather than a count. This
    // means that we need to either always scale the result by the actual resolution (to
    // get a count) or we need to say the resolution is in terms of nanoseconds. We prefer
    // the latter since it allows the highest throughput and should minimize error propagated
    // to the user.

    lpFrequency->QuadPart = (LONGLONG)(tccSecondsToNanoSeconds);
#else
    #error "The PAL requires either mach_absolute_time() or clock_gettime(CLOCK_MONOTONIC) to be supported."
#endif

    LOGEXIT("QueryPerformanceFrequency\n");
    PERF_EXIT(QueryPerformanceFrequency);
    return retval;
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
  GetTickCount64

Returns a 64-bit tick count with a millisecond resolution. It tries its best
to return monotonically increasing counts and avoid being affected by changes
to the system clock (either due to drift or due to explicit changes to system
time).
--*/
PALAPI
ULONGLONG
GetTickCount64()
{
    LONGLONG retval = 0;

#if HAVE_MACH_ABSOLUTE_TIME
    // use denom == 0 to indicate that s_TimebaseInfo is uninitialised.
    if (s_TimebaseInfo.denom == 0)
    {
        ASSERT("s_TimebaseInfo is uninitialized.\n");
        retval = FALSE;
    }
    else
    {
        retval = ((LONGLONG)mach_absolute_time() * (LONGLONG)(s_TimebaseInfo.numer)) / ((LONGLONG)(tccMillieSecondsToNanoSeconds) * (LONGLONG)(s_TimebaseInfo.denom));
    }
#elif HAVE_CLOCK_MONOTONIC || HAVE_CLOCK_MONOTONIC_COARSE
    struct timespec ts;

#if HAVE_CLOCK_MONOTONIC_COARSE
    // CLOCK_MONOTONIC_COARSE has enough precision for GetTickCount but
    // doesn't have the same overhead as CLOCK_MONOTONIC. This allows
    // overall higher throughput. See dotnet/coreclr#2257 for more details.

    const clockid_t clockType = CLOCK_MONOTONIC_COARSE;
#else
    const clockid_t clockType = CLOCK_MONOTONIC;
#endif

    int result = clock_gettime(clockType, &ts);

    if (result != 0)
    {
#if HAVE_CLOCK_MONOTONIC_COARSE
        ASSERT("clock_gettime(CLOCK_MONOTONIC_COARSE) failed: %d\n", result);
#else
        ASSERT("clock_gettime(CLOCK_MONOTONIC) failed: %d\n", result);
#endif
        retval = FALSE;     
    }
    else
    {
        retval = ((LONGLONG)(ts.tv_sec) * (LONGLONG)(tccSecondsToMillieSeconds)) + ((LONGLONG)(ts.tv_nsec) / (LONGLONG)(tccMillieSecondsToNanoSeconds));
    }
#else
    #error "The PAL requires either mach_absolute_time() or clock_gettime(CLOCK_MONOTONIC) to be supported."
#endif

    return (ULONGLONG)(retval);
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
