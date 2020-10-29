// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  pal_except_filter.c (test 1)
**
** Purpose: Tests the PAL implementation of the PAL_EXCEPT_FILTER. An 
**          exception is forced and a known value is passed to the filter
**          fuction. The known value as well as booleans are tested to
**          ensure proper functioning.
**
**
**===================================================================*/



#include <palsuite.h>

BOOL bFilter = FALSE;
BOOL bTry = FALSE;
const int nValidator = 12321;

LONG ExitFilter(EXCEPTION_POINTERS* ep, LPVOID pnTestInt)
{
    int nTestInt = *(int *)pnTestInt;
    
    /* let the main know we've hit the filter function */
    bFilter = TRUE;

    if (!bTry)
    {
        Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
            " The filter was hit without PAL_TRY being hit.\n");
    }

    /* was the correct value passed? */
    if (nValidator != nTestInt)
    {
        Fail("PAL_EXCEPT_FILTER: ERROR -> Parameter passed to filter function"
            " should have been \"%d\" but was \"%d\".\n",
            nValidator,
            nTestInt);
    }
    return EXCEPTION_EXECUTE_HANDLER;
}


PALTEST(exception_handling_PAL_EXCEPT_FILTER_test1_paltest_pal_except_filter_test1, "exception_handling/PAL_EXCEPT_FILTER/test1/paltest_pal_except_filter_test1")
{
    int* p = 0x00000000;   /* pointer to NULL */
    BOOL bExcept = FALSE;

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /*
    ** test to make sure we get into the exception block
    */
    
    PAL_TRY 
    {
        if (bExcept)
        {
            Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                " PAL_EXCEPT_FILTER was hit before PAL_TRY.\n");
        }
        bTry = TRUE;    /* indicate we hit the PAL_TRY block */
        *p = 13;        /* causes an access violation exception */

        Fail("PAL_EXCEPT_FILTER: ERROR -> code was executed after the "
            "access violation.\n");
    }
    PAL_EXCEPT_FILTER(ExitFilter, (LPVOID)&nValidator)
    {
        if (!bTry)
        {
            Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                " PAL_EXCEPT_FILTER was hit without PAL_TRY being hit.\n");
        }
        bExcept = TRUE; /* indicate we hit the PAL_EXCEPT_FILTER block */
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the PAL_TRY"
            " block was not executed.\n");
    }

    if (!bExcept)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the "
            "PAL_EXCEPT_FILTER block was not executed.\n");
    }

    if (!bFilter)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the filter"
            " function was not executed.\n");
    }


    /* did we hit all the code blocks? */
    if(!bTry || !bExcept || !bFilter)
    {
        Fail("");
    }


    PAL_Terminate();  
    return PASS;

}
