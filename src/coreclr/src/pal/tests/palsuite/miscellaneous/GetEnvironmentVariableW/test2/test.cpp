// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source : test.c
**
** Purpose: Test for GetEnvironmentVariable() function
**
**
**=========================================================*/

#define UNICODE

#include <palsuite.h>

#define SMALL_BUFFER_SIZE 1

PALTEST(miscellaneous_GetEnvironmentVariableW_test2_paltest_getenvironmentvariablew_test2, "miscellaneous/GetEnvironmentVariableW/test2/paltest_getenvironmentvariablew_test2")
{
    
    WCHAR pSmallBuffer[SMALL_BUFFER_SIZE];
    
    /* A place to stash the returned values */
    int  ReturnValueForSmallBuffer;
    
    /*
     * Initialize the PAL and return FAILURE if this fails
     */
    
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    
    /* PATH won't fit in this buffer, it should return how many characters 
       it needs 
    */
    
    ReturnValueForSmallBuffer = GetEnvironmentVariable(convert("PATH"),       
                                                       pSmallBuffer,
                                                       SMALL_BUFFER_SIZE); 
    
    if(ReturnValueForSmallBuffer <= 0) 
    {
        Fail("The return value was %d when it should have been greater "
             "than 0. This should return the  number of characters needed "
             "to contained the contents of PATH in a buffer.\n",
             ReturnValueForSmallBuffer);      
    }
    
    PAL_Terminate();
    return PASS;
}

