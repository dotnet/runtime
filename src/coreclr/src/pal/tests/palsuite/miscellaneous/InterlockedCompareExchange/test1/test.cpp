// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source : test.c
**
** Purpose: Test for InterlockedCompareExchange() function
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
#define SECOND_VALUE 5
#define THIRD_VALUE 10

PALTEST(miscellaneous_InterlockedCompareExchange_test1_paltest_interlockedcompareexchange_test1, "miscellaneous/InterlockedCompareExchange/test1/paltest_interlockedcompareexchange_test1")
{
  
    int BaseVariableToManipulate = START_VALUE;
    int ValueToExchange = SECOND_VALUE;
    int TempValue;
    int TheReturn;
  
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Compare START_VALUE with BaseVariableToManipulate, they're equal, 
       so exchange 
    */
  
    TheReturn = InterlockedCompareExchange(
        &BaseVariableToManipulate, /* Destination */
        ValueToExchange,           /* Exchange value */
        START_VALUE);              /* Compare value */
  
    /* Exchanged, these should be equal now */
    if(BaseVariableToManipulate != ValueToExchange) 
    {
        Fail("ERROR: A successful compare and exchange should have occurred, "
             "making the variable have the value of %d, as opposed to the "
             "current value of %d.",
             ValueToExchange,BaseVariableToManipulate);  
    }
  
    /* Check to make sure it returns the original number which 
       'BaseVariableToManipulate' was set to.  
    */
    if(TheReturn != START_VALUE) 
    {
        Fail("ERROR: The return value after the first exchange should be the "
             "former value of the variable, which was %d, but it is now %d.",
             START_VALUE,TheReturn);
    }


  
    ValueToExchange = THIRD_VALUE;         /* Give this a new value */
    TempValue = BaseVariableToManipulate;  /* Note value of Base */
  
    /* 
       Do an exchange where 'BaseVariableToManipulate' doesn't 
       match -- therefore the exchange shouldn't happen.  
       So, it should end up the same as the 'TempValue' we saved.
    */ 
  
    InterlockedCompareExchange(&BaseVariableToManipulate,
                               ValueToExchange,
                               START_VALUE);
  
    if(BaseVariableToManipulate != TempValue) 
    {
        Fail("ERROR:  An attempted exchange should have failed due to "
             "the compare failing.  But, it seems to have succeeded.  The "
             "value should be %d but is %d in this case.",
             TempValue,BaseVariableToManipulate);  
    }
    
    PAL_Terminate();
    return PASS; 
} 





