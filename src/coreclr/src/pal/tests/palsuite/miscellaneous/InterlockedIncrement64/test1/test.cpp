// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: InterlockedIncrement64() function
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

    InterlockedIncrement64(&TheValue);
    TheReturn = InterlockedIncrement64(&TheValue);
  
    /* Incremented twice, it should be 2 now */
    if(TheValue != 2) 
    {
#ifdef PLATFORM_UNIX
        Fail("ERROR: The value was incremented twice and shoud now be 2, "
             "but it is really %ll",TheValue); 
#else
        Fail("ERROR: The value was incremented twice and shoud now be 2, "
             "but it is really %I64",TheValue); 
#endif
    }
  
    /* Check to make sure it returns itself */
    if(TheReturn != TheValue) 
    {
#ifdef PLATFORM_UNIX
        Fail("ERROR: The function should return the new value, which shoud "
             "have been %d, but it returned %ll.",TheValue,TheReturn);          
#else
        Fail("ERROR: The function should return the new value, which shoud "
             "have been %d, but it returned %I64.",TheValue,TheReturn);          
#endif
    }

#endif  //defined(BIT64) && defined(PLATFORM_UNIX)
    PAL_Terminate();
    return PASS; 
} 





