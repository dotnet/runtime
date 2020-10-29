// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  PAL_TRY_EXCEPT.c (test 2)
**
** Purpose: Tests the PAL implementation of the PAL_TRY and 
**          PAL_EXCEPT functions. Exceptions are not forced to ensure
**          the proper blocks are hit.
**
**
**===================================================================*/



#include <palsuite.h>


PALTEST(exception_handling_PAL_TRY_EXCEPT_EX_test2_paltest_pal_try_except_ex_test2, "exception_handling/PAL_TRY_EXCEPT_EX/test2/paltest_pal_try_except_ex_test2")
{
    BOOL bTry = FALSE;
    BOOL bExcept = FALSE;
    BOOL bTestA = TRUE;

    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /*
    ** test to make sure we get into the exception block
    */
    
    PAL_TRY 
    {
        if (!bTestA)
        {
            Fail("PAL_TRY_EXCEPT: ERROR ->"
                " It appears the first try block was hit a second time.\n");
        }
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
        Trace("PAL_TRY_EXCEPT: ERROR -> It appears the code in the first"
            " PAL_EXCEPT block was executed even though no exceptions were "
            "encountered.\n");
    }

    /* did we hit all the proper code blocks? */
    if(!bTry || bExcept)
    {
        Fail("");
    }


    /*
    ** test to make sure we skip the second exception block
    */
    
    bTry = FALSE;
    bExcept = FALSE;
    bTestA = FALSE;    /* we are now going into the second block test */


    PAL_TRY 
    {
        if (bTestA)
        {
            Fail("PAL_TRY_EXCEPT: ERROR -> It appears"
                " the second try block was hit too early.\n");
        }
        bTry = TRUE;    /* indicate we hit the PAL_TRY block */
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if (bTestA)
        {
            Fail("PAL_TRY_EXCEPT: ERROR -> It appears"
                " the second except block was hit too early.\n");
        }
        bExcept = TRUE; /* indicate we hit the PAL_TRY block */
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("PAL_TRY_EXCEPT: ERROR -> It appears the code in the second"
            " PAL_TRY block was not executed.\n");
    }

    if (bExcept)
    {
        Trace("PAL_TRY_EXCEPT: ERROR -> It appears the code in the second"
            " PAL_EXCEPT block was executed even though no exceptions were "
            "encountered.\n");
    }

    /* did we hit all the proper code blocks? */
    if(!bTry || bExcept)
    {
        Fail("\n");
    }

    PAL_Terminate();  
    return PASS;

}
