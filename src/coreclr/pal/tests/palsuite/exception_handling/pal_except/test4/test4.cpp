// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test4.c (exception_handling\pal_except\test4)
**
** Purpose: Test to make sure the PAL_EXCEPT block is executed
**          after an exception occurs in the PAL_TRY block
**          if the PAL_EXCEPT block contains a nested 
**          PAL_TRY-PAL_EXCEPT block
**
**
**===================================================================*/
#include <palsuite.h>

/* Execution flags */
BOOL bTry_palexcept_test4           = FALSE;
BOOL bExcept_palexcept_test4        = FALSE;
BOOL bTry_nested_palexcept_test4    = FALSE;
BOOL bExcept_nested_palexcept_test4 = FALSE;

PALTEST(exception_handling_pal_except_test4_paltest_pal_except_test4, "exception_handling/pal_except/test4/paltest_pal_except_test4")
{
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    PAL_TRY 
    {
        int* p = 0x00000000;   /* NULL pointer */

        bTry_palexcept_test4 = TRUE;    /* indicate we hit the PAL_TRY block */
        *p = 13;        /* causes an access violation exception */

        Fail("ERROR: code was executed after the access violation.\n");
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if (!bTry_palexcept_test4)
        {
            Fail("ERROR: PAL_EXCEPT was hit without PAL_TRY being hit.\n");
        }

        PAL_TRY
        {
            int *lp = 0x00000000;

            bTry_nested_palexcept_test4 = TRUE;
            *lp = 13; /* causes an access violation exception */

            Fail("ERROR: code was executed after the "
                 "nested access violation.\n");
        }
        PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
        {
            if (!bTry_nested_palexcept_test4)
            {
                Fail("ERROR: PAL_EXCEPT was hit without PAL_TRY being hit "
                     "in the nested block.\n");
            }
            bExcept_nested_palexcept_test4 = TRUE;
        }
        PAL_ENDTRY;

        bExcept_palexcept_test4 = TRUE; /* indicate we hit the PAL_EXCEPT block */
    }
    PAL_ENDTRY;

    if (!bTry_palexcept_test4)
    {
        Trace("ERROR: the code in the PAL_TRY block was not executed.\n");
    }

    if (!bExcept_palexcept_test4)
    {
        Trace("ERROR: the code in the PAL_EXCEPT block was not executed.\n");
    }

    if (!bTry_nested_palexcept_test4)
    {
        Trace("ERROR: the code in the nested "
              "PAL_TRY block was not executed.\n");
    }

    if (!bExcept_nested_palexcept_test4)
    {
        Trace("ERROR: the code in the nested "
              "PAL_EXCEPT block was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry_palexcept_test4 || !bExcept_palexcept_test4 ||
       !bTry_nested_palexcept_test4 || !bExcept_nested_palexcept_test4)
    {
        Fail("");
    }


    PAL_Terminate();  
    return PASS;

}
