// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

Also note that there is no analog to "creation time" on Unix systems.
Instead, we use the inode change time, which is set to the current time
whenever mtime changes or when chmod, chown, etc. syscalls modify the
file status.



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
  SetFileTime

Notes: This function will drop one digit (radix 10) of precision from
the supplied times, since Unix can set to the microsecond (at most, i.e.
if the futimes() function is available).

As noted in the file header, there is no analog to "creation time" on Unix
systems, so the lpCreationTime argument to this function will always be
ignored, and the inode change time will be set to the current time.
--*/
BOOL
PALAPI
SetFileTime(
        IN HANDLE hFile,
        IN CONST FILETIME *lpCreationTime,
        IN CONST FILETIME *lpLastAccessTime,
        IN CONST FILETIME *lpLastWriteTime)
{
    CPalThread *pThread;
    PAL_ERROR palError = NO_ERROR;
    const UINT64 MAX_FILETIMEVALUE = 0x8000000000000000LL;

    PERF_ENTRY(SetFileTime);
    ENTRY("SetFileTime(hFile=%p, lpCreationTime=%p, lpLastAccessTime=%p, "
          "lpLastWriteTime=%p)\n", hFile, lpCreationTime, lpLastAccessTime, 
          lpLastWriteTime);

    pThread = InternalGetCurrentThread();

    /* validate filetime values */
    if ( (lpCreationTime && (((UINT64)lpCreationTime->dwHighDateTime   << 32) + 
          lpCreationTime->dwLowDateTime   >= MAX_FILETIMEVALUE)) ||        
         (lpLastAccessTime && (((UINT64)lpLastAccessTime->dwHighDateTime << 32) + 
          lpLastAccessTime->dwLowDateTime >= MAX_FILETIMEVALUE)) ||
         (lpLastWriteTime && (((UINT64)lpLastWriteTime->dwHighDateTime  << 32) + 
          lpLastWriteTime->dwLowDateTime  >= MAX_FILETIMEVALUE)))
    {
        pThread->SetLastError(ERROR_INVALID_HANDLE);
        return FALSE;
    }

    palError = InternalSetFileTime(
        pThread,
        hFile,
        lpCreationTime,
        lpLastAccessTime,
        lpLastWriteTime
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("SetFileTime returns BOOL %s\n", NO_ERROR == palError ? "TRUE":"FALSE");
    PERF_EXIT(SetFileTime);
    return NO_ERROR == palError;
}

PAL_ERROR
CorUnix::InternalSetFileTime(
        CPalThread *pThread,
        IN HANDLE hFile,
        IN CONST FILETIME *lpCreationTime,
        IN CONST FILETIME *lpLastAccessTime,
        IN CONST FILETIME *lpLastWriteTime)
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pFileObject = NULL;
    CFileProcessLocalData *pLocalData = NULL;
    IDataLock *pLocalDataLock = NULL;
    struct timeval Times[2];
    int fd;
    long nsec;
    struct stat stat_buf;

    if (INVALID_HANDLE_VALUE == hFile)
    {
        ERROR( "Invalid file handle\n" );
        palError = ERROR_INVALID_HANDLE;
        goto InternalSetFileTimeExit;
    }

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pThread,
        hFile,
        &aotFile,
        GENERIC_READ,
        &pFileObject
        );

    if (NO_ERROR != palError)
    {
        goto InternalSetFileTimeExit;
    }

    palError = pFileObject->GetProcessLocalData(
        pThread,
        ReadLock, 
        &pLocalDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto InternalSetFileTimeExit;
    }
    
    if (lpCreationTime)
    {
        palError = ERROR_NOT_SUPPORTED;
        goto InternalSetFileTimeExit;
    }

    if( !lpLastAccessTime && !lpLastWriteTime )
    { 
	// if both pointers are NULL, the function simply returns.
        goto InternalSetFileTimeExit;
    }
    else if( !lpLastAccessTime || !lpLastWriteTime )
    {
	// if either pointer is NULL, fstat will need to be called.
	fd = pLocalData->unix_fd;
	if ( fd == -1 )
        {
          TRACE("pLocalData = [%p], fd = %d\n", pLocalData, fd);
          palError = ERROR_INVALID_HANDLE;
          goto InternalSetFileTimeExit;
        } 

	if ( fstat(fd, &stat_buf) != 0 )
    	{
          TRACE("fstat failed on file descriptor %d\n", fd);
          palError = FILEGetLastErrorFromErrno();
          goto InternalSetFileTimeExit;
        }
    }

    if (lpLastAccessTime)
    {
        Times[0].tv_sec = FILEFileTimeToUnixTime( *lpLastAccessTime, &nsec );
        Times[0].tv_usec = nsec / 1000; /* convert to microseconds */
    }
    else
    {
	    Times[0].tv_sec = stat_buf.st_atime;
	    Times[0].tv_usec = ST_ATIME_NSEC(&stat_buf) / 1000;
    }

    if (lpLastWriteTime)
    {
        Times[1].tv_sec = FILEFileTimeToUnixTime( *lpLastWriteTime, &nsec );
        Times[1].tv_usec = nsec / 1000; /* convert to microseconds */
    }
    else
    {
        Times[1].tv_sec = stat_buf.st_mtime;
        Times[1].tv_usec = ST_MTIME_NSEC(&stat_buf) / 1000;
    }

    TRACE("Setting atime = [%ld.%ld], mtime = [%ld.%ld]\n",
          Times[0].tv_sec, Times[0].tv_usec,
          Times[1].tv_sec, Times[1].tv_usec);

