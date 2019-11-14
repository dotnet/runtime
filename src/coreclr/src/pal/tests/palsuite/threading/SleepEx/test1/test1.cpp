// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

/* Milliseconds of error which are acceptable Function execution time, etc. */
DWORD AcceptableTimeError = 150;

int __cdecl main( int argc, char **argv ) 
{
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
