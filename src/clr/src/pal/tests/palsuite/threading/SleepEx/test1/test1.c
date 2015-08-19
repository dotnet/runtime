//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
    DWORD OldTickCount;
    DWORD NewTickCount;
    DWORD MaxDelta;
    DWORD TimeDelta;
    DWORD i;

    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    for (i = 0; i<sizeof(testCases) / sizeof(testCases[0]); i++)
    {
        OldTickCount = GetTickCount();

        SleepEx(testCases[i].SleepTime, testCases[i].Alertable);

        NewTickCount = GetTickCount();

        /* 
         * Check for DWORD wraparound
         */
        if (OldTickCount>NewTickCount)
        {
            OldTickCount -= NewTickCount+1;
            NewTickCount  = 0xFFFFFFFF;
        }

        TimeDelta = NewTickCount - OldTickCount;

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
