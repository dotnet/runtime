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
#if HAVE_MACH_ABSOLUTE_TIME
    kern_return_t machRet;
    if ((machRet = mach_timebase_info(&s_TimebaseInfo)) != KERN_SUCCESS)
    {
        ASSERT("mach_timebase_info() failed: %s\n", mach_error_string(machRet));
        return FALSE;
    }
#endif

    return TRUE;
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
    do
#if HAVE_MACH_ABSOLUTE_TIME
    {
        lpPerformanceCount->QuadPart = (LONGLONG)mach_absolute_time();
    }
#elif HAVE_CLOCK_MONOTONIC
    {
        struct timespec ts;
        if (clock_gettime(CLOCK_MONOTONIC, &ts) != 0)
        {
            ASSERT("clock_gettime(CLOCK_MONOTONIC) failed; errno is %d (%s)\n", errno, strerror(errno));
            retval = FALSE;
            break;
        }
        lpPerformanceCount->QuadPart = 
            (LONGLONG)ts.tv_sec * (LONGLONG)tccSecondsToNanoSeconds + (LONGLONG)ts.tv_nsec;
    }
#elif HAVE_GETHRTIME
    {
        lpPerformanceCount->QuadPart = (LONGLONG)gethrtime();
    }
#elif HAVE_READ_REAL_TIME
    {
        timebasestruct_t tb;
        read_real_time(&tb, TIMEBASE_SZ);
        if (time_base_to_time(&tb, TIMEBASE_SZ) != 0)
        {
            ASSERT("time_base_to_time() failed; errno is %d (%s)\n", errno, strerror(errno));
            retval = FALSE;
            break;
        }
        lpPerformanceCount->QuadPart = 
            (LONGLONG)tb.tb_high * (LONGLONG)tccSecondsToNanoSeconds + (LONGLONG)tb.tb_low;
    }
#else
    {
        struct timeval tv;    
        if (gettimeofday(&tv, NULL) == -1)
        {
            ASSERT("gettimeofday() failed; errno is %d (%s)\n", errno, strerror(errno));
            retval = FALSE;
            break;
        }
        lpPerformanceCount->QuadPart = 
            (LONGLONG)tv.tv_sec * (LONGLONG)tccSecondsToMicroSeconds + (LONGLONG)tv.tv_usec;    
    }
#endif // HAVE_CLOCK_MONOTONIC 
    while (false);

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
        lpFrequency->QuadPart = (LONGLONG)tccSecondsToNanoSeconds * ((LONGLONG)s_TimebaseInfo.denom / (LONGLONG)s_TimebaseInfo.numer);
    }
#elif HAVE_GETHRTIME || HAVE_READ_REAL_TIME || HAVE_CLOCK_MONOTONIC
    lpFrequency->QuadPart = (LONGLONG)tccSecondsToNanoSeconds;
#else
    lpFrequency->QuadPart = (LONGLONG)tccSecondsToMicroSeconds;
#endif // HAVE_MACH_ABSOLUTE_TIME
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
    ULONGLONG retval = 0;

#if HAVE_MACH_ABSOLUTE_TIME
    {
        // use denom == 0 to indicate that s_TimebaseInfo is uninitialised.
        if (s_TimebaseInfo.denom == 0)
        {
            ASSERT("s_TimebaseInfo is uninitialized.\n");
            goto EXIT;
        }
        retval = (mach_absolute_time() * s_TimebaseInfo.numer / s_TimebaseInfo.denom) / tccMillieSecondsToNanoSeconds;
    }
#elif HAVE_CLOCK_MONOTONIC_COARSE || HAVE_CLOCK_MONOTONIC
    {
        clockid_t clockType = 
#if HAVE_CLOCK_MONOTONIC_COARSE
            CLOCK_MONOTONIC_COARSE; // good enough resolution, fastest speed
#else
            CLOCK_MONOTONIC;
#endif
        struct timespec ts;
        if (clock_gettime(clockType, &ts) != 0)
        {
            ASSERT("clock_gettime(CLOCK_MONOTONIC*) failed; errno is %d (%s)\n", errno, strerror(errno));
            goto EXIT;
        }
        retval = (ts.tv_sec * tccSecondsToMillieSeconds)+(ts.tv_nsec / tccMillieSecondsToNanoSeconds);
    }
#elif HAVE_GETHRTIME
    {
        retval = (ULONGLONG)(gethrtime() / tccMillieSecondsToNanoSeconds);
    }
#elif HAVE_READ_REAL_TIME
    {
        timebasestruct_t tb;
        read_real_time(&tb, TIMEBASE_SZ);
        if (time_base_to_time(&tb, TIMEBASE_SZ) != 0)
        {
            ASSERT("time_base_to_time() failed; errno is %d (%s)\n", errno, strerror(errno));
            goto EXIT;
        }
        retval = (tb.tb_high * tccSecondsToMillieSeconds)+(tb.tb_low / tccMillieSecondsToNanoSeconds);
    }
#else
    {
        struct timeval tv;    
        if (gettimeofday(&tv, NULL) == -1)
        {
            ASSERT("gettimeofday() failed; errno is %d (%s)\n", errno, strerror(errno));
            goto EXIT;
        }
        retval = (tv.tv_sec * tccSecondsToMillieSeconds) + (tv.tv_usec / tccMillieSecondsToMicroSeconds);
    }
#endif // HAVE_CLOCK_MONOTONIC 
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
