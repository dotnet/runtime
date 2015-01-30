//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source : test.c
**
** Purpose: Test for GetEnvironmentVariable() function
** Pass a small buffer to GetEnvironmentVariableA to ensure 
** it returns the size it requires.	
**
**
**=========================================================*/

#include <palsuite.h>

#define SMALL_BUFFER_SIZE 1

int __cdecl main(int argc, char *argv[]) {

    /* A place to stash the returned values */
    int  ReturnValueForSmallBuffer = 0;
    
    char pSmallBuffer[SMALL_BUFFER_SIZE];
 
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
  
    /* PATH won't fit in this buffer, it should return how many 
       characters it needs 
    */
  
    ReturnValueForSmallBuffer = GetEnvironmentVariable("PATH",       
                                                       pSmallBuffer,
                                                       SMALL_BUFFER_SIZE);  
    if(ReturnValueForSmallBuffer <= 0) 
    {
        Fail("The return value was %d when it should have been greater "
             "than 0. " 
             "This should return the number of characters needed to contained "
             "the contents of PATH in a buffer.\n",ReturnValueForSmallBuffer);
    }
  
    PAL_Terminate();
    return PASS;
}



