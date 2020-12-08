// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  pal_try_leave_finally.c
**
** Purpose: Tests the PAL implementation of the PAL_TRY, PAL_LEAVE  
**          and PAL_FINALLY functions.
**
**
**===================================================================*/



#include <palsuite.h>


PALTEST(exception_handling_PAL_TRY_LEAVE_FINALLY_test1_paltest_pal_try_leave_finally_test1, "exception_handling/PAL_TRY_LEAVE_FINALLY/test1/paltest_pal_try_leave_finally_test1")
{
    BOOL bTry = FALSE;
    BOOL bFinally = FALSE;
    BOOL bLeave = TRUE;

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    PAL_TRY 
    {
        bTry = TRUE;    /* indicate we hit the PAL_TRY block */

        goto Done;

        bLeave = FALSE; /* indicate we stuck around */
    Done: ;
    }
    PAL_FINALLY
    {
        bFinally = TRUE;    /* indicate we hit the PAL_FINALLY block */
    }
    PAL_ENDTRY;

    /* did we go where we were meant to go */
    if (!bTry)
    {
        Trace("PAL_TRY_FINALLY: ERROR -> It appears the code in the PAL_TRY"
            " block was not executed.\n");
    }

    if (!bLeave)
    {
        Trace("PAL_TRY_FINALLY: ERROR -> It appears code was executed after "
            "PAL_LEAVE was called. It should have jumped directly to the "
            "PAL_FINALLY block.\n");
    }

    if (!bFinally)
    {
        Trace("PAL_TRY_FINALLY: ERROR -> It appears the code in the PAL_FINALLY"
            " block was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry || !bLeave || !bFinally)
    {
        Fail("");
    }


    PAL_Terminate();  
    return PASS;

}
