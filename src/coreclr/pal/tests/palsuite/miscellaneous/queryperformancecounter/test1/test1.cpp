// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test1.c
**
** Purpose: Test for QueryPerformanceCounter function
**
**
**=========================================================*/

/* Depends on: QueryPerformanceFrequency. */

#include <palsuite.h>


PALTEST(miscellaneous_queryperformancecounter_test1_paltest_queryperformancecounter_test1, "miscellaneous/queryperformancecounter/test1/paltest_queryperformancecounter_test1")
{
    /* Milliseconds of error which are acceptable Function execution time, etc.
    FreeBSD has a "standard" resolution of 50ms for waiting operations, so we
    must take that into account as well */
    DWORD AcceptableTimeError = 15; 

    int           i;
    int           NumIterations = 100;
    DWORD         AvgTimeDiff;
    DWORD         TimeDiff[100];
    DWORD         TotalTimeDiff = 0;
    DWORD         SleepInterval = 50;
    LARGE_INTEGER StartTime;
    LARGE_INTEGER EndTime;
    LARGE_INTEGER Freq;

    /* Initialize the PAL.
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Get the frequency of the High-Performance Counter,
     * in order to convert counter time to milliseconds.
     */
    if (!QueryPerformanceFrequency(&Freq))
    {
        Fail("ERROR:%u:Unable to retrieve the frequency of the "
             "high-resolution performance counter.\n", 
             GetLastError());
    }

    /* Perform this set of sleep timings a number of times.
     */
    for(i=0; i < NumIterations; i++)
    {

        /* Get the current counter value.
        */ 
        if (!QueryPerformanceCounter(&StartTime))
        {
            Fail("ERROR:%u:Unable to retrieve the current value of the "
                "high-resolution performance counter.\n", 
                GetLastError());
        }

        /* Sleep a predetermined interval.
        */
        Sleep(SleepInterval);

        /* Get the new current counter value.
        */
        if (!QueryPerformanceCounter(&EndTime))
        {
            Fail("ERROR:%u:Unable to retrieve the current value of the "
                "high-resolution performance counter.\n", 
                GetLastError());
        }

        /* Determine elapsed time, in milliseconds. Compare the elapsed time
         * with the sleep interval, and add to counter.
         */
        TimeDiff[i] = (DWORD)(((EndTime.QuadPart - StartTime.QuadPart)*1000)/
                             (Freq.QuadPart));
        TotalTimeDiff += TimeDiff[i] - SleepInterval;

    }

    /* Verify that the average of the difference between the performance 
     * counter and the sleep interval is within our acceptable range.
     */
    AvgTimeDiff = TotalTimeDiff / NumIterations;
    if (AvgTimeDiff > AcceptableTimeError)
    {
        Fail("ERROR:  average diff %u acceptable %u.\n",
            AvgTimeDiff,
            AcceptableTimeError);
    }

    /* Terminate the PAL.
     */  
    PAL_Terminate();
    return PASS;
}


