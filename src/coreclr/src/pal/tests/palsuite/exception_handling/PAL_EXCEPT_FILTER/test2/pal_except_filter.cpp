// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  pal_except_filter.c (test 2)
**
** Purpose: Tests the PAL implementation of the PAL_EXCEPT_FILTER. An 
**          exception is forced and the filter returns
**          EXCEPTION_CONTINUE_EXECUTION to allow execution to continue.
**
**
**===================================================================*/


#include <palsuite.h>

char* p;   /* pointer to be abused */

BOOL bFilter = FALSE;
BOOL bTry = FALSE;
BOOL bTry2 = FALSE;
BOOL bContinued = FALSE;
const int nValidator = 12321;

LONG ExitFilter(EXCEPTION_POINTERS* ep, LPVOID pnTestInt)
{
    int nTestInt = *(int *)pnTestInt;
    void *Temp;
    
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

    /* Are we dealing with the exception we expected? */
    if (EXCEPTION_ACCESS_VIOLATION != ep->ExceptionRecord->ExceptionCode)
    {
        Fail("PAL_EXCEPT_FILTER: ERROR -> Unexpected Exception"
            " should have been \"%x\" but was \"%x\".\n",
            EXCEPTION_ACCESS_VIOLATION,
            ep->ExceptionRecord->ExceptionCode);
    }

    /* attempt to correct the problem by commiting the page at address 'p'  */
    Temp= VirtualAlloc(p, 1, MEM_COMMIT, PAGE_READWRITE);	
    if (!Temp) 
    { 
        Fail("EXCEPTION_CONTINUE_EXECUTION: last error = %u - probably "
             "out of memory. Unable to continue, not proof of exception "
             "failure\n",
             GetLastError());
    }
    /*  The memory that 'p' points to is now valid */

    return EXCEPTION_CONTINUE_EXECUTION;
}


PALTEST(exception_handling_PAL_EXCEPT_FILTER_test2_paltest_pal_except_filter_test2, "exception_handling/PAL_EXCEPT_FILTER/test2/paltest_pal_except_filter_test2")
{
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

        /* reserve an address chunk for p to point to */
        p = (char*) VirtualAlloc(0, 1, MEM_RESERVE, PAGE_READONLY);
        if (!p) 
        {
            Fail("EXCEPTION_CONTINUE_EXECUTION: test setup via "
                 "VirtualAlloc failed.\n");
        }

        *p = 13;        /* causes an access violation exception */

        bTry2 = TRUE;

 
    }
    PAL_EXCEPT_FILTER(ExitFilter, (LPVOID)&nValidator)
    {
        bExcept = TRUE; /* indicate we hit the PAL_EXCEPT_FILTER block */
        Fail("PAL_EXCEPT_FILTER: ERROR -> in handler despite filter's "
            "continue choice\n");
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the PAL_TRY"
            " block was not executed.\n");
    }

    if (bExcept)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the "
            "PAL_EXCEPT_FILTER block was executed.\n");
    }

    if (!bFilter)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the filter"
            " function was not executed.\n");
    }


    if (!bTry2)
    {
        Trace("PAL_EXCEPT_FILTER: ERROR -> It appears the code in the PAL_TRY"
            " block after the exception causing statements was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry || bExcept || !bFilter)
    {
        Fail("");
    }


    PAL_Terminate();  
    return PASS;

}
