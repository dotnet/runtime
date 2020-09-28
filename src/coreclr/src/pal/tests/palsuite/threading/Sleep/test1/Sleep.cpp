// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: Sleep.c
**
** Purpose: Test to establish whether the Sleep function stops the thread from 
** executing for the specified times.
**
** Dependencies: GetSystemTime
**               Fail   
**               Trace
** 

**
**=========================================================*/

#include <palsuite.h>

PALTEST(threading_Sleep_test1_paltest_sleep_test1, "threading/Sleep/test1/paltest_sleep_test1")
{
    DWORD SleepTimes[] =
    {
        0,
        50,
        100,
        500,
        2000
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

        /* For longer intervals use a 10 percent tolerance */
        if ((SleepTimes[i] * 0.1) > AcceptableTimeError)
        {
            MaxDelta = SleepTimes[i] + (DWORD)(SleepTimes[i] * 0.1);
        }
        else
        {
            MaxDelta = SleepTimes[i] + AcceptableTimeError;
        }

        if ( TimeDelta<SleepTimes[i] || TimeDelta>MaxDelta )
        {
            Fail("The sleep function slept for %d ms when it should have "
             "slept for %d ms\n", TimeDelta, SleepTimes[i]);
       }
    }
    PAL_Terminate();
    return ( PASS );

}
