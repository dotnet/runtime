// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: test1.c
**
** Purpose: Tests that RaiseException throws a catchable exception
**          and Tests the behaviour of RaiseException with
**          PAL_FINALLY
**
**
**============================================================*/


#include <palsuite.h>

BOOL bExcept_RaiseException_test1  = FALSE;
BOOL bTry_RaiseException_test1     = FALSE;
BOOL bFinally_RaiseException_test1 = FALSE;

PALTEST(exception_handling_RaiseException_test1_paltest_raiseexception_test1, "exception_handling/RaiseException/test1/paltest_raiseexception_test1")
{

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /*********************************************************
     * Tests that RaiseException throws a catchable exception
     */
    PAL_TRY(VOID*, unused, NULL)
    {
        bTry_RaiseException_test1 = TRUE;
        RaiseException(0,0,0,0);

        Fail("RaiseException: ERROR -> code was executed after the "
             "exception was raised.\n");
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        bExcept_RaiseException_test1 = TRUE;
    }
    PAL_ENDTRY;

    if (!bTry_RaiseException_test1)
    {
        Trace("RaiseException: ERROR -> It appears the code in the "
              "PAL_TRY block was not executed.\n");
    }

    if (!bExcept_RaiseException_test1)
    {
        Trace("RaiseException: ERROR -> It appears the code in the "
              "PAL_EXCEPT_FILTER_EX block was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry_RaiseException_test1 || !bExcept_RaiseException_test1)
    {
        Fail("");
    }

    /* Reinit flags */
    bTry_RaiseException_test1 = bExcept_RaiseException_test1 = FALSE;


    /*********************************************************
     * Tests the behaviour of RaiseException with
     * PAL_FINALLY
     * (bFinally_RaiseException_test1 should be set before bExcept_RaiseException_test1)
     */
    PAL_TRY(VOID*, unused, NULL)
    {
        PAL_TRY(VOID*, unused, NULL)
        {
            bTry_RaiseException_test1 = TRUE;
            RaiseException(0,0,0,0);

            Fail("RaiseException: ERROR -> code was executed after the "
                 "exception was raised.\n");
        }
        PAL_FINALLY
        {
            bFinally_RaiseException_test1 = TRUE;
        }
        PAL_ENDTRY;
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if( bFinally_RaiseException_test1 == FALSE )
        {
            Fail("RaiseException: ERROR -> It appears the code in the "
                 "PAL_EXCEPT executed before the code in PAL_FINALLY.\n");
        }

        bExcept_RaiseException_test1 = TRUE;
    }
    
    PAL_ENDTRY;

    if (!bTry_RaiseException_test1)
    {
        Trace("RaiseException: ERROR -> It appears the code in the "
              "PAL_TRY block was not executed.\n");
    }

    if (!bExcept_RaiseException_test1)
    {
        Trace("RaiseException: ERROR -> It appears the code in the "
              "PAL_EXCEPT block was not executed.\n");
    }

    if (!bFinally_RaiseException_test1)
    {
        Trace("RaiseException: ERROR -> It appears the code in the "
              "PAL_FINALLY block was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry_RaiseException_test1 || !bExcept_RaiseException_test1 || !bFinally_RaiseException_test1)
    {
        Fail("");
    }

    PAL_Terminate();
    return PASS;
}
