// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetSystemTimeAsFileTime.c
**
** Purpose: Tests the PAL implementation of GetSystemTimeAsFileTime
** Take two times, three seconds apart, and ensure that the time is 
** increasing, and that it has increased at least 3 seconds.
**
**
**
**===================================================================*/

#include <palsuite.h>


PALTEST(file_io_GetSystemTimeAsFileTime_test1_paltest_getsystemtimeasfiletime_test1, "file_io/GetSystemTimeAsFileTime/test1/paltest_getsystemtimeasfiletime_test1")
{

    FILETIME TheFirstTime, TheSecondTime;
    ULONG64 FullFirstTime, FullSecondTime;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
  
    /* Get two times, 3 seconds apart */

    GetSystemTimeAsFileTime( &TheFirstTime );

    Sleep( 3000 );
  
    GetSystemTimeAsFileTime( &TheSecondTime );

    /* Convert them to ULONG64 to work with */

    FullFirstTime = ((( (ULONG64)TheFirstTime.dwHighDateTime )<<32) | 
                     ( (ULONG64)TheFirstTime.dwLowDateTime ));
  
    FullSecondTime = ((( (ULONG64)TheSecondTime.dwHighDateTime )<<32) | 
                      ( (ULONG64)TheSecondTime.dwLowDateTime ));

    /* Test to ensure the second value is larger than the first */

    if( FullSecondTime <= FullFirstTime )
    {
        Fail("ERROR:  The system time didn't increase in the last "
               "three seconds.  The second time tested was less than "
               "or equal to the first.");
    }

    /* Note: The 30000000 magic number is 3 seconds in hundreds of nano
       seconds.  This test checks to ensure at least 3 seconds passed
       between the readings.
    */
  
    if( ( (LONG64)( FullSecondTime - FullFirstTime ) - 30000000 ) < 0 )
    {
        ULONG64 TimeError;
        
        /* Note: This test used to compare the difference between full times
        in terms of hundreds of nanoseconds.  But the x86 clock seems to be 
        precise only to the level of about 10000 nanoseconds, so we would 
        fail the comparison depending on when we took time slices.  
       
        To fix this, we just check that we're within a millisecond of
        sleeping 3000 milliseconds.  We're not currently ensuring that we 
        haven't slept much more than 3000 ms.  We may want to do that.
        */
        TimeError = 30000000 - ( FullSecondTime - FullFirstTime );
        if ( TimeError > 10000)
        {
        Fail("ERROR: Two system times were tested, with a sleep of 3 "
               "seconds between.  The time passed should have been at least "
               "3 seconds.  But, it was less according to the function.");
        }
    }

    PAL_Terminate();
    return PASS;
}

