// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
int
PALAPI
InitializeDllTest1()
{
    PAL_SetInitializeDLLFlags(PAL_INITIALIZE_DLL | PAL_INITIALIZE_REGISTER_SIGNALS);
    return PAL_InitializeDLL();
}

__attribute__((noinline,NOOPT_ATTRIBUTE))
static void FailingFunction(volatile int *p)
{
    if (p == NULL)
    {
        throw PAL_SEHException();
    }

    *p = 1;          // Causes an access violation exception
}

BOOL bTry    = FALSE;
BOOL bExcept = FALSE;

extern "C"
int
PALAPI
DllTest1()
{
    Trace("Starting pal_sxs test1 DllTest1\n");

    PAL_TRY(VOID*, unused, NULL)
    {
        volatile int* p = (volatile int *)0x11000; // Invalid pointer

        bTry = TRUE;                            // Indicate we hit the PAL_TRY block
        FailingFunction(p);  // Throw in function to fool C++ runtime into handling
                             // h/w exception

        Fail("ERROR: code was executed after the access violation.\n");
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if (!bTry)
        {
            Fail("ERROR: PAL_EXCEPT was hit without PAL_TRY being hit.\n");
        }

        // Validate that the faulting address is correct; the contents of "p" (0x11000).
        if (ex.GetExceptionRecord()->ExceptionInformation[1] != 0x11000)
        {
            Fail("ERROR: PAL_EXCEPT ExceptionInformation[1] != 0x11000\n");
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
