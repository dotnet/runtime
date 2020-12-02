// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test1.c
**
** Purpose: Test to ensure that IsBadCodePtr return 0 when
**          it can read memory or non zero when it can't.
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**				 Fail
** 

**
**===========================================================================*/

#include <palsuite.h>

/**
 * main
 * 
 * executable entry point
 */
PALTEST(miscellaneous_IsBadCodePtr_test1_paltest_isbadcodeptr_test1, "miscellaneous/IsBadCodePtr/test1/paltest_isbadcodeptr_test1")
{
    BOOL ResultValue = 0;
  
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }     

    /* This should be readable, and return 0 */
    ResultValue = IsBadCodePtr((FARPROC)main);
    if(ResultValue != 0) 
    {
        Fail("ERROR: IsBadCodePtr returned %d instead of 0, when pointing "
             "at readable memory.\n",ResultValue);    
    }

    /* 0x00 is usually unreadable memory so the function should 
       return non zero */
    ResultValue = IsBadCodePtr((FARPROC)0x00);

    if(ResultValue == 0)
    {
        Fail("ERROR: IsBadCodePtr returned %d instead of non zero  "
             "when checking on unreadable memory.\n",ResultValue);
    }

    PAL_Terminate();
    return PASS;
}
