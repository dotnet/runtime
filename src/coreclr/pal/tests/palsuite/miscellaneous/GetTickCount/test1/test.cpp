// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source:  
**
** Source : test1.c
**
** Purpose: Test for GetTickCount() function
**
**
**=========================================================*/

#include <palsuite.h>

PALTEST(miscellaneous_GetTickCount_test1_paltest_gettickcount_test1, "miscellaneous/GetTickCount/test1/paltest_gettickcount_test1")
{

    DWORD FirstCount = 0;
    DWORD SecondCount = 0;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Grab a FirstCount, then loop for a bit to make the clock increase */
    FirstCount = GetTickCount();
  
    /* Make sure some time passes */
	Sleep(60); //Since the get tick count is accurate within 55 milliseconds.

    /* Get a second count */
    SecondCount = GetTickCount();

    /* Make sure the second one is bigger than the first. 
       This isn't the best test, but it at least shows that it's returning a
       DWORD which is increasing.
    */
  
    if(FirstCount >= SecondCount) 
    {
        Fail("ERROR: The first time (%d) was greater/equal than the second time "
             " (%d).  The tick count should have increased.\n",
             FirstCount,SecondCount);
    }
    
    PAL_Terminate();
    return PASS;
}



