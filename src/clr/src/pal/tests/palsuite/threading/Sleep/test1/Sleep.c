//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main( int argc, char **argv ) 
{
    DWORD OldTickCount;
    DWORD NewTickCount;
    DWORD MaxDelta;
    DWORD TimeDelta;
    DWORD i;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    for( i = 0; i < sizeof(SleepTimes) / sizeof(DWORD); i++)
    {
        OldTickCount = GetTickCount();
        Sleep(SleepTimes[i]);
        NewTickCount = GetTickCount();

        /* 
         * Check for DWORD wraparound
         */
        if (OldTickCount>NewTickCount)
        {
            OldTickCount -= NewTickCount+1;
            NewTickCount  = 0xFFFFFFFF;
        }
        TimeDelta = NewTickCount-OldTickCount;

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
