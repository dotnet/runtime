// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for CloseHandle function, try to close an unopened HANDLE
**
**
**=========================================================*/

#include <palsuite.h>

PALTEST(miscellaneous_CloseHandle_test2_paltest_closehandle_test2, "miscellaneous/CloseHandle/test2/paltest_closehandle_test2")
{

    HANDLE SomeHandle = NULL;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
 
    /* If the handle is already closed and you can close it again, 
     * something is wrong. 
     */
  
    if(CloseHandle(SomeHandle) != 0) 
    {
        Fail("ERROR: Called CloseHandle on an already closed Handle "
             "and it still returned as a success.\n");
    }
  
    
    PAL_Terminate();
    return PASS;
}



