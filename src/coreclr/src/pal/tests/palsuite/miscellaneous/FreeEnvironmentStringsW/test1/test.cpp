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

PALTEST(miscellaneous_FreeEnvironmentStringsW_test1_paltest_freeenvironmentstringsw_test1, "miscellaneous/FreeEnvironmentStringsW/test1/paltest_freeenvironmentstringsw_test1")
{

    LPWSTR CapturedEnvironment = NULL;
    BOOL TheResult = 0;
  
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
 
    CapturedEnvironment = GetEnvironmentStrings();

    /* If it's pointing to NULL, it failed. This checks the dependency */
    if(CapturedEnvironment == NULL) {
        Fail("The function GetEnvironmentStrings() failed, and the "
             "FreeEnvironmentStrings() tests is dependant on it.\n");    
    }

    /* This should return 1, if it succeeds, otherwise, test fails */
    TheResult = FreeEnvironmentStrings(CapturedEnvironment);
    if(TheResult != 1) {
        Fail("The function returned %d which indicates failure to Free the "
             "Environment Strings.\n",TheResult);
    }
  
    
    PAL_Terminate();
    return PASS;
}



