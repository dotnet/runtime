//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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



