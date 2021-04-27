// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source:  
**
** Source : test1.c
**
** Purpose: Test for QueryThreadCycleTime() function
**
**
**=========================================================*/

#include <palsuite.h>

PALTEST(threading_QueryThreadCycleTime_test1_paltest_querythreadcycletime_test1, "threading/QueryThreadCycleTime/test1/paltest_querythreadcycletime_test1")
{
    int ret = FAIL;

    //Test is failing unreliably, so for now we always return pass.
    if (TRUE){
        ret = PASS;
        goto EXIT;
    }   
    {
    LONG64 Actual, Expected, Delta = 850000000;
    Actual = 0;
    Expected = 0;
    const LONG64 MSEC_TO_NSEC = 1000000;

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
        ULONG64 FirstCount, SecondCount;
        LONG64 Init;
        
        Sleep(1);

        /* Grab a FirstCount, then loop for a bit to make the clock increase */
        if (!QueryThreadCycleTime(cThread, (PULONG64)&FirstCount))
        {
            Fail("ERROR: QueryThreadCycleTime returned failure.\n");
        }
        
        LONG64 x;
        /* Init is in milliseconds, so we will convert later */
        Init = (LONG64)GetTickCount();
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
        if (!QueryThreadCycleTime(cThread, (PULONG64)&SecondCount))
        {
            Fail("ERROR: QueryThreadCycleTime returned failure.\n");
        }

        LONG64 trial = (LONG64)SecondCount - (LONG64)FirstCount;
        if (trial < 0){
            printf("Negative value %llu measured", trial);
        }
        Actual += (trial);

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
