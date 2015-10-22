//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test.c
**
** Purpose: InterlockedIncrement() function
**
**
**=========================================================*/

/* This test is FINISHED.  Note:  The biggest feature of this function is that
   it locks the value before it increments it -- in order to make it so only 
   one thread can access it.  But, I really don't have a great test to make 
   sure it's thread safe. Any ideas?  Nothing I've tried has worked.
*/


#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) 
{

    int TheValue = 0;
    int TheReturn;
  
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    InterlockedIncrement(&TheValue);
    TheReturn = InterlockedIncrement(&TheValue);
  
    /* Incremented twice, it should be 2 now */
    if(TheValue != 2) 
    {
        Fail("ERROR: The value was incremented twice and shoud now be 2, "
             "but it is really %d",TheValue); 
    }
  
    /* Check to make sure it returns itself */
    if(TheReturn != TheValue) 
    {
        Fail("ERROR: The function should return the new value, which shoud "
             "have been %d, but it returned %d.",TheValue,TheReturn);          
    }
    
    PAL_Terminate();
    return PASS; 
} 





