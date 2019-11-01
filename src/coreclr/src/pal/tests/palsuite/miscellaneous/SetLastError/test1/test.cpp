// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for SetLastError() function
**
**
**=========================================================*/
/* Depends on GetLastError() */


#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) {

    /* Error value that we can set to test */
    const unsigned int FAKE_ERROR = 5;
    const int NEGATIVE_ERROR = -1;
  
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Set error */
    SetLastError(FAKE_ERROR);
  
    /* Check to make sure it returns the error value we just set */
    if(GetLastError() != FAKE_ERROR) 
    {
        Fail("ERROR: The last error should have been '%d' but the error "
             "returned was '%d'\n",FAKE_ERROR,GetLastError());
    }
  
    /* Set the error to a negative */
    SetLastError(NEGATIVE_ERROR);
  
    /* Check to make sure it returns the error value we just set */
    if((signed)GetLastError() != NEGATIVE_ERROR) 
    {
        Fail("ERROR: The last error should have been '%d' but the error "
             "returned was '%d'\n",NEGATIVE_ERROR,GetLastError());
    }
  
    
    PAL_Terminate();
    return PASS;
}



