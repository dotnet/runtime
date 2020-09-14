// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests that SleepEx correctly sleeps for a given amount of time,
**          regardless of the alertable flag.
**
**
**===================================================================*/

#include <palsuite.h>

typedef struct
{
    DWORD SleepTime;
    BOOL Alertable;
} testCase;

PALTEST(threading_SleepEx_test1_paltest_sleepex_test1, "threading/SleepEx/test1/paltest_sleepex_test1")
{
    /* Milliseconds of error which are acceptable Function execution time, etc. */
    DWORD AcceptableTimeError = 150;

    testCase testCases[] =
    {
        {0, FALSE},
        {50, FALSE},
        {100, FALSE},
        {500, FALSE},
        {2000, FALSE},

        {0, TRUE},
        {50, TRUE},
        {100, TRUE},
        {500, TRUE},
        {2000, TRUE},
    };

    UINT64 OldTimeStamp;
    UINT64 NewTimeStamp;
    DWORD MaxDelta;
    DWORD TimeDelta;
    DWORD i;

    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    LARGE_INTEGER performanceFrequency;
    if (!QueryPerformanceFrequency(&performanceFrequency))
    {
        return FAIL;
    }

    for (i = 0; i<sizeof(testCases) / sizeof(testCases[0]); i++)
    {
        OldTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);

        SleepEx(testCases[i].SleepTime, testCases[i].Alertable);

        NewTimeStamp = GetHighPrecisionTimeStamp(performanceFrequency);

        TimeDelta = NewTimeStamp - OldTimeStamp;

        /* For longer intervals use a 10 percent tolerance */
        if ((testCases[i].SleepTime * 0.1) > AcceptableTimeError)
        {
            MaxDelta = testCases[i].SleepTime + (DWORD)(testCases[i].SleepTime * 0.1);
        }
        else
        {
            MaxDelta = testCases[i].SleepTime + AcceptableTimeError;
        }

        if (TimeDelta < testCases[i].SleepTime || TimeDelta > MaxDelta)
        {
            Fail("The sleep function slept for %d ms when it should have "
             "slept for %d ms\n", TimeDelta, testCases[i].SleepTime);
       }
    }

    PAL_Terminate();
    return PASS;
}
