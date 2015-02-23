//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

BOOL bExcept  = FALSE;
BOOL bTry     = FALSE;
BOOL bFinally = FALSE;

int __cdecl main(int argc, char *argv[])
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
        bTry = TRUE;
        RaiseException(0,0,0,0);

        Fail("RaiseException: ERROR -> code was executed after the "
             "exception was raised.\n");
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        bExcept = TRUE;
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

    /* did we hit all the code blocks? */
    if(!bTry || !bExcept)
    {
        Fail("");
    }

    /* Reinit flags */
    bTry = bExcept = FALSE;


    /*********************************************************
     * Tests the behaviour of RaiseException with
     * PAL_FINALLY
     * (bFinally should be set before bExcept)
     */
    PAL_TRY(VOID*, unused, NULL)
    {
        PAL_TRY(VOID*, unused, NULL)
        {
            bTry = TRUE;
            RaiseException(0,0,0,0);

            Fail("RaiseException: ERROR -> code was executed after the "
                 "exception was raised.\n");
        }
        PAL_FINALLY
        {
            bFinally = TRUE;
        }
        PAL_ENDTRY;
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if( bFinally == FALSE )
        {
            Fail("RaiseException: ERROR -> It appears the code in the "
                 "PAL_EXCEPT executed before the code in PAL_FINALLY.\n");
        }

        bExcept = TRUE;
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
              "PAL_EXCEPT block was not executed.\n");
    }

    if (!bFinally)
    {
        Trace("RaiseException: ERROR -> It appears the code in the "
              "PAL_FINALLY block was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry || !bExcept || !bFinally)
    {
        Fail("");
    }

    PAL_Terminate();
    return PASS;
}
