//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

SET_DEFAULT_DEBUG_CHANNEL(MISC);

typedef struct
{
    struct tm dst_date;
    struct tm std_date;
} TRANS_DATES;

static TRANS_DATES *glob_trans_dates = NULL;

static  BOOL TIMEGetStdDstDates(int year, struct tm *dst_date, struct tm *std_date);
static  int  TIMEGetWeekDayNumber(struct tm date);

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
  GetTimeZoneInformation

The GetTimeZoneInformation function retrieves the current time-zone
parameters. These parameters control the translations between
Coordinated Universal Time (UTC) and local time.

Parameters

lpTimeZoneInformation 
       [out] Pointer to a TIME_ZONE_INFORMATION structure to receive
       the current time-zone parameters.

Return Values

If the function succeeds, the return value is one of the following values: 

TIME_ZONE_ID_UNKNOWN
TIME_ZONE_ID_STANDARD
TIME_ZONE_ID_DAYLIGHT
TIME_ZONE_ID_INVALID

--*/
DWORD
PALAPI
PAL_GetTimeZoneInformation(
               IN int year,
               OUT LPTIME_ZONE_INFORMATION lpTimeZoneInformation)
{
#if HAVE_TIMEZONE_VAR
    extern long int timezone;
#endif  // HAVE_TIMEZONE_VAR
    time_t tt;
    const char *tzStandardName;
    const char *tzDaylightName;
#if HAVE_LOCALTIME_R
    struct tm ut;
#endif  /* HAVE_LOCALTIME_R */
    struct tm *utPtr;
    DWORD retval = TIME_ZONE_ID_INVALID;
    struct tm dst_date, std_date;
    struct tm local_date;

    PERF_ENTRY(GetTimeZoneInformation);
    ENTRY("PAL_GetTimeZoneInformation (year=%d, lpTimeZoneInformation=%p)\n", 
          year, lpTimeZoneInformation);    

    
    memset(&local_date, 0, sizeof(struct tm));
    local_date.tm_year = year-1900;
    local_date.tm_mday    = 1;

    if (-1 == (tt = mktime(&local_date)))
    {
        ERROR("mktime() failed.\n");
        return FALSE;
    }
    
#if HAVE_LOCALTIME_R
    utPtr = &ut;
    localtime_r(&tt,&ut);
#else   /* HAVE_LOCALTIME_R */
    utPtr = localtime(&tt);
#endif  /* HAVE_LOCALTIME_R */

#if HAVE_TZNAME
    tzStandardName = tzname[0];
    tzDaylightName = tzname[1];
#else   /* HAVE_TZNAME */
    tzStandardName = utPtr->tm_zone;
    tzDaylightName = "";
#endif  /* HAVE_TZNAME */
    TRACE("standard timezone name = %s\n", tzStandardName);
    TRACE("daylight timezone name = %s\n", tzDaylightName);
#if HAVE_TM_GMTOFF
    TRACE("timezone offset = %ld\n", utPtr->tm_gmtoff / 60);
#else
    TRACE("timezone offset = %ld\n", (-timezone + (utPtr->tm_isdst ? 3600 : 0)) / 60);
#endif
    TRACE("daylight savings = %d\n", utPtr->tm_isdst);

#if HAVE_TM_GMTOFF
    lpTimeZoneInformation->Bias = -utPtr->tm_gmtoff / 60;
#else
    lpTimeZoneInformation->Bias = (timezone - (utPtr->tm_isdst ? 3600 : 0)) / 60;
#endif
    
    /* This may be wrong for some obscure time zones. */
    lpTimeZoneInformation->StandardBias = 0;
    lpTimeZoneInformation->DaylightBias = -60;

    if (MultiByteToWideChar(CP_ACP, 0, tzStandardName, -1,
            lpTimeZoneInformation->StandardName, 32) == 0)
    {
        ASSERT("failed to convert multibytes to wide chars\n");
        goto EXIT;
    }
    
    if (MultiByteToWideChar(CP_ACP, 0, tzDaylightName, -1,
            lpTimeZoneInformation->DaylightName, 32) == 0)
    {
        ASSERT("failed to convert multibytes to wide chars\n");
        goto EXIT;
    }
    
    memset(&lpTimeZoneInformation->StandardDate, 0, sizeof(SYSTEMTIME));
    memset(&lpTimeZoneInformation->DaylightDate, 0, sizeof(SYSTEMTIME));

    if (utPtr->tm_isdst >= 0)
    {
        if (!TIMEGetStdDstDates(year, &dst_date, &std_date))
        {
            goto EXIT;
        }

        if ((dst_date.tm_mon == 0) || (std_date.tm_mon == 0))
        {
            retval = TIME_ZONE_ID_STANDARD;
            goto EXIT;
        }

        TRACE("Transition to Standard date: %s\n", asctime(&dst_date));
        TRACE("Transition to Daylight date: %s\n", asctime(&std_date));

        lpTimeZoneInformation->StandardDate.wDay = TIMEGetWeekDayNumber(std_date);
        lpTimeZoneInformation->DaylightDate.wDay = TIMEGetWeekDayNumber(dst_date);

        if(!lpTimeZoneInformation->StandardDate.wDay || 
           !lpTimeZoneInformation->DaylightDate.wDay)
        {
            goto EXIT;
        }
        
        lpTimeZoneInformation->StandardDate.wMonth = std_date.tm_mon + 1;
        lpTimeZoneInformation->DaylightDate.wMonth = dst_date.tm_mon + 1;
        lpTimeZoneInformation->StandardDate.wDayOfWeek = std_date.tm_wday;
        lpTimeZoneInformation->DaylightDate.wDayOfWeek = dst_date.tm_wday;
        lpTimeZoneInformation->StandardDate.wHour = (std_date.tm_hour + 1) % 24;
        lpTimeZoneInformation->DaylightDate.wHour = (dst_date.tm_hour + 1) % 24;
        
        TRACE("StandardDate: mon:%d dofweek:%d hour:%d wday:%d\n", 
               lpTimeZoneInformation->StandardDate.wMonth, 
               lpTimeZoneInformation->StandardDate.wDayOfWeek,
               lpTimeZoneInformation->StandardDate.wHour,
               lpTimeZoneInformation->StandardDate.wDay);
        
        TRACE("DaylightDate: mon:%d dofweek:%d hour:%d wday:%d\n", 
               lpTimeZoneInformation->DaylightDate.wMonth, 
               lpTimeZoneInformation->DaylightDate.wDayOfWeek,
               lpTimeZoneInformation->DaylightDate.wHour,
               lpTimeZoneInformation->DaylightDate.wDay);
        
        if (utPtr->tm_isdst)
        {
            lpTimeZoneInformation->Bias -= lpTimeZoneInformation->DaylightBias;
            retval = TIME_ZONE_ID_DAYLIGHT;
        }
        else
        {
            lpTimeZoneInformation->Bias -= lpTimeZoneInformation->StandardBias;
            retval = TIME_ZONE_ID_STANDARD;
        }
    }
    else 
    {
        retval = TIME_ZONE_ID_UNKNOWN;
    }
    
EXIT:
    LOGEXIT("PAL_GetTimeZoneInformation returns DWORD %u\n", retval);
    PERF_EXIT(GetTimeZoneInformation);
    return retval;
}


