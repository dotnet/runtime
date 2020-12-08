// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
BOOL bTry_pal_except_test3             = FALSE;
BOOL bExcept_pal_except_test3          = FALSE;
BOOL bTry_function_pal_except_test3    = FALSE;
BOOL bExcept_function_pal_except_test3 = FALSE;

/* 
 * Helper function that contains a PAL_TRY-PAL_EXCEPT block
 */
void Helper()
{
    /* Nested PAL_TRY */
    PAL_TRY
    {
        int *lp = 0x00000000;

        bTry_function_pal_except_test3 = TRUE;

        *lp = 13;    /* causes an access violation exception */

        Fail("ERROR: code was executed after the function's access violation.\n");

    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if (!bTry_pal_except_test3)
        {
            Fail("ERROR: Nested PAL_EXCEPT was hit without "
                    "the function's PAL_TRY being hit.\n");
        }
        bExcept_function_pal_except_test3 = TRUE;
    }
    PAL_ENDTRY;
}

PALTEST(exception_handling_pal_except_test3_paltest_pal_except_test3, "exception_handling/pal_except/test3/paltest_pal_except_test3")
{
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    PAL_TRY 
    {
        int* p = 0x00000000;   /* NULL pointer */

        bTry_pal_except_test3 = TRUE;    /* indicate we hit the PAL_TRY block */

        Helper();

        *p = 13;        /* causes an access violation exception */

        Fail("ERROR: code was executed after the access violation.\n");
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if (!bTry_pal_except_test3)
        {
            Fail("ERROR: PAL_EXCEPT was hit without PAL_TRY being hit.\n");
        }
        if (!bExcept_function_pal_except_test3)
        {
            Fail("ERROR: PAL_EXCEPT was hit without "
                 "function's PAL_EXCEPT being hit.\n");
        }

        bExcept_pal_except_test3 = TRUE; /* indicate we hit the PAL_EXCEPT block */
    }
    PAL_ENDTRY;

    if (!bTry_pal_except_test3)
    {
        Trace("ERROR: the code in the PAL_TRY block was not executed.\n");
    }

    if (!bExcept_pal_except_test3)
    {
        Trace("ERROR: the code in the PAL_EXCEPT block was not executed.\n");
    }

    if (!bTry_function_pal_except_test3)
    {
        Trace("ERROR: the code in the "
              "function's PAL_TRY block was not executed.\n");
    }

    if (!bExcept_function_pal_except_test3)
    {
        Trace("ERROR: the code in the "
              "function's PAL_EXCEPT block was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry_pal_except_test3 || !bExcept_pal_except_test3 ||
       !bTry_function_pal_except_test3 || !bExcept_function_pal_except_test3)
    {
        Fail("");
    }

    PAL_Terminate();  
    return PASS;

}
