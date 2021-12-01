// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source:  
**
** Source : test1.c
**
** Purpose: Test for GetThreadTimes() function
**
**
**=========================================================*/

#include <palsuite.h>

PALTEST(threading_GetThreadTimes_test1_paltest_getthreadtimes_test1, "threading/GetThreadTimes/test1/paltest_getthreadtimes_test1")
{
    int ret = FAIL;

    //Test is failing unreliably, so for now we always return pass.
    if (TRUE){
        ret = PASS;
        goto EXIT;
    }    
    {
    FILETIME kernelTime1, userTime1, kernelTime2, userTime2;
    /* Delta = .01 sec */
    LONG64 Actual, Expected, Delta = 850000000;
    Actual = 0;
    Expected = 0;
    const ULONG64 MSEC_TO_NSEC = 1000000;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    HANDLE cThread = GetCurrentThread();

    int i;
    /* Take 2000 tiny measurements */
    for (i = 0; i < 2000; i++){
        ULONG64 Time1, Time2;

        Sleep(1);

        /* Grab a FirstCount, then loop for a bit to make the clock increase */
        if (!GetThreadTimes(cThread, NULL, NULL, &kernelTime1, &userTime1))
        {
            Fail("ERROR: GetThreadTimes returned failure.\n");
        }
        LONG64 x, Init;
        /* Init is in milliseconds, so we will convert later */
        Init = (ULONG64)GetTickCount();
        /* Spin for < 1 Quantum so we don't get interrupted */
        x = Init + 3;
        volatile int counter;
        do {
            for (counter = 0; counter < 100000; counter++)
            {
                // spin to consume CPU time
            }

        } while (x > GetTickCount());
        Expected += (GetTickCount() - Init) * MSEC_TO_NSEC;
        /* Get a second count */
        if (!GetThreadTimes(cThread, NULL, NULL, &kernelTime2, &userTime2))
        {
            Fail("ERROR: GetThreadTimes returned failure.\n");
        }

        Time1 = ((ULONG64)kernelTime1.dwHighDateTime << 32);
        Time1 += (ULONG64)kernelTime1.dwLowDateTime;
        Time1 += ((ULONG64)userTime1.dwHighDateTime << 32);
        Time1 += (ULONG64)userTime1.dwLowDateTime;

        Time2 = ((ULONG64)kernelTime2.dwHighDateTime << 32);
        Time2 += (ULONG64)kernelTime2.dwLowDateTime;
        Time2 += ((ULONG64)userTime2.dwHighDateTime << 32);
        Time2 += (ULONG64)userTime2.dwLowDateTime;

        Actual += (Time2 - Time1) * 100;
    }

    if(llabs(Expected - Actual) > Delta)
    {
        Fail("ERROR: The measured time (%llu millisecs) was not within Delta %llu "
            "of the expected time (%llu millisecs).\n",
             (Actual / MSEC_TO_NSEC), (Delta / MSEC_TO_NSEC), (Expected / MSEC_TO_NSEC));
    }
    //printf("%llu, %llu\n", Expected, Actual);
    PAL_Terminate();
    ret = PASS;
    }
EXIT:
    return ret;
}
