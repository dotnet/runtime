// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  pal_finally.c (test 3)
**
** Purpose: Tests the PAL implementation of the PAL_FINALLY in the
**          presence of a call stack. An exception is forced and
**          passed to two nested exception filters for consideration.
**          The first filter returns EXCEPTION_CONTINUE_SEARCH so the
**          second can run and return EXCEPTION_EXECUTE_HANDLER.  The
**          initial exception handler should be skipped, and the
**          second executed, and all the PAL_FINALLY blocks handled
**
**
**===================================================================*/
#include <palsuite.h>

BOOL bFilterCS  = FALSE;
BOOL bFilterEE  = FALSE;
BOOL bFinally1  = FALSE;
BOOL bFinally2  = FALSE;
BOOL bFinally3  = FALSE;
BOOL bFinally4  = FALSE;
BOOL bTry1      = FALSE;
BOOL bTry2      = FALSE;
BOOL bExcept1   = FALSE;
BOOL bExcept2   = FALSE;
BOOL bContinued = FALSE;
const int nValidator = 12321;

LONG ContSearchFilter(EXCEPTION_POINTERS* ep, LPVOID pnTestInt)
{

    /* let the main know we've hit the filter function */
    bFilterCS = TRUE;

    if (!bTry1 || !bTry2)
    {
        Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
            " The ContSearch filter was hit without PAL_TRY being hit.\n");
    }

    if (bFilterEE)
    {
        Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
             " The ContSearch filter was hit after the ExecuteException "
             "filter.\n");
    }

    return EXCEPTION_CONTINUE_SEARCH;
}

LONG ExecExceptionFilter(EXCEPTION_POINTERS* ep, LPVOID pnTestInt)
{
    /* let the main know we've hit the filter function */
    bFilterEE = TRUE;

    if (!bTry1 || !bTry2)
    {
        Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
            " The ContSearch filter was hit without PAL_TRY being hit.\n");
    }

    if (!bFilterCS)
    {
        Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
             " The ExecException filter was hit before the ContSearch "
             "filter.\n");
    }
    return EXCEPTION_EXECUTE_HANDLER;
}

void NestedFunc1 (void)
{
    int* p = 0x00000000;   /* pointer to NULL */

    PAL_TRY
    {
        PAL_TRY
        {

            if (bExcept1 || bExcept2)
            {
                Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                     " PAL_EXCEPT_FILTER was hit before PAL_TRY.\n");
            }
            bTry2 = TRUE; /* indicate we hit the inner PAL_TRY block */
            *p = 13;        /* causes an access violation exception */

            Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                 " We executed beyond the trapping code.\n");
        }
        PAL_FINALLY
        {
            if (!bTry1)
            {
                Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                    " The first finally handler was hit without the outer "
                    "PAL_TRY's code being hit.\n");
            }
            if (!bTry2)
            {
                Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                     " The first finally handler was hit without the inner "
                     "PAL_TRY's code being hit.\n");
            }
            if (!bFilterCS)
            {
                Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                     " The first finally handler was hit without the inner "
                     "filter being hit.\n");
            }
            if (!bFilterEE)
            {
                Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                     " The first finally handler handler was hit without the "
                     "outer filter being hit.\n");
            }
            bFinally1 = TRUE; /* indicate we hit the first finally block */
        }
        PAL_ENDTRY;
    }
    PAL_EXCEPT_FILTER(ContSearchFilter, (LPVOID)&nValidator)
    {
        Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
             " The dummy handler was  "
             "being hit.\n");
        bExcept2 = TRUE; /* indicate we hit the inner block */
    }
    PAL_ENDTRY;

}

void NestedFunc2 (void)
{
    PAL_TRY
    {
        NestedFunc1();
    }
    PAL_FINALLY
    {
        if (!bFinally1)
        {
            Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                 " The second finally handler handler was hit without the "
                 " top level one being hit.\n");
        }
        bFinally2 = TRUE;
    }
    PAL_ENDTRY ;
}

PALTEST(exception_handling_pal_finally_test1_paltest_pal_finally_test1, "exception_handling/pal_finally/test1/paltest_pal_finally_test1")
{
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /*
    ** test to make sure we get into the second exception block only based
    ** on the return codes of the filters
    */

    PAL_TRY
    {
        PAL_TRY
        {
            PAL_TRY
            {
                if (bExcept1 || bExcept2)
                {
                    Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going"
                        " on. PAL_EXCEPT_FILTER was hit before PAL_TRY.\n");
                }
                bTry1 = TRUE;    /* indicate we hit the outer PAL_TRY block */

                NestedFunc2();

                Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                    " We executed beyond the trapping code.\n");
            }
            PAL_FINALLY
            {
                if (!bFinally2)
                {
                    Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going"
                         " on. Finally handlers hit out of order (2->3)\n");
                }
                bFinally3 = TRUE;
            }
            PAL_ENDTRY;
        }
        PAL_EXCEPT_FILTER(ExecExceptionFilter, (LPVOID)&nValidator)
        {
            if (!bTry1)
            {
                Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                    " PAL_EXCEPT_FILTER's handler was hit without the outer "
                     "PAL_TRY's code being hit.\n");
            }
            if (!bTry2)
            {
                Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                    " PAL_EXCEPT_FILTER's handler was hit without the inner "
                     "PAL_TRY's code being hit.\n");
            }
            if (!bFilterCS)
            {
                Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                    " PAL_EXCEPT_FILTER's handler was hit without the inner "
                     "filter being hit.\n");
            }
            if (!bFilterEE)
            {
                Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                    " PAL_EXCEPT_FILTER's handler was hit without the outer "
                     "filter being hit.\n");
            }
            bExcept1 = TRUE; /* indicate we hit the outer block */
        }
        PAL_ENDTRY;

        if (bFinally4)
        {
            Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                 " Finally handler # 4 executed before it should be \n");
        }
    }
    PAL_FINALLY
    {
        if (!bExcept1 || bExcept2)
        {
            Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                 " Exceptions handlers hit in weird ways\n");
        }
        if (!bFinally3)
        {
            Fail("PAL_EXCEPT_FILTER: ERROR -> Something weird is going on."
                 " Finally handlers hit out of order 3->4)\n");
        }

        bFinally4 = TRUE;
    }
    PAL_ENDTRY;

    if (!bTry1)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the outer"
              " PAL_TRY block was not executed.\n");
    }

    if (!bTry2)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the inner"
              " PAL_TRY block was not executed.\n");
    }

    if (bExcept2)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the "
              "inner PAL_EXCEPT_FILTER block was executed.\n");
    }
    if (!bExcept1)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the "
              "outer PAL_EXCEPT_FILTER block was not executed.\n");
    }

    if (!bFilterCS)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the "
              "search continuing filter"
              " function was not executed.\n");
    }
    if (!bFilterEE)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the "
              "execute handler filter"
              " function was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if (!bFinally1 || !bFinally2 || !bFinally3 || !bFinally4)
    {
        Fail("");
    }
    if(!bTry1 || !bTry2 || !bExcept1 || bExcept2 || !bFilterEE || !bFilterCS )
    {
        Fail("");
    }


    PAL_Terminate();
    return PASS;

}

