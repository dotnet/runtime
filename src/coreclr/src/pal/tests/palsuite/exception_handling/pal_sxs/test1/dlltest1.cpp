// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  dlltest1.c (exception_handling\pal_sxs\test1)
**
** Purpose: Test to make sure the PAL_EXCEPT block is executed
**          after an exception occurs in the PAL_TRY block with
**          multiple PALs in the process.
**
**
**===================================================================*/
#include <palsuite.h>

extern "C"
int InitializeDllTest1()
{
    return PAL_InitializeDLL();
}

BOOL bTry    = FALSE;
BOOL bExcept = FALSE;

extern "C"
int DllTest1()
{
    Trace("Starting pal_sxs test1 DllTest1\n");

    PAL_TRY(VOID*, unused, NULL)
    {
        volatile int* p = (volatile int *)0x11; // Invalid pointer

        bTry = TRUE;                            // Indicate we hit the PAL_TRY block
        *p = 1;                                 // Causes an access violation exception

        Fail("ERROR: code was executed after the access violation.\n");
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if (!bTry)
        {
            Fail("ERROR: PAL_EXCEPT was hit without PAL_TRY being hit.\n");
        }

        // Validate that the faulting address is correct; the contents of "p" (0x11).
        if (ex.ExceptionRecord.ExceptionInformation[1] != 0x11)
        {
            Fail("ERROR: PAL_EXCEPT ExceptionInformation[1] != 0x11\n");
        }

        bExcept = TRUE;                         // Indicate we hit the PAL_EXCEPT block 
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

    // Did we hit all the code blocks? 
    if(!bTry || !bExcept)
    {
        Fail("DllTest1 FAILED\n");
    }

    Trace("DLLTest1 PASSED\n");
    return PASS;
}
