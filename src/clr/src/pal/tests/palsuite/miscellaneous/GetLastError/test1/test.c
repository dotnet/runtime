//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source : test.c
**
** Purpose: Test for GetLastError() function
**
**
**=========================================================*/

/* Depends on SetLastError() */

#include <palsuite.h>

/**
 * Helper functions that does the actual test
 */
static void test(DWORD error )
{
    DWORD  TheResult;

    /* Set error */
    SetLastError(error);

    /* Check to make sure it returns the error value we just set */
    TheResult = GetLastError();
    if(TheResult!= error) 
    {
        Fail("ERROR: The last error should have been %u, but when " 
            "GetLastError was called, it returned %u.\n",error,TheResult);
    }

}

int __cdecl main(int argc, char *argv[]) {
  

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
  
    /* test setting and getting some  values */
    test(5);
    test(0xffffffff);
    test(0xEEEEEEEE);
    test(0xAAAAAAAA);       
    
    PAL_Terminate();
    return PASS;
}





