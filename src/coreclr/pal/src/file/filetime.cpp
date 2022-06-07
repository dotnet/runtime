// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    filetime.cpp

Abstract:

    Implementation of the file WIN API related to file time.

Notes:

One very important thing to note is that on BSD systems, the stat structure
stores nanoseconds for the time-related fields. This is implemented by
replacing the time_t fields st_atime, st_mtime, and st_ctime by timespec
structures, instead named st_atimespec, st_mtimespec, and st_ctimespec.

However, if _POSIX_SOURCE is defined, the fields are time_t values and use
their POSIX names. For compatibility purposes, when _POSIX_SOURCE is NOT
defined, the time-related fields are defined in sys/stat.h as:

#ifndef _POSIX_SOURCE
#define st_atime st_atimespec.tv_sec
#define st_mtime st_mtimespec.tv_sec
#define st_ctime st_ctimespec.tv_sec
#endif

Furthermore, if _POSIX_SOURCE is defined, the structure still has
additional fields for nanoseconds, named st_atimensec, st_mtimensec, and
st_ctimensec.

In the PAL, there is a configure check to see if the system supports
nanoseconds for the time-related fields. This source file also sets macros
so that STAT_ATIME_NSEC etc. will always refer to the appropriate field
if it exists, and are defined as 0 otherwise.

--

Also note that there is no analog to "creation time" on Linux systems.
Instead, we use the inode change time, which is set to the current time
whenever mtime changes or when chmod, chown, etc. syscalls modify the
file status; or mtime if older. Ideally we would use birthtime when
available.


--*/

#include "pal/corunix.hpp"
#include "pal/dbgmsg.h"
#include "pal/filetime.h"
#include "pal/thread.hpp"
#include "pal/file.hpp"

#include <sys/types.h>
#include <sys/stat.h>
#include <utime.h>
#include <time.h>

#if HAVE_SYS_TIME_H
#include <sys/time.h>
#endif  // HAVE_SYS_TIME_H

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(FILE);

// In safemath.h, Template SafeInt uses macro _ASSERTE, which need to use variable
// defdbgchan defined by SET_DEFAULT_DEBUG_CHANNEL. Therefore, the include statement
// should be placed after the SET_DEFAULT_DEBUG_CHANNEL(FILE)
#include <safemath.h>

/* Magic number explanation:

   To 1970:
   Both epochs are Gregorian. 1970 - 1601 = 369. Assuming a leap
   year every four years, 369 / 4 = 92. However, 1700, 1800, and 1900
   were NOT leap years, so 89 leap years, 280 non-leap years.
   89 * 366 + 280 * 365 = 134774 days between epochs. Of course
   60 * 60 * 24 = 86400 seconds per day, so 134774 * 86400 =
   11644473600 = SECS_BETWEEN_1601_AND_1970_EPOCHS.

   To 2001:
   Again, both epochs are Gregorian. 2001 - 1601 = 400. Assuming a leap
   year every four years, 400 / 4 = 100. However, 1700, 1800, and 1900
   were NOT leap years (2000 was because it was divisible by 400), so
   97 leap years, 303 non-leap years.
   97 * 366 + 303 * 365 = 146097 days between epochs. 146097 * 86400 =
   12622780800 = SECS_BETWEEN_1601_AND_2001_EPOCHS.

   This result is also confirmed in the MSDN documentation on how
   to convert a time_t value to a win32 FILETIME.
*/
static const __int64 SECS_BETWEEN_1601_AND_1970_EPOCHS = 11644473600LL;
static const __int64 SECS_TO_100NS = 10000000; /* 10^7 */

#ifdef __APPLE__
static const __int64 SECS_BETWEEN_1601_AND_2001_EPOCHS = 12622780800LL;
#endif // __APPLE__

/*++
Function:
  CompareFileTime

See MSDN doc.
--*/
LONG
PALAPI
CompareFileTime(
        IN CONST FILETIME *lpFileTime1,
        IN CONST FILETIME *lpFileTime2)
{
    __int64 First;
    __int64 Second;

    long Ret;

    PERF_ENTRY(CompareFileTime);
    ENTRY("CompareFileTime(lpFileTime1=%p lpFileTime2=%p)\n",
          lpFileTime1, lpFileTime2);

    First = ((__int64)lpFileTime1->dwHighDateTime << 32) +
        lpFileTime1->dwLowDateTime;
    Second = ((__int64)lpFileTime2->dwHighDateTime << 32) +
        lpFileTime2->dwLowDateTime;

    if ( First < Second )
    {
        Ret = -1;
    }
    else if ( First > Second )
    {
        Ret = 1;
    }
    else
    {
        Ret = 0;
    }

    LOGEXIT("CompareFileTime returns LONG %ld\n", Ret);
    PERF_EXIT(CompareFileTime);
    return Ret;
}


