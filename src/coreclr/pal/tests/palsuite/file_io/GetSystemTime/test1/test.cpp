// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source:  test.c
**
** Purpose: Test for GetSystemTime() function
**
**
**=========================================================*/

/* Note:  Some of the range comparisons only check
 * the high end of the range.  Since the structure
 * contains WORDs, negative values can never be included,
 * so there is no reason to check and see if the lower
 * end of the spectrum is <0
*/

#include <palsuite.h>


PALTEST(file_io_GetSystemTime_test1_paltest_getsystemtime_test1, "file_io/GetSystemTime/test1/paltest_getsystemtime_test1")
{
    SYSTEMTIME TheTime;
    SYSTEMTIME firstTime;
    SYSTEMTIME secondTime;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    GetSystemTime(&TheTime);

    /* Go through each item in the structure and ensure it is a valid value.
    We can't ensure they have the exact values of the current time, but 
    at least that they have been set to a valid range of values for that 
    item.
    */

    /* Year */
    if(TheTime.wYear < (WORD)2001) 
    {
        Fail("ERROR: The year is %d, when it should be at least 2001.",         
            TheTime.wYear);
    }

    /* Month */
    if(TheTime.wMonth > (WORD)12 || TheTime.wMonth < (WORD)1) 
    {
        Fail("ERROR: The month should be between 1 and 12, and it is "
            "showing up as %d.",TheTime.wMonth);
    }

    /* Weekday */
    if(TheTime.wDayOfWeek > 6) 
    {
        Fail("ERROR: The day of the week should be between 0 and 6, "
            "and it is showing up as %d.",TheTime.wDayOfWeek);
    }

    /* Day of the Month */
    if(TheTime.wDay > 31 || TheTime.wDay < 1) 
    {
        Fail("ERROR: The day of the month should be between 1 and "
            "31, and it is showing up as %d.",TheTime.wDay);
    }

    /* Hour */
    if(TheTime.wHour > 23) 
    {
        Fail("ERROR: The hour should be between 0 and 23, and it is "
            "showing up as %d.",TheTime.wHour);
    }

    /* Minute */
    if(TheTime.wMinute > 59) 
    {
        Fail("ERROR: The minute should be between 0 and 59 and it is "
            "showing up as %d.",TheTime.wMinute);
    }

    /* Second */
    if(TheTime.wSecond > 59) 
    {
        Fail("ERROR: The second should be between 0 and 59 and it is" 
            " showing up as %d.",TheTime.wSecond);
    }

    /* Millisecond */
    if(TheTime.wMilliseconds > 999) 
    {
        Fail("ERROR:  The milliseconds should be between 0 and 999 "
            "and it is currently showing %d.",TheTime.wMilliseconds);
    }

    /* verify that two consecutive calls to system time return */
    /* different values of seconds across sleep() call. */
    /* do this 5 times in case we are super unlucky. */
    float avgDeltaFileTime = 0;
    for (int i = 0; i < 5; i++)
    {
        GetSystemTime(&firstTime);
        Sleep(1000);
        GetSystemTime(&secondTime);
        avgDeltaFileTime += abs(firstTime.wSecond - secondTime.wSecond);
    }

    if( (avgDeltaFileTime / 5) == 0)
    {
        Fail("ERROR: GetSystemTime always returning same value of seconds Value[%f]", avgDeltaFileTime / 5);
    }

    PAL_Terminate();
    return PASS;
}