#if HAVE_FUTIMES
    if ( futimes(pLocalData->unix_fd, Times) != 0 )
#elif HAVE_UTIMES
    if ( utimes(pLocalData->unix_filename, Times) != 0 )
#else
  #error Operating system not supported
#endif
    {
        palError = FILEGetLastErrorFromErrno();
    }

InternalSetFileTimeExit:
    if (NULL != pLocalDataLock)
    {
        pLocalDataLock->ReleaseLock(pThread, FALSE);
    }

    if (NULL != pFileObject)
    {
        pFileObject->ReleaseReference(pThread);
    }

    return palError;
}


/*++
Function:
  GetFileTime

Notes: As noted at the top of this file, there is no analog to "creation
time" on Unix systems, so the inode change time is used instead. Also, Win32
LastAccessTime is updated after a write operation, but it is not on Unix.
To be consistent with Win32, this function returns the greater of mtime and
atime for LastAccessTime.
--*/
BOOL
PALAPI
GetFileTime(
        IN HANDLE hFile,
        OUT LPFILETIME lpCreationTime,
        OUT LPFILETIME lpLastAccessTime,
        OUT LPFILETIME lpLastWriteTime)
{
    CPalThread *pThread;
    PAL_ERROR palError = NO_ERROR;

    PERF_ENTRY(GetFileTime);
    ENTRY("GetFileTime(hFile=%p, lpCreationTime=%p, lpLastAccessTime=%p, "
          "lpLastWriteTime=%p)\n",
          hFile, lpCreationTime, lpLastAccessTime, lpLastWriteTime);

    pThread = InternalGetCurrentThread();

    palError = InternalGetFileTime(
        pThread,
        hFile,
        lpCreationTime,
        lpLastAccessTime,
        lpLastWriteTime
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("GetFileTime returns BOOL %s\n", NO_ERROR == palError ? "TRUE":"FALSE");
    PERF_EXIT(GetFileTime);
    return NO_ERROR == palError;
}

PAL_ERROR
CorUnix::InternalGetFileTime(
        CPalThread *pThread,
        IN HANDLE hFile,
        OUT LPFILETIME lpCreationTime,
        OUT LPFILETIME lpLastAccessTime,
        OUT LPFILETIME lpLastWriteTime)
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pFileObject = NULL;
    CFileProcessLocalData *pLocalData = NULL;
    IDataLock *pLocalDataLock = NULL;
    int   Fd = -1;

    struct stat StatData;

    if (INVALID_HANDLE_VALUE == hFile)
    {
        ERROR( "Invalid file handle\n" );
        palError = ERROR_INVALID_HANDLE;
        goto InternalGetFileTimeExit;
    }

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pThread,
        hFile,
        &aotFile,
        GENERIC_READ,
        &pFileObject
        );

    if (NO_ERROR != palError)
    {
        goto InternalGetFileTimeExit;
    }

    palError = pFileObject->GetProcessLocalData(
        pThread,
        ReadLock, 
        &pLocalDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto InternalGetFileTimeExit;
    }

    Fd = pLocalData->unix_fd;

    if ( Fd == -1 )
    {
        TRACE("pLocalData = [%p], Fd = %d\n", pLocalData, Fd);
        palError = ERROR_INVALID_HANDLE;
        goto InternalGetFileTimeExit;
    }

    if ( fstat(Fd, &StatData) != 0 )
    {
        TRACE("fstat failed on file descriptor %d\n", Fd);
        palError = FILEGetLastErrorFromErrno();
        goto InternalGetFileTimeExit;
    }

    if ( lpCreationTime )
    {
        *lpCreationTime = FILEUnixTimeToFileTime(StatData.st_ctime,
                                                 ST_CTIME_NSEC(&StatData));
    }
    if ( lpLastWriteTime )
    {
        *lpLastWriteTime = FILEUnixTimeToFileTime(StatData.st_mtime,
                                                  ST_MTIME_NSEC(&StatData));
    }
    if ( lpLastAccessTime )
    {
        *lpLastAccessTime = FILEUnixTimeToFileTime(StatData.st_atime,
                                                   ST_ATIME_NSEC(&StatData));
        /* if Unix mtime is greater than atime, return mtime as the last
           access time */
        if ( lpLastWriteTime &&
             CompareFileTime(lpLastAccessTime, lpLastWriteTime) < 0 )
        {
            *lpLastAccessTime = *lpLastWriteTime;
        }
    }

