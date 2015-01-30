//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test.c
**
** Purpose: Test for lstrlenA() function
**
**
**=========================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) {

    char * FirstString = "Pal Testing"; /* 11 characters */

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
  
    /* The string size should be 11 */
    if(lstrlen(FirstString) != 11) 
    {
        Fail("ERROR: The string size returned was %d but it should have "
             "been 11 in this test.\n",lstrlen(FirstString));    
    }

    /* A NULL pointer should return 0 length */
    if(lstrlen(NULL) != 0) 
    {
        Fail("ERROR: Checking the length of NULL pointer should return "
             "a value of 0, but %d was returned.\n",lstrlen(NULL));
    }

    
    PAL_Terminate();
    return PASS;
}



