// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test5.c (exception_handling\pal_except\test5)
**
** Purpose: Test to make sure the PAL_EXCEPT block is executed
**          after an exception occurs in the PAL_TRY block
**          if the PAL_EXCEPT block calls a function that contains 
**          another PAL_TRY-PAL_EXCEPT block
**
**
**===================================================================*/
#include <palsuite.h>

/* Execution flags */
BOOL bTry           = FALSE;
BOOL bExcept        = FALSE;
BOOL bTry_nested    = FALSE;
BOOL bExcept_nested = FALSE;

PALTEST(exception_handling_pal_except_test5_paltest_pal_except_test5, "exception_handling/pal_except/test5/paltest_pal_except_test5")
{
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    PAL_TRY 
    {
        int* p = 0x00000000;   /* NULL pointer */

        bTry = TRUE;    /* indicate we hit the PAL_TRY block */


        /* Nested PAL_TRY */
        PAL_TRY
        {
            bTry_nested = TRUE;

            *p = 13;    /* causes an access violation exception */

            Fail("ERROR: code was executed after the nested access violation.\n");

        }
        PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
        {
            if (!bTry)
            {
                Fail("ERROR: Nested PAL_EXCEPT was hit without "
                     "nested PAL_TRY being hit.\n");
            }
            bExcept_nested = TRUE;
        }
        PAL_ENDTRY;

        *p = 13;        /* causes an access violation exception */

        Fail("ERROR: code was executed after the access violation.\n");
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if (!bTry)
        {
            Fail("ERROR: PAL_EXCEPT was hit without PAL_TRY being hit.\n");
        }
        if (!bExcept_nested)
        {
            Fail("ERROR: PAL_EXCEPT was hit without "
                 "nested PAL_EXCEPT being hit.\n");
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

    if (!bTry_nested)
    {
        Trace("ERROR: the code in the "
              "nested PAL_TRY block was not executed.\n");
    }

    if (!bExcept_nested)
    {
        Trace("ERROR: the code in the "
              "nested PAL_EXCEPT block was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry || !bExcept ||
       !bTry_nested || !bExcept_nested)
    {
        Fail("");
    }


    PAL_Terminate();  
    return PASS;

}