InternalGetFileTimeExit:
    if (NULL != pLocalDataLock)
    {
        pLocalDataLock->ReleaseLock(pThread, FALSE);
    }

    if (NULL != pFileObject)
    {
        pFileObject->ReleaseReference(pThread);
    }

    return palError;
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


/*++
Function:
  FILEFileTimeToUnixTime

See FILEUnixTimeToFileTime above.

This function takes a win32 FILETIME structures, returns the equivalent
time_t value, and, if the nsec parameter is non-null, also returns the
nanoseconds.

NOTE: a 32-bit time_t is only capable of representing dates between
13 December 1901 and 19 January 2038. This function will calculate the
number of seconds (positive or negative) since the Unix epoch, however if
this value is outside of the range of 32-bit numbers, the result will be
truncated on systems with a 32-bit time_t.
--*/
time_t FILEFileTimeToUnixTime( FILETIME FileTime, long *nsec )
{
    __int64 UnixTime;

    /* get the full win32 value, in 100ns */
    UnixTime = ((__int64)FileTime.dwHighDateTime << 32) + 
        FileTime.dwLowDateTime;

    /* convert to the Unix epoch */
    UnixTime -= (SECS_BETWEEN_1601_AND_1970_EPOCHS * SECS_TO_100NS);

    TRACE("nsec=%p\n", nsec);

    if ( nsec )
    {
        /* get the number of 100ns, convert to ns */
        *nsec = (UnixTime % SECS_TO_100NS) * 100;
    }

    UnixTime /= SECS_TO_100NS; /* now convert to seconds */

    if ( (time_t)UnixTime != UnixTime )
    {
        WARN("Resulting value is too big for a time_t value\n");
    }

    TRACE("Win32 FILETIME = [%#x:%#x] converts to Unix time = [%ld.%09ld]\n", 
          FileTime.dwHighDateTime, FileTime.dwLowDateTime ,(long) UnixTime,
          nsec?*nsec:0L);

    return (time_t)UnixTime;
}



