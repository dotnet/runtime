//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source : test.c
**
** Purpose: InterlockedDecrement64() function
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
    LONGLONG TheValue = 0;
    LONGLONG TheReturn;
  
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

/*
**  Run only on 64 bit platforms
*/
#if defined(BIT64) && defined(PLATFORM_UNIX)
    /* Compare START_VALUE with BaseVariableToManipulate, they're equal, 
       so exchange 
    */
    InterlockedDecrement64(&TheValue);
    TheReturn = InterlockedDecrement64(&TheValue);

    /* Decremented twice, it should be -2 now */
    if(TheValue != -2) 
    {
#ifdef PLATFORM_UNIX
        Fail("ERROR: After being decremented twice, the value should be -2, "
             "but it is really %ll.",TheValue);
#else
        Fail("ERROR: After being decremented twice, the value should be -2, "
             "but it is really %I64.",TheValue);
#endif
    }
  
    /* Check to make sure it returns itself */
    if(TheReturn != TheValue) 
    {
#ifdef PLATFORM_UNIX
        Fail("ERROR: The function should have returned the new value of %d "
             "but instead returned %ll.",TheValue,TheReturn);    
#else
        Fail("ERROR: After being decremented twice, the value should be -2, "
             "but it is really %I64.",TheValue);
#endif
    }
#endif  //defined(BIT64) && defined(PLATFORM_UNIX)
    PAL_Terminate();
    return PASS; 
} 





