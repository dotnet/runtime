// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: test.c (exception_handling\raiseexception\test3)
**
** Purpose: Tests that the correct ExceptionCode is passed 
**          to the filter by RaiseException 
**
**
**============================================================*/


#include <palsuite.h>

BOOL bFilter_RaiseException_test3 = FALSE;
BOOL bTry_RaiseException_test3    = FALSE;
BOOL bExcept_RaiseException_test3 = FALSE;

/**
**
**  Filter function that checks for the parameters
**
**/
LONG Filter_test1_RaiseException_test3(EXCEPTION_POINTERS* ep, VOID* unused)
{
    /* let the main know we've hit the filter function */
    bFilter_RaiseException_test3 = TRUE;

    if (!bTry_RaiseException_test3)
    {
        Fail("PAL_EXCEPT_FILTER_EX: ERROR -> Something weird is going on."
            " The filter was hit without PAL_TRY being hit.\n");
    }

    
    /* was the correct exception code passed? */
    if (ep->ExceptionRecord->ExceptionCode != EXCEPTION_ARRAY_BOUNDS_EXCEEDED)
    {
        Fail("RaiseException: ERROR -> ep->ExceptionRecord->ExceptionCode"
            " was %x when it was expected to be %x\n",
            ep->ExceptionRecord->ExceptionCode,
            EXCEPTION_ARRAY_BOUNDS_EXCEEDED);

    }

    return EXCEPTION_EXECUTE_HANDLER;
}

PALTEST(exception_handling_RaiseException_test3_paltest_raiseexception_test3, "exception_handling/RaiseException/test3/paltest_raiseexception_test3")
{
    bExcept_RaiseException_test3 = FALSE;
    
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /********************************************************
     * Test that the correct arguments are passed 
     * to the filter by RaiseException 
     */
    PAL_TRY(VOID*, unused, NULL)
    {
        bTry_RaiseException_test3 = TRUE;    /* indicate we hit the PAL_TRY block */

        RaiseException(EXCEPTION_ARRAY_BOUNDS_EXCEEDED,
		       0,
		       0,NULL);

        Fail("RaiseException: ERROR -> code was executed after the "
             "exception was raised.\n");
    }
    PAL_EXCEPT_FILTER(Filter_test1_RaiseException_test3)
    {
        if (!bTry_RaiseException_test3)
        {
            Fail("RaiseException: ERROR -> Something weird is going on."
                 " PAL_EXCEPT_FILTER was hit without PAL_TRY being hit.\n");
        }
        bExcept_RaiseException_test3 = TRUE; /* indicate we hit the PAL_EXCEPT_FILTER_EX block */
    }
    PAL_ENDTRY;

    if (!bTry_RaiseException_test3)
    {
        Trace("RaiseException: ERROR -> It appears the code in the "
              "PAL_TRY block was not executed.\n");
    }

    if (!bExcept_RaiseException_test3)
    {
        Trace("RaiseException: ERROR -> It appears the code in the "
              "PAL_EXCEPT_FILTER_EX block was not executed.\n");
    }

    if (!bFilter_RaiseException_test3)
    {
        Trace("RaiseException: ERROR -> It appears the code in the"
              " filter function was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry_RaiseException_test3 || !bExcept_RaiseException_test3 || !bFilter_RaiseException_test3)
    {
        Fail("");
    }


    PAL_Terminate();  
    return PASS;

}