DWORD
PALAPI
GetTimeZoneInformation(
               OUT LPTIME_ZONE_INFORMATION lpTimeZoneInformation)
{
    time_t tt;
#if HAVE_LOCALTIME_R
    struct tm ut;
#endif  /* HAVE_LOCALTIME_R */
    struct tm *utPtr;

    tt = time(NULL);
#if HAVE_LOCALTIME_R
    utPtr = &ut;
    localtime_r(&tt,&ut);
#else   /* HAVE_LOCALTIME_R */
    utPtr = localtime(&tt);
#endif  /* HAVE_LOCALTIME_R */

    return PAL_GetTimeZoneInformation (utPtr->tm_year + 1900,lpTimeZoneInformation);
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

In the ROTOR implementation the return value is the elapsed time since
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

/*++
Function:
  TIMEGetWeekDayNumber

calculate the week day number of the indicated date. for example, if the week day 
is Sunday, this function will returns 1 if it's the first Sunday of the month, 
2 if it's the second, etc. and 5 if it's the last Sunday on the month.

Parameters
date_param: the date for which we are going to calculate the week day order.

Return Values

returns the week day number (range 1-5), 0 if it fails.
--*/
int TIMEGetWeekDayNumber(struct tm date_param)
{
    time_t date_t;
    struct tm date = date_param;
    struct tm *datePtr;
    int month = date.tm_mon;
    
    /* first, check if it's the last week-day of the month */
    date.tm_mday += 7;
    date_t = mktime(&date);
#if HAVE_LOCALTIME_R
    datePtr = &date;
    if (NULL == localtime_r(&date_t, &date))
#else   /* HAVE_LOCALTIME_R */
    if (NULL == (datePtr = localtime(&date_t)))
#endif  /* HAVE_LOCALTIME_R */
    {
        ERROR("localtime() failed.\n");
        return 0;
    }
    
    if (datePtr->tm_mon != month)
    {
        return 5;
    }

    return (date_param.tm_mday + 6) / 7;
}

/*++
Function:
  TIMEGetStdDstDates

retrieve the two dates preceding the two transition dates:
    1- from standard date to daylight date
    2- from daylight date to standard date

if daylight/standard date doesn't apply for the current zone, then tm_mon of 
dst_date and std_date parameters will be set to 0.

Parameters
    dst_date: return transition date from standard date to daylight date
    std_date: return transition date from daylight date to standard date

Return Values
    FALSE, if it fails, TRUE, otherwise.
--*/
BOOL TIMEGetStdDstDates(int year, struct tm *dst_date, struct tm *std_date)
{
    const int sec_per_year = 31536000; /* 365x24x60x60 */
    
    int dayLightActive;
   /* the POSIX time_t type is opaque; to be portable, we should assume
       nothing about its representation. */
    time_t date_LO;
    time_t date_MID;
    time_t date_HI;
    time_t date_HI_Copy;
    struct tm local_date;
    struct tm date_MID_tm;
#if HAVE_LOCALTIME_R
    struct tm tmp_tm;
#endif  /* HAVE_LOCALTIME_R */
    struct tm *dateMIDPtr;
    struct tm *tmpPtr;
    
    TRANS_DATES tmp_transd, *lptmp_transd;
    memset(&tmp_transd, 0, sizeof(tmp_transd));

    /* initialize date_LO to the first date of the current year, and date_HI to
       the last date */
    
    if( (glob_trans_dates) && 
        (glob_trans_dates->dst_date.tm_year == year - 1900))
    {
        /* tansition dates already calculated */
        TRACE("Tansition dates already calculated\n");
        *dst_date = glob_trans_dates->dst_date;
        *std_date = glob_trans_dates->std_date;
        return TRUE;
    }
    
    memset(&local_date, 0, sizeof(struct tm));
    local_date.tm_year = year - 1900;
    local_date.tm_mday    = 1;
    
    if (-1 == (date_LO = mktime(&local_date)))
    {
        ERROR("mktime() failed.\n");
        return FALSE;
    }

    date_HI_Copy = date_HI = date_LO +  sec_per_year;

    /* get the initial daylight status */
#if HAVE_LOCALTIME_R
    tmpPtr = &tmp_tm;
    if (NULL == localtime_r(&date_LO, &tmp_tm))
#else   /* HAVE_LOCALTIME_R */
    if (NULL == (tmpPtr = localtime(&date_LO)))
#endif  /* HAVE_LOCALTIME_R */
    {
        ERROR("localtime_r() failed.\n");
        return FALSE;
    }
    
    dayLightActive = tmpPtr->tm_isdst;

    /* retrieve the date before the first transition to/from daylight/standard 
       date happens */
    do
    {
        date_MID = date_LO + difftime (date_HI, date_LO) / 2.0;
        
#if HAVE_LOCALTIME_R
        dateMIDPtr = &date_MID_tm;
        if (NULL == localtime_r(&date_MID, &date_MID_tm))
#else   /* HAVE_LOCALTIME_R */
        if (NULL == (dateMIDPtr = localtime(&date_MID)))
#endif  /* HAVE_LOCALTIME_R */
        {
            ERROR("localtime_r() failed.\n");
            return FALSE;
        }
        
        if(dateMIDPtr->tm_isdst != dayLightActive)
        {
           date_HI = date_MID - 1;
        }
        else
        {               
           date_LO = date_MID + 1;
        }
    }    
    while (!(difftime (date_LO, date_HI) > 0)); // date_LO <= date_HI
    
    if(dateMIDPtr->tm_isdst != dayLightActive)
    {
        /* passed the transition date, move back by one second */
        dateMIDPtr->tm_sec--;
        date_MID = mktime(dateMIDPtr);
#if HAVE_LOCALTIME_R
        localtime_r(&date_MID, dateMIDPtr);
#else   /* HAVE_LOCALTIME_R */
        dateMIDPtr = localtime(&date_MID);
#endif  /* HAVE_LOCALTIME_R */
    }
    
    if (date_HI_Copy == date_MID)
    {
        /* no daylight/standard date for the current zone */
        memset(dst_date, 0, sizeof(*dst_date));
        memset(std_date, 0, sizeof(*std_date));
        return TRUE;
    }
    
    if (dayLightActive == 0)
    {
        tmp_transd.dst_date = *dateMIDPtr;
        dayLightActive = 1;
    }
    else
    {
        tmp_transd.std_date = *dateMIDPtr;
        dayLightActive = 0;
    }
    
    /* retrieve the date before the second transition to/from daylight/standard 
       date happens */
    date_HI = date_HI_Copy;    
    date_MID_tm.tm_sec++;
    date_LO = mktime(dateMIDPtr);

    do
    {
        date_MID = date_LO + difftime (date_HI, date_LO) / 2.0;
        
#if HAVE_LOCALTIME_R
        dateMIDPtr = &date_MID_tm;
        if (NULL == localtime_r(&date_MID, dateMIDPtr))
#else   /* HAVE_LOCALTIME_R */
        if (NULL == (dateMIDPtr = localtime(&date_MID)))
#endif  /* HAVE_LOCALTIME_R */
        {
            ERROR("localtime() failed.\n");
            return FALSE;
        }
        
        if(dateMIDPtr->tm_isdst != dayLightActive)
        {
           date_HI = date_MID - 1;
        }
        else
        {               
           date_LO = date_MID + 1;
        }
    }    
    while (!(difftime (date_LO, date_HI) > 0)); // date_LO <= date_HI
    
    if(dateMIDPtr->tm_isdst != dayLightActive)
    {
        /* passed the transition date, move back by one second */
        date_MID_tm.tm_sec--;
        date_MID = mktime(dateMIDPtr);
#if HAVE_LOCALTIME_R
        dateMIDPtr = &date_MID_tm;
        localtime_r(&date_MID, dateMIDPtr);
#else   /* HAVE_LOCALTIME_R */
        dateMIDPtr = localtime(&date_MID);
#endif  /* HAVE_LOCALTIME_R */
    }
    
    if (dayLightActive == 0)
    {
        tmp_transd.dst_date = *dateMIDPtr;
    }
    else
    {
        tmp_transd.std_date = *dateMIDPtr;
    }
        
    if(glob_trans_dates == NULL)
    {
        if((lptmp_transd = (TRANS_DATES*)PAL_malloc(sizeof(TRANS_DATES))) == NULL)
        {
            ERROR("can't allocate memory for global transition dates\n");
            return FALSE;
        }
        
        *lptmp_transd = tmp_transd;

        if(NULL != InterlockedCompareExchangePointer(&glob_trans_dates, lptmp_transd, NULL))
        {
            /* another thread got in before us! */
            WARN("Another thread initialized global trans dates\n");
            PAL_free(lptmp_transd);
        }
    }
    else
    {
        *glob_trans_dates = tmp_transd;
    }
    
    *dst_date = glob_trans_dates->dst_date;
    *std_date = glob_trans_dates->std_date;

    return TRUE;
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
#if HAVE_CLOCK_MONOTONIC
    {
        struct timespec ts;
        if (clock_gettime(CLOCK_MONOTONIC, &ts) != 0)
        {
            ASSERT("clock_gettime(CLOCK_MONOTONIC) failed; errno is %d (%s)\n", errno, strerror(errno));
            retval = FALSE;
            goto EXIT;
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
            goto EXIT;
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
            goto EXIT;
        }
        lpPerformanceCount->QuadPart = 
            (LONGLONG)tv.tv_sec * (LONGLONG)tccSecondsToMicroSeconds + (LONGLONG)tv.tv_usec;    
    }
#endif // HAVE_CLOCK_MONOTONIC 
EXIT:
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
    PERF_ENTRY(QueryPerformanceFrequency);
    ENTRY("QueryPerformanceFrequency()\n");
#if HAVE_GETHRTIME || HAVE_READ_REAL_TIME || HAVE_CLOCK_MONOTONIC
    lpFrequency->QuadPart = (LONGLONG)tccSecondsToNanoSeconds;
#else
    lpFrequency->QuadPart = (LONGLONG)tccSecondsToMicroSeconds;
#endif // HAVE_GETHRTIME || HAVE_READ_REAL_TIME || HAVE_CLOCK_MONOTONIC 
    LOGEXIT("QueryPerformanceFrequency\n");
    PERF_EXIT(QueryPerformanceFrequency);
    return TRUE;
}

BOOL
PALAPI
QueryThreadCycleTime(
IN HANDLE ThreadHandle,
OUT PULONG64 CycleTime)
{
    // UNIXTODO: Implement this!
    ERROR("Needs Implementation!!!");
    return FALSE;
}

/*++
Function:
  TIMECleanUpTransitionDates

Free the memory allocated in TIMEGetStdDstDates.
--*/
void TIMECleanUpTransitionDates()
{
    if (glob_trans_dates)
    {
        PAL_free(glob_trans_dates);
        glob_trans_dates = 0;
    }
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

#if HAVE_CLOCK_MONOTONIC
    {
        struct timespec ts;
        if (clock_gettime(CLOCK_MONOTONIC, &ts) != 0)
        {
            ASSERT("clock_gettime(CLOCK_MONOTONIC) failed; errno is %d (%s)\n", errno, strerror(errno));
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