/**
Function 

    FileTimeToSystemTime()
    
    Helper function for FileTimeToDosTime.
    Converts the necessary file time attibutes to system time, for
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
        lpSystemTime->wDay      = UnixSystemTime->tm_mday;
    
        /* Unix time counts January as a 0, under Windows it is 1*/
        lpSystemTime->wMonth    = UnixSystemTime->tm_mon + 1;
        /* Unix time returns the year - 1900, Windows returns the current year*/
        lpSystemTime->wYear     = UnixSystemTime->tm_year + 1900;
        
        lpSystemTime->wSecond   = UnixSystemTime->tm_sec;
        lpSystemTime->wMinute   = UnixSystemTime->tm_min;
        lpSystemTime->wHour     = UnixSystemTime->tm_hour;
        return TRUE;
    }
    else
    {
        ERROR( "The file time is to large.\n" );
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
    }
}


    
/**
Function:
    FileTimeToDosDateTime
    
    Notes due to the difference between how BSD and Windows
    calculates time, this function can only repersent dates between
    1980 and 2037. 2037 is the upperlimit for the BSD time functions( 1900 - 
    2037 range ).
    
See msdn for more details.
--*/
BOOL
PALAPI
FileTimeToDosDateTime(
            IN CONST FILETIME *lpFileTime,
            OUT LPWORD lpFatDate,
            OUT LPWORD lpFatTime )
{
    BOOL bRetVal = FALSE;

    PERF_ENTRY(FileTimeToDosDateTime);
    ENTRY( "FileTimeToDosDateTime( lpFileTime=%p, lpFatDate=%p, lpFatTime=%p )\n",
           lpFileTime, lpFatDate, lpFatTime );

    /* Sanity checks. */
    if ( !lpFileTime || !lpFatDate || !lpFatTime )
    {
        ERROR( "Incorrect parameters.\n" );
        SetLastError( ERROR_INVALID_PARAMETER );
    }
    else
    {
        /* Do conversion. */
        SYSTEMTIME SysTime;
        if ( FileTimeToSystemTime( lpFileTime, &SysTime ) )
        {
            if ( SysTime.wYear >= 1980 && SysTime.wYear <= 2037 )
            {
                *lpFatDate = 0;
                *lpFatTime = 0;

                *lpFatDate |= ( SysTime.wDay & 0x1F );
                *lpFatDate |= ( ( SysTime.wMonth & 0xF ) << 5 );
                *lpFatDate |= ( ( ( SysTime.wYear - 1980 ) & 0x7F ) << 9 );

                if ( SysTime.wSecond % 2 == 0 )
                {
                    *lpFatTime |= ( ( SysTime.wSecond / 2 )  & 0x1F );
                }
                else
                {
                    *lpFatTime |= ( ( SysTime.wSecond / 2 + 1 )  & 0x1F );
                }

                *lpFatTime |= ( ( SysTime.wMinute & 0x3F ) << 5 );
                *lpFatTime |= ( ( SysTime.wHour & 0x1F ) << 11 );
                
                bRetVal = TRUE;
            }
            else
            {
                ERROR( "The function can only repersent dates between 1/1/1980"
                       " and 12/31/2037\n" );
                SetLastError( ERROR_INVALID_PARAMETER );
            }
        }
        else
        {
            ERROR( "Unable to convert file time to system time.\n" );
            SetLastError( ERROR_INVALID_PARAMETER );
            bRetVal = FALSE;
        }
    }

    LOGEXIT( "returning BOOL %d\n", bRetVal );
    PERF_EXIT(FileTimeToDosDateTime);
    return bRetVal;
}

