// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  dlltest2.c (exception_handling\pal_sxs\test1)
**
** Purpose: Test to make sure the PAL_EXCEPT block is executed
**          after an exception occurs in the PAL_TRY block with
**          multiple PALs in the process.
**
**
**===================================================================*/
#include <palsuite.h>

extern "C"
int InitializeDllTest2()
{
    return PAL_InitializeDLL();
}

BOOL bTry    = FALSE;
BOOL bExcept = FALSE;

extern "C"
int DllTest2()
{
    Trace("Starting pal_sxs test1 DllTest2\n");

    PAL_TRY(VOID*, unused, NULL)
    {
        volatile int* p = (volatile int *)0x22; /* invalid pointer */

        bTry = TRUE;    /* indicate we hit the PAL_TRY block */
        *p = 13;        /* causes an access violation exception */

        Fail("ERROR: code was executed after the access violation.\n");
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if (!bTry)
        {
            Fail("ERROR: PAL_EXCEPT was hit without PAL_TRY being hit.\n");
        }

        bExcept = TRUE; /* indicate we hit the PAL_EXCEPT block */
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("ERROR: the code in the PAL_TRY block was not executed.\n");
    }

    if (!bExcept)
    {
        Trace("ERROR: the code in the PAL_EXCEPT block was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry || !bExcept)
    {
        Fail("DllTest2 FAILED\n");
    }

    Trace("DLLTest2 PASSED\n");

    return PASS;
}
