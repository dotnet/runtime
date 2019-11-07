// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test3.c (exception_handling\pal_except\test3)
**
** Purpose: Test to make sure the PAL_EXCEPT block is executed
**          after an exception occurs in the PAL_TRY block
**          that calls a function that contains 
**          another PAL_TRY-PAL_EXCEPT block
**
**
**===================================================================*/
#include <palsuite.h>

/* Execution flags */
BOOL bTry             = FALSE;
BOOL bExcept          = FALSE;
BOOL bTry_function    = FALSE;
BOOL bExcept_function = FALSE;

/* 
 * Helper function that contains a PAL_TRY-PAL_EXCEPT block
 */
void Helper()
{
    /* Nested PAL_TRY */
    PAL_TRY
    {
        int *lp = 0x00000000;

        bTry_function = TRUE;

        *lp = 13;    /* causes an access violation exception */

        Fail("ERROR: code was executed after the function's access violation.\n");

    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if (!bTry)
        {
            Fail("ERROR: Nested PAL_EXCEPT was hit without "
                    "the function's PAL_TRY being hit.\n");
        }
        bExcept_function = TRUE;
    }
    PAL_ENDTRY;
}

int __cdecl main(int argc, char *argv[])
{
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    PAL_TRY 
    {
        int* p = 0x00000000;   /* NULL pointer */

        bTry = TRUE;    /* indicate we hit the PAL_TRY block */

        Helper();

        *p = 13;        /* causes an access violation exception */

        Fail("ERROR: code was executed after the access violation.\n");
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if (!bTry)
        {
            Fail("ERROR: PAL_EXCEPT was hit without PAL_TRY being hit.\n");
        }
        if (!bExcept_function)
        {
            Fail("ERROR: PAL_EXCEPT was hit without "
                 "function's PAL_EXCEPT being hit.\n");
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

    if (!bTry_function)
    {
        Trace("ERROR: the code in the "
              "function's PAL_TRY block was not executed.\n");
    }

    if (!bExcept_function)
    {
        Trace("ERROR: the code in the "
              "function's PAL_EXCEPT block was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry || !bExcept ||
       !bTry_function || !bExcept_function)
    {
        Fail("");
    }

    PAL_Terminate();  
    return PASS;

}
