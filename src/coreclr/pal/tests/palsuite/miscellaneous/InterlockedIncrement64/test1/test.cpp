// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

PALTEST(miscellaneous_InterlockedIncrement64_test1_paltest_interlockedincrement64_test1, "miscellaneous/InterlockedIncrement64/test1/paltest_interlockedincrement64_test1")
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

    InterlockedIncrement64(&TheValue);
    TheReturn = InterlockedIncrement64(&TheValue);

    /* Incremented twice, it should be 2 now */
    if(TheValue != 2)
    {
        Fail("ERROR: The value was incremented twice and should now be 2, "
             "but it is really %ll",TheValue);
    }

    /* Check to make sure it returns itself */
    if(TheReturn != TheValue)
    {
        Fail("ERROR: The function should return the new value, which should "
             "have been %d, but it returned %ll.",TheValue,TheReturn);
    }

#endif  //defined(HOST_64BIT)
    PAL_Terminate();
    return PASS;
}





