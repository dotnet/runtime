// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  PAL_EXCEPT_FILTER_EX.c (test 1)
**
** Purpose: Tests the PAL implementation of the PAL_EXCEPT_FILTER_EX. 
**          There are two try blocks in this test. The first forces an
**          exception error to force hitting the first filter. The second
**          doesn't to make sure we don't hit the filter. A value is also
**          passed into the filter program and it is validated to make sure
**          it was passed correctly.
**
**
**===================================================================*/

#include <palsuite.h>

BOOL bFilter = FALSE;
BOOL bTry = FALSE;
const int nValidator = 12321;


/**
**
**  Filter function for the first try block
**
**/

LONG Filter_01(EXCEPTION_POINTERS* ep, VOID *pnTestInt)
{
    int nTestInt = *(int *)pnTestInt;
    
    /* let the main know we've hit the filter function */
    bFilter = TRUE;

    if (!bTry)
    {
        Fail("PAL_EXCEPT_FILTER_EX: ERROR -> Something weird is going on."
            " The filter was hit without PAL_TRY being hit.\n");
    }

    /* was the correct value passed? */
    if (nValidator != nTestInt)
    {
        Fail("PAL_EXCEPT_FILTER_EX: ERROR -> Parameter passed to filter"
            " function should have been \"%d\" but was \"%d\".\n",
            nValidator,
            nTestInt);
    }

    return EXCEPTION_EXECUTE_HANDLER;
}


/**
**
**  Filter function for the second try block. We shouldn't
**  hit this function.
**
**/

LONG Filter_02(EXCEPTION_POINTERS* ep, VOID *pnTestInt)
{
    /* let the main know we've hit the filter function */
    bFilter = TRUE;

    return EXCEPTION_EXECUTE_HANDLER;
}


PALTEST(exception_handling_PAL_EXCEPT_FILTER_EX_test1_paltest_pal_except_filter_ex_test1, "exception_handling/PAL_EXCEPT_FILTER_EX/test1/paltest_pal_except_filter_ex_test1")
{
    int* p = 0x00000000;
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
            Fail("PAL_EXCEPT_FILTER_EX: ERROR -> Something weird is going on."
                " The first PAL_EXCEPT_FILTER_EX was hit before PAL_TRY.\n");
        }

        bTry = TRUE;    /* indicate we hit the PAL_TRY block */
        *p = 13;        /* causes an access violation exception */

        Fail("PAL_EXCEPT_FILTER_EX: ERROR -> code was executed after the "
            "access violation.\n");
    }
    PAL_EXCEPT_FILTER(Filter_01, (LPVOID)&nValidator)
    {
        if (!bTry)
        {
            Fail("PAL_EXCEPT_FILTER_EX: ERROR -> Something weird is going on."
                " PAL_EXCEPT_FILTER_EX was hit without PAL_TRY being hit.\n");
        }
        bExcept = TRUE; /* indicate we hit the PAL_EXCEPT_FILTER_EX block */
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("PAL_EXCEPT_FILTER_EX: ERROR -> It appears the code in the "
            "first PAL_TRY block was not executed.\n");
    }

    if (!bExcept)
    {
        Trace("PAL_EXCEPT_FILTER_EX: ERROR -> It appears the code in the "
            "first PAL_EXCEPT_FILTER_EX block was not executed.\n");
    }

    if (!bFilter)
    {
        Trace("PAL_EXCEPT_FILTER_EX: ERROR -> It appears the code in the first"
            " filter function was not executed.\n");
    }


    /* did we hit all the code blocks? */
    if(!bTry || !bExcept || !bFilter)
    {
        Fail("");
    }

    bTry = bExcept = bFilter = FALSE;


    /*
    ** test to make sure we skip the exception block
    */

    PAL_TRY 
    {
        if (bExcept)
        {
            Fail("PAL_EXCEPT_FILTER_EX: ERROR -> Something weird is going on."
                " Second PAL_EXCEPT_FILTER_EX was hit before PAL_TRY.\n");
        }
        bTry = TRUE;    /* indicate we hit the PAL_TRY block */
    }
    PAL_EXCEPT_FILTER(Filter_02, (LPVOID)&nValidator)
    {
        if (!bTry)
        {
            Fail("PAL_EXCEPT_FILTER_EX: ERROR -> Something weird is going on."
                " PAL_EXCEPT_FILTER_EX was hit without PAL_TRY being hit.\n");
        }
        bExcept = TRUE; /* indicate we hit the PAL_EXCEPT_FILTER_EX block */
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("PAL_EXCEPT_FILTER_EX: ERROR -> It appears the code in the "
            "second PAL_TRY block was not executed.\n");
    }

    if (bExcept)
    {
        Trace("PAL_EXCEPT_FILTER_EX: ERROR -> It appears the code in the "
            "second PAL_EXCEPT_FILTER_EX block was executed even though an"
            " exception was not triggered.\n");
    }

    if (bFilter)
    {
        Trace("PAL_EXCEPT_FILTER_EX: ERROR -> It appears the code in the second"
            " filter function was executed even though an exception was"
            " not triggered.\n");
    }


    /* did we hit all the correct code blocks? */
    if(!bTry || bExcept || bFilter)
    {
        Fail("");
    }


    PAL_Terminate();  
    return PASS;

}
