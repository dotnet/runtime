// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for FreeEnvironmentStringsW() function
**
**
**=========================================================*/

#define UNICODE
#include <palsuite.h>

PALTEST(miscellaneous_FreeEnvironmentStringsW_test2_paltest_freeenvironmentstringsw_test2, "miscellaneous/FreeEnvironmentStringsW/test2/paltest_freeenvironmentstringsw_test2")
{

    WCHAR CapturedEnvironment[] = {'T','E','S','T','\0'};
    BOOL TheResult = 0;
    LPWSTR lpCapturedEnvironment = NULL;
    
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    lpCapturedEnvironment = (LPWSTR)malloc( sizeof(CapturedEnvironment) / 
                                            sizeof(CapturedEnvironment[0]) );

    if ( lpCapturedEnvironment )
    {
        memcpy( lpCapturedEnvironment, CapturedEnvironment, 
                sizeof(CapturedEnvironment) / sizeof(CapturedEnvironment[0]) );
    }
    else
    {
        Fail( "malloc()  failed to allocate memory.\n" );
    }
    /* Even if this is not a valid Environment block, the function will 
       still return success 
    */
  
    TheResult = FreeEnvironmentStrings( lpCapturedEnvironment );
    if(TheResult == 0) 
    {
        Fail("The function should still return a success value even if it is "
             "passed a LPWSTR which is not an environment block properly "
             "acquired from GetEnvironmentStrings\n");
    }
  
    /* Even passing this function NULL, should still return a success value */
    TheResult = FreeEnvironmentStrings(NULL);
    if(TheResult == 0) 
    {
        Fail("The function should still return a success value even if pass "
             "NULL.\n");    
    }
 
    
    PAL_Terminate();
    return PASS;
}



