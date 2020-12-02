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

PALTEST(miscellaneous_InterlockedCompareExchange64_test1_paltest_interlockedcompareexchange64_test1, "miscellaneous/InterlockedCompareExchange64/test1/paltest_interlockedcompareexchange64_test1")
{
  
    LONGLONG BaseVariableToManipulate = START_VALUE;
    LONGLONG ValueToExchange = SECOND_VALUE;
    LONGLONG TempValue;
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
  
    TheReturn = InterlockedCompareExchange64(
        &BaseVariableToManipulate, /* Destination */
        ValueToExchange,           /* Exchange value */
        START_VALUE);              /* Compare value */
  
    /* Exchanged, these should be equal now */
    if(BaseVariableToManipulate != ValueToExchange) 
    {
        Fail("ERROR: A successful compare and exchange should have occurred, "
             "making the variable have the value of %ll, as opposed to the "
             "current value of %ll.",
             ValueToExchange,BaseVariableToManipulate);  
    }
  
    /* Check to make sure it returns the original number which 
       'BaseVariableToManipulate' was set to.  
    */
    if(TheReturn != START_VALUE) 
    {
        Fail("ERROR: The return value after the first exchange should be the "
             "former value of the variable, which was %ll, but it is now %ll.",
             START_VALUE,TheReturn);

    }


  
    ValueToExchange = THIRD_VALUE;         /* Give this a new value */
    TempValue = BaseVariableToManipulate;  /* Note value of Base */
  
    /* 
       Do an exchange where 'BaseVariableToManipulate' doesn't 
       match -- therefore the exchange shouldn't happen.  
       So, it should end up the same as the 'TempValue' we saved.
    */ 
  
    InterlockedCompareExchange64(&BaseVariableToManipulate,
                               ValueToExchange,
                               START_VALUE);
  
    if(BaseVariableToManipulate != TempValue) 
    {
        Fail("ERROR:  An attempted exchange should have failed due to "
             "the compare failing.  But, it seems to have succeeded.  The "
             "value should be %ll but is %ll in this case.",
             TempValue,BaseVariableToManipulate);  
    }

#endif  //if defined(HOST_64BIT)
    PAL_Terminate();
    return PASS; 
}
