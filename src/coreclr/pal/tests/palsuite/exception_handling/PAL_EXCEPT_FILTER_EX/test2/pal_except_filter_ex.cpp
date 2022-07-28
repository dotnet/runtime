// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  PAL_EXCEPT_FILTER_EX.c (test 2)
**
** Purpose: Tests the PAL implementation of the PAL_EXCEPT_FILTER_EX.
**          There is a nested try blocks in this test. The nested
**          PAL_TRY creates an exception and the FILTER creates another.
**          This test makes sure that this case does not end in a
**          infinite loop.
**
**
**===================================================================*/

#include <palsuite.h>

BOOL bFilter = FALSE;
BOOL bTry = FALSE;
BOOL bTry2 = FALSE;
const int nValidator = 12321;

/* Filter function for the first try block.
 */
LONG Filter_01(EXCEPTION_POINTERS* ep, VOID *pnTestInt)
{
    int nTestInt = *(int *)pnTestInt;

    /* Signal main() that filter has been executed. */
    bFilter = TRUE;

    if (!bTry)
    {
        Fail("ERROR: The filter was executed without "
             "entering the first PAL_TRY.\n");
    }

    if (!bTry2)
    {
        Fail("ERROR: The filter was executed without "
             "entering the second PAL_TRY.\n");
    }

    /* Was the correct value passed? */
    if (nValidator != nTestInt)
    {
        Fail("ERROR: Parameter passed to filter function "
             "should have been \"%d\" but was \"%d\".\n",
             nValidator,
             nTestInt);
    }

    return EXCEPTION_EXECUTE_HANDLER;
}

PALTEST(exception_handling_PAL_EXCEPT_FILTER_EX_test2_paltest_pal_except_filter_ex_test2, "exception_handling/PAL_EXCEPT_FILTER_EX/test2/paltest_pal_except_filter_ex_test2")
{
    int* p = 0x00000000;
    BOOL bExcept  = FALSE;
    BOOL bExcept2 = FALSE;

    /* Initialize the PAL.
     */
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Test a nested PAL_Try block.
     */
    PAL_TRY
    {
        /* Signal entry into first PAL_TRY block.*/
        bTry = TRUE;

        PAL_TRY
        {
            /* Signal entry into second PAL_TRY block.*/
            bTry2 = TRUE;
            /* Cause an exception.*/
            *p = 13;
        }
        PAL_EXCEPT_FILTER(Filter_01, (LPVOID)&nValidator)
        {
            /* Signal entry into second PAL_EXCEPT filter.*/
            bExcept = TRUE;
            /* Cause another exception.*/
            *p = 13;
        }
        PAL_ENDTRY

    }
    PAL_EXCEPT_FILTER(Filter_01, (LPVOID)&nValidator)
    {
        /* Signal entry into second PAL_EXCEPT filter.*/
        bExcept2 = TRUE;
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("ERROR: The code in the first "
              "PAL_TRY block was not executed.\n");
    }

    if (!bTry2)
    {
        Trace("ERROR: The code in the nested "
              "PAL_TRY block was not executed.\n");
    }

    if (!bExcept)
    {
        Trace("ERROR: The code in the first "
              "PAL_EXCEPT_FILTER_EX block was not executed.\n");
    }

    if (!bExcept2)
    {
        Trace("ERROR: The code in the second "
              "PAL_EXCEPT_FILTER_EX block was not executed.\n");
    }

    if (!bFilter)
    {
        Trace("ERROR: The code in the first "
              "filter function was not executed.\n");
    }

    if(!bTry || !bTry2 || !bExcept || !bExcept2 || !bFilter )
    {
        Fail("");
    }

    /* Terminate the PAL.
     */
    PAL_Terminate();
    return PASS;

}
