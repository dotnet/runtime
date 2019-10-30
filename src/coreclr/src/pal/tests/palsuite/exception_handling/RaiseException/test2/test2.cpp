// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test2.c (exception_handling\raiseexception\test2)
**
** Purpose: Tests that the correct arguments are passed 
**          to the filter by RaiseException and tests that 
**          the number of arguments never exceeds 
**          EXCEPTION_MAXIMUM_PARAMETERS, even though we
**          pass a greater number of arguments 
**
**
**============================================================*/


#include <palsuite.h>

BOOL bFilter;
BOOL bTry;
BOOL bExcept;

ULONG_PTR lpArguments_test1[EXCEPTION_MAXIMUM_PARAMETERS];
DWORD nArguments_test1 = EXCEPTION_MAXIMUM_PARAMETERS;

ULONG_PTR lpArguments_test2[EXCEPTION_MAXIMUM_PARAMETERS+1];
DWORD nArguments_test2 = EXCEPTION_MAXIMUM_PARAMETERS+1;


/**
**
**  Filter function that checks for the parameters
**
**/
LONG Filter_test1(EXCEPTION_POINTERS* ep, VOID *unused)
{
    int i;
    
    /* let the main know we've hit the filter function */
    bFilter = TRUE;

    if (!bTry)
    {
        Fail("PAL_EXCEPT_FILTER_EX: ERROR -> Something weird is going on."
            " The filter was hit without PAL_TRY being hit.\n");
    }

    
    /* was the correct number of arguments passed */
    if (ep->ExceptionRecord->NumberParameters != (DWORD) nArguments_test1)
    {
        Fail("RaiseException: ERROR -> Number of arguments passed to filter"
            " was %d when it should have been %d",
            ep->ExceptionRecord->NumberParameters,
            nArguments_test1);

    }

    /* were the correct arguments passed */
    for( i=0; ((DWORD)i)<nArguments_test1; i++ )
    {
        if( ep->ExceptionRecord->ExceptionInformation[i] 
            != lpArguments_test1[i])
        {
            Fail("RaiseException: ERROR -> Argument %d passed to filter"
                 " was %d when it should have been %d",
                 i,
                 ep->ExceptionRecord->ExceptionInformation[i],
                 lpArguments_test1[i]);
        }
    }

    return EXCEPTION_EXECUTE_HANDLER;
}

/**
**
**  Filter function that checks for the maximum parameters
**
**/
LONG Filter_test2(EXCEPTION_POINTERS* ep, VOID* unused)
{
    /* let the main know we've hit the filter function */
    bFilter = TRUE;

    if (ep->ExceptionRecord->NumberParameters > EXCEPTION_MAXIMUM_PARAMETERS)
    {
        Fail("RaiseException: ERROR -> Number of arguments passed to filter"
             " was %d which is greater than the maximum allowed of %d\n",
             ep->ExceptionRecord->NumberParameters,
             EXCEPTION_MAXIMUM_PARAMETERS);
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

        /* Initialize arguments to pass to filter */
        for(int i = 0; ((DWORD)i) < nArguments_test1; i++ )
        {
            lpArguments_test1[i] = i;
        }

        RaiseException(0,0,nArguments_test1,lpArguments_test1);

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


    /* Reinit flags */
    bTry = bExcept = bFilter = FALSE;

    /********************************************************
     * Test that the number of arguments never 
     * exceeds EXCEPTION_MAXIMUM_PARAMETERS, even though we
     * pass a greater number of arguments 
     */
    PAL_TRY(VOID*, unused, NULL)
    {
        bTry = TRUE;    /* indicate we hit the PAL_TRY block */

        /* Initialize arguments to pass to filter */
        for(int i = 0; ((DWORD)i) < nArguments_test2; i++ )
        {
            lpArguments_test2[i] = i;
        }

        RaiseException(0,0,nArguments_test2,lpArguments_test2);

        Fail("RaiseException: ERROR -> code was executed after the "
             "exception was raised.\n");
    }
    PAL_EXCEPT_FILTER(Filter_test2)
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
