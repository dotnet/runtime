//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source : test.c
**
** Purpose: InterlockedDecrement() function
**
**
**=========================================================*/

/* This test is FINISHED.  Note:  The biggest feature of this function is that
   it locks the value before it increments it -- in order to make it so only 
   one thread can access it.  But, I really don't have a great test to make 
   sure it's thread safe. Any ideas?
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

    InterlockedDecrement(&TheValue);
    TheReturn = InterlockedDecrement(&TheValue);

    /* Decremented twice, it should be -2 now */
    if(TheValue != -2) 
    {
        Fail("ERROR: After being decremented twice, the value should be -2, "
             "but it is really %d.",TheValue);
    }
  
    /* Check to make sure it returns itself */
    if(TheReturn != TheValue) 
    {
        Fail("ERROR: The function should have returned the new value of %d "
             "but instead returned %d.",TheValue,TheReturn);    
    }
    
    PAL_Terminate();
    return PASS; 
} 





