// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for lstrlenW() function
**
**
**=========================================================*/

#define UNICODE

#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) {
  
    WCHAR FirstString[] = {'T','E','S','T','\0'}; /* 4 characters */
  
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* The string size should be 4, as noted just above */
    if(lstrlen(FirstString) != 4) 
    {
        Fail("ERROR:  The return value was %d when it should have shown the "
             "size to be 4 characters.\n",lstrlen(FirstString));    
    }

    /* A NULL pointer should return 0 length */
    if(lstrlen(NULL) != 0) 
    {
        Fail("ERROR: The return value was %d when it should have been 0, the "
             "length of a NULL pointer.\n",lstrlen(NULL));
    }
  
    
    PAL_Terminate();
    return PASS;
}



