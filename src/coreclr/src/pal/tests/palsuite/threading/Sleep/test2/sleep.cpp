// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: Sleep.c
**
** Purpose: Test to establish whether the Sleep function stops the thread from 
** executing for the specified times.
**
** Dependencies: GetTickCount
** 

**
**=========================================================*/

#include <palsuite.h>

PALTEST(threading_Sleep_test2_paltest_sleep_test2, "threading/Sleep/test2/paltest_sleep_test2")
{
    /* 
    * times in 10^(-3) seconds
    */

    DWORD SleepTimes[] =
    {
        60000,
        300000,
        1800000,
        3200000
    };

    /* Milliseconds of error which are acceptable Function execution time, etc. */
    DWORD AcceptableTimeError = 150;

    UINT64 OldTimeStamp;
    UINT64 NewTimeStamp;
    DWORD MaxDelta;
    DWORD TimeDelta;
    DWORD i;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    LARGE_INTEGER performanceFrequency;
    if (!QueryPerformanceFrequency(&performanceFrequency))
    {
        return FAIL;
    }

    for( i = 0; i < sizeof(SleepTimes) / sizeof(DWORD); i++)
    {
        OldTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);
        Sleep(SleepTimes[i]);
        NewTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);

        TimeDelta = NewTimeStamp - OldTimeStamp;

        MaxDelta = SleepTimes[i] + AcceptableTimeError;

        if ( TimeDelta<SleepTimes[i] || TimeDelta>MaxDelta )
        {
            Fail("The sleep function slept for %u ms when it should have "
             "slept for %u ms\n", TimeDelta, SleepTimes[i]);
       }
    }
    PAL_Terminate();
    return ( PASS );

}
