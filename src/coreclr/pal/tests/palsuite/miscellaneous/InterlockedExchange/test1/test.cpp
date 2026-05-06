// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test.c
**
** Purpose: InterlockedExchange() function
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

PALTEST(miscellaneous_InterlockedExchange_test1_paltest_interlockedexchange_test1, "miscellaneous/InterlockedExchange/test1/paltest_interlockedexchange_test1")
{

    int TheValue = START_VALUE;
    int NewValue = 5;
    int TheReturn;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    TheReturn = InterlockedExchange(&TheValue,NewValue);

    /* Compare the exchanged value with the value we exchanged it with.  Should
       be the same.
    */
    if(TheValue != NewValue)
    {
        Fail("ERROR: The value which was exchanged should now be %d, but "
             "instead it is %d.",NewValue,TheValue);
    }

    /* Check to make sure it returns the original number which 'TheValue' was
       set to.
    */

    if(TheReturn != START_VALUE)
    {
        Fail("ERROR: The value returned should be the value before the "
             "exchange happened, which was %d, but %d was returned.",
             START_VALUE,TheReturn);
    }


    PAL_Terminate();
    return PASS;
}





