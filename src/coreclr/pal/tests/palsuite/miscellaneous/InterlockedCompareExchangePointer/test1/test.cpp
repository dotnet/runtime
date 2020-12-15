// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for InterlockedCompareExchangePointer() function
**
**
**=========================================================*/

/* This test is FINISHED.  Note:  The biggest feature of this function is that
   it locks the value before it increments it -- in order to make it so only
   one thread can access it.  But, I really don't have a great test to 
   make sure it's thread safe.  Any ideas?
*/

#include <palsuite.h>

PALTEST(miscellaneous_InterlockedCompareExchangePointer_test1_paltest_interlockedcompareexchangepointer_test1, "miscellaneous/InterlockedCompareExchangePointer/test1/paltest_interlockedcompareexchangepointer_test1")
{

    long StartValue = 5;
    long NewValue   = 10;
    PVOID ReturnValue = NULL;
  
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    ReturnValue = InterlockedCompareExchangePointer((PVOID)&StartValue,
                                                    (PVOID)NewValue,
                                                    (PVOID)StartValue);

    /* StartValue and NewValue should be equal now */
    if(StartValue != NewValue) 
    {
        Fail("ERROR: These values should be equal after the exchange.  "
             "They should both be %d, however the value that should have "
             "been exchanged is %d.\n",NewValue,StartValue);    
    }
  
    /* Returnvalue should have been set to what 'StartValue' was 
       (5 in this case) 
    */
  
    if((int)(size_t)ReturnValue != 5) 
    {
        Fail("ERROR: The return value should be the value of the "
             "variable before the exchange took place, which was 5.  "
             "But, the return value was %d.\n",ReturnValue);
    }

    /* This is a mismatch, so no exchange should happen */
    InterlockedCompareExchangePointer((PVOID)&StartValue,
                                       ReturnValue,
                                       ReturnValue);
    if(StartValue != NewValue) 
    {
        Fail("ERROR:  The compare should have failed and no exchange should "
             "have been made, but it seems the exchange still happened.\n");
    }
  
    
    PAL_Terminate();
    return PASS; 
} 






