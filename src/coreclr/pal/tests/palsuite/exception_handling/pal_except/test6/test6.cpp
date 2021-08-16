// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test6.c (exception_handling\pal_except\test6)
**
** Purpose: Test to make sure the PAL_EXCEPT block is executed
**          after an exception occurs in the PAL_TRY block
**          that contains multiple PAL_TRY-PAL_EXCEPT blocks
**
**
**===================================================================*/
#include <palsuite.h>

/* Execution flags */
BOOL bTry            = FALSE;
BOOL bExcept         = FALSE;
BOOL bTry_nested     = FALSE;
BOOL bExcept_nested  = FALSE;
BOOL bTry_nested2    = FALSE;
BOOL bExcept_nested2 = FALSE;


PALTEST(exception_handling_pal_except_test6_paltest_pal_except_test6, "exception_handling/pal_except/test6/paltest_pal_except_test6")
{
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* First block */
    PAL_TRY 
    {
        int* p = 0x00000000;   /* NULL pointer */

        bTry = TRUE;    /* indicate we hit the PAL_TRY block */

        /* Second PAL_TRY block */
        PAL_TRY
        {
            bTry_nested = TRUE;

            /* Third PAL_TRY block*/
            PAL_TRY
            {
                bTry_nested2 = TRUE;

                *p = 13;    /* causes an access violation exception */

                Fail("ERROR: code was executed after the nested access violation.\n");

            }
            PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
            {
                if (!bTry_nested2)
                {
                    Fail("ERROR: Third PAL_EXCEPT was hit without "
                        "third PAL_TRY being hit.\n");
                }
                bExcept_nested2 = TRUE;
            }
            PAL_ENDTRY;

            *p = 13;    /* causes an access violation exception */

            Fail("ERROR: code was executed after the nested access violation.\n");

        }
        PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
        {
            if (!bTry_nested)
            {
                Fail("ERROR: Second PAL_EXCEPT was hit without "
                     "second PAL_TRY being hit.\n");
            }
            if (!bExcept_nested2)
            {
                Fail("ERROR: second PAL_EXCEPT was hit without "
                    "third PAL_EXCEPT being hit.\n");
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
            Fail("ERROR: first PAL_EXCEPT was hit without "
                 "second PAL_EXCEPT being hit.\n");
        }
        if (!bExcept_nested2)
        {
            Fail("ERROR: first PAL_EXCEPT was hit without "
                 "third PAL_EXCEPT being hit.\n");
        }

        bExcept = TRUE; /* indicate we hit the PAL_EXCEPT block */
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("ERROR: the code in the "
              "first PAL_TRY block was not executed.\n");
    }

    if (!bExcept)
    {
        Trace("ERROR: the code in the "
              "first PAL_EXCEPT block was not executed.\n");
    }

    if (!bTry_nested)
    {
        Trace("ERROR: the code in the "
              "second PAL_TRY block was not executed.\n");
    }

    if (!bExcept_nested)
    {
        Trace("ERROR: the code in the "
              "second PAL_EXCEPT block was not executed.\n");
    }

    if (!bTry_nested2)
    {
        Trace("ERROR: the code in the "
              "third PAL_TRY block was not executed.\n");
    }

    if (!bExcept_nested2)
    {
        Trace("ERROR: the code in the "
              "third PAL_EXCEPT block was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry || !bExcept ||
       !bTry_nested || !bExcept_nested ||
       !bTry_nested2 || !bExcept_nested2)
    {
        Fail("");
    }


    PAL_Terminate();  
    return PASS;

}
