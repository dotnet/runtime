// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

PALTEST(miscellaneous_InterlockedDecrement64_test1_paltest_interlockeddecrement64_test1, "miscellaneous/InterlockedDecrement64/test1/paltest_interlockeddecrement64_test1")
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
#if defined(HOST_64BIT)
    /* Compare START_VALUE with BaseVariableToManipulate, they're equal, 
       so exchange 
    */
    InterlockedDecrement64(&TheValue);
    TheReturn = InterlockedDecrement64(&TheValue);

    /* Decremented twice, it should be -2 now */
    if(TheValue != -2) 
    {
        Fail("ERROR: After being decremented twice, the value should be -2, "
             "but it is really %ll.",TheValue);
    }
  
    /* Check to make sure it returns itself */
    if(TheReturn != TheValue) 
    {
        Fail("ERROR: The function should have returned the new value of %d "
             "but instead returned %ll.",TheValue,TheReturn);    
    }
#endif  //defined(HOST_64BIT)
    PAL_Terminate();
    return PASS; 
} 





