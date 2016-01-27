// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

BOOL bFilter = FALSE;
BOOL bTry    = FALSE;
BOOL bExcept = FALSE;

/**
**
**  Filter function that checks for the parameters
**
**/
LONG Filter_test1(EXCEPTION_POINTERS* ep, VOID* unused)
{
    /* let the main know we've hit the filter function */
    bFilter = TRUE;

    if (!bTry)
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

int __cdecl main(int argc, char *argv[])
{
    bExcept = FALSE;
    
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
        bTry = TRUE;    /* indicate we hit the PAL_TRY block */

        RaiseException(EXCEPTION_ARRAY_BOUNDS_EXCEEDED,
		       0,
		       0,NULL);

        Fail("RaiseException: ERROR -> code was executed after the "
             "exception was raised.\n");
    }
    PAL_EXCEPT_FILTER(Filter_test1)
    {
        if (!bTry)
        {
            Fail("RaiseException: ERROR -> Something weird is going on."
                 " PAL_EXCEPT_FILTER was hit without PAL_TRY being hit.\n");
        }
        bExcept = TRUE; /* indicate we hit the PAL_EXCEPT_FILTER_EX block */
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("RaiseException: ERROR -> It appears the code in the "
              "PAL_TRY block was not executed.\n");
    }

    if (!bExcept)
    {
        Trace("RaiseException: ERROR -> It appears the code in the "
              "PAL_EXCEPT_FILTER_EX block was not executed.\n");
    }

    if (!bFilter)
    {
        Trace("RaiseException: ERROR -> It appears the code in the"
              " filter function was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry || !bExcept || !bFilter)
    {
        Fail("");
    }


    PAL_Terminate();  
    return PASS;

}