/*++
Function:
  GetSystemTimeAsFileTime

See MSDN doc.
--*/
VOID
PALAPI
GetSystemTimeAsFileTime(
            OUT LPFILETIME lpSystemTimeAsFileTime)
{
    PERF_ENTRY(GetSystemTimeAsFileTime);
    ENTRY("GetSystemTimeAsFileTime(lpSystemTimeAsFileTime=%p)\n",
          lpSystemTimeAsFileTime);

#if HAVE_WORKING_CLOCK_GETTIME
    struct timespec Time;
    if (clock_gettime(CLOCK_REALTIME, &Time) == 0)
    {
        *lpSystemTimeAsFileTime = FILEUnixTimeToFileTime( Time.tv_sec, Time.tv_nsec );
    }
#else
    struct timeval Time;
    if (gettimeofday(&Time, NULL) == 0)
    {
        /* use (tv_usec * 1000) because 2nd arg is in nanoseconds */
        *lpSystemTimeAsFileTime = FILEUnixTimeToFileTime( Time.tv_sec, Time.tv_usec * 1000);
    }
#endif
    else
    {
        /* no way to indicate failure, so set time to zero */
        ASSERT("clock_gettime or gettimeofday failed");
        *lpSystemTimeAsFileTime = FILEUnixTimeToFileTime( 0, 0 );
    }

    LOGEXIT("GetSystemTimeAsFileTime returns.\n");
    PERF_EXIT(GetSystemTimeAsFileTime);
}


#ifdef __APPLE__
/*++
Function:
  FILECFAbsoluteTimeToFileTime

Convert a CFAbsoluteTime value to a win32 FILETIME structure, as described
in MSDN documentation. CFAbsoluteTime is the number of seconds elapsed since
00:00 01 January 2001 UTC (Mac OS X epoch), while FILETIME represents a
64-bit number of 100-nanosecond intervals that have passed since 00:00
01 January 1601 UTC (win32 epoch).
--*/
FILETIME FILECFAbsoluteTimeToFileTime( CFAbsoluteTime sec )
{
    __int64 Result;
    FILETIME Ret;

    Result = ((__int64)sec + SECS_BETWEEN_1601_AND_2001_EPOCHS) * SECS_TO_100NS;

    Ret.dwLowDateTime = (DWORD)Result;
    Ret.dwHighDateTime = (DWORD)(Result >> 32);

    TRACE("CFAbsoluteTime = [%9f] converts to Win32 FILETIME = [%#x:%#x]\n",
          sec, Ret.dwHighDateTime, Ret.dwLowDateTime);

    return Ret;
}
#endif // __APPLE__


/*++
Function:
  FILEUnixTimeToFileTime

Convert a time_t value to a win32 FILETIME structure, as described in
MSDN documentation. time_t is the number of seconds elapsed since
00:00 01 January 1970 UTC (Unix epoch), while FILETIME represents a
64-bit number of 100-nanosecond intervals that have passed since 00:00
01 January 1601 UTC (win32 epoch).
--*/
FILETIME FILEUnixTimeToFileTime( time_t sec, long nsec )
{
    __int64 Result;
    FILETIME Ret;

    Result = ((__int64)sec + SECS_BETWEEN_1601_AND_1970_EPOCHS) * SECS_TO_100NS +
        (nsec / 100);

    Ret.dwLowDateTime = (DWORD)Result;
    Ret.dwHighDateTime = (DWORD)(Result >> 32);

    TRACE("Unix time = [%ld.%09ld] converts to Win32 FILETIME = [%#x:%#x]\n",
          sec, nsec, Ret.dwHighDateTime, Ret.dwLowDateTime);

    return Ret;
}


/**
Function

    FileTimeToSystemTime()

    Helper function for FileTimeToDosTime.
    Converts the necessary file time attributes to system time, for
    easier manipulation in FileTimeToDosTime.

--*/
BOOL PALAPI FileTimeToSystemTime( CONST FILETIME * lpFileTime,
                                  LPSYSTEMTIME lpSystemTime )
{
    UINT64 FileTime = 0;
    time_t UnixFileTime = 0;
    struct tm * UnixSystemTime = 0;

    /* Combine the file time. */
    FileTime = lpFileTime->dwHighDateTime;
    FileTime <<= 32;
    FileTime |= (UINT)lpFileTime->dwLowDateTime;
    bool isSafe = ClrSafeInt<UINT64>::subtraction(
            FileTime,
            SECS_BETWEEN_1601_AND_1970_EPOCHS * SECS_TO_100NS,
            FileTime);

    if (isSafe == true)
    {
#if HAVE_GMTIME_R
        struct tm timeBuf;
#endif  /* HAVE_GMTIME_R */
        /* Convert file time to unix time. */
        if (((INT64)FileTime) < 0)
        {
            UnixFileTime =  -1 - ( ( -FileTime - 1 ) / 10000000 );
        }
        else
        {
            UnixFileTime = FileTime / 10000000;
        }

        /* Convert unix file time to Unix System time. */
#if HAVE_GMTIME_R
        UnixSystemTime = gmtime_r( &UnixFileTime, &timeBuf );
#else   /* HAVE_GMTIME_R */
        UnixSystemTime = gmtime( &UnixFileTime );
#endif  /* HAVE_GMTIME_R */

        /* Convert unix system time to Windows system time. */
        lpSystemTime->wDay      = (WORD)UnixSystemTime->tm_mday;

        /* Unix time counts January as a 0, under Windows it is 1*/
        lpSystemTime->wMonth    = (WORD)UnixSystemTime->tm_mon + 1;
        /* Unix time returns the year - 1900, Windows returns the current year*/
        lpSystemTime->wYear     = (WORD)UnixSystemTime->tm_year + 1900;

        lpSystemTime->wSecond   = (WORD)UnixSystemTime->tm_sec;
        lpSystemTime->wMinute   = (WORD)UnixSystemTime->tm_min;
        lpSystemTime->wHour     = (WORD)UnixSystemTime->tm_hour;
        return TRUE;
    }
    else
    {
        ERROR( "The file time is to large.\n" );
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
    }
}

