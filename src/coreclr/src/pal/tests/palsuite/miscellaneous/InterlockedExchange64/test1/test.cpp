// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: InterlockedExchange64() function
**
**
**=========================================================*/

/* This test is FINISHED.  Note:  The biggest feature of this function is that
   it locks the value before it increments it -- in order to make it so only 
   one thread can access it.  But, I really don't have a great test to make 
   sure it's thread safe.  Any ideas?
*/

#include <palsuite.h>

#define START_VALUE 0

int __cdecl main(int argc, char *argv[]) {

    LONGLONG TheValue = START_VALUE;
    LONGLONG NewValue = 5;
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
#if defined(BIT64)

    TheReturn = InterlockedExchange64(&TheValue,NewValue);
  
    /* Compare the exchanged value with the value we exchanged it with.  Should
       be the same.
    */  
    if(TheValue != NewValue) 
    {
        Fail("ERROR: The value which was exchanged should now be %ll, but "
             "instead it is %ll.",NewValue,TheValue);    
    }
  
    /* Check to make sure it returns the origional number which 'TheValue' was 
       set to. 
    */
  
    if(TheReturn != START_VALUE) 
    {
        Fail("ERROR: The value returned should be the value before the "
             "exchange happened, which was %ll, but %ll was returned.",
             START_VALUE,TheReturn);
    }

#endif  // BIT64
    PAL_Terminate();
    return PASS; 
} 





