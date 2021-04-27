// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test7.c (exception_handling\pal_except\test7)
**
** Purpose: Tests the PAL implementation of the PAL_TRY and 
**          PAL_EXCEPT functions. Tests that the EXCEPTION block
**          is missed if no exceptions happen
**
**
**===================================================================*/



#include <palsuite.h>


PALTEST(exception_handling_pal_except_test7_paltest_pal_except_test7, "exception_handling/pal_except/test7/paltest_pal_except_test7")
{
    BOOL bTry = FALSE;
    BOOL bExcept = FALSE;

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /*
    ** Test to make sure we skip the exception block.
    */

    PAL_TRY 
    {
        bTry = TRUE;    /* indicate we hit the PAL_TRY block */
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        bExcept = TRUE; /* indicate we hit the PAL_EXCEPT block */
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("PAL_TRY_EXCEPT: ERROR -> It appears the code in the PAL_TRY"
            " block was not executed.\n");
    }

    if (bExcept)
    {
        Trace("PAL_TRY_EXCEPT: ERROR -> It appears the code in the PAL_EXCEPT"
            " block was executed even though no exception was supposed to"
            " happen.\n");
    }

    /* did we hit the correct code blocks? */
    if(!bTry || bExcept)
    {
        Fail("");
    }

    PAL_Terminate();  
    return PASS;

}
