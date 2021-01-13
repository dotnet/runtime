// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  PAL_TRY_EXCEPT.c (test 1)
**
** Purpose: Tests the PAL implementation of the PAL_TRY and 
**          PAL_EXCEPT functions. Exceptions are forced to ensure
**          the exception blocks are hit.
**
**
**===================================================================*/



#include <palsuite.h>


PALTEST(exception_handling_PAL_TRY_EXCEPT_EX_test1_paltest_pal_try_except_ex_test1, "exception_handling/PAL_TRY_EXCEPT_EX/test1/paltest_pal_try_except_ex_test1")
{
    int* p = 0x00000000;   /* NULL pointer */
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
        *p = 13;        /* causes an access violation exception */
        Fail("PAL_TRY_EXCEPT: ERROR -> code was executed after the "
            "access violation.\n");
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if (!bTestA)
        {
            Fail("PAL_TRY_EXCEPT: ERROR ->"
                " It appears the first except block was hit a second time.\n");
        }
        bExcept = TRUE; /* indicate we hit the PAL_EXCEPT block */
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("PAL_TRY_EXCEPT: ERROR -> It appears the code in the PAL_TRY"
            " block was not executed.\n");
    }

    if (!bExcept)
    {
        Trace("PAL_TRY_EXCEPT: ERROR -> It appears the code in the first"
            " PAL_EXCEPT block was not executed.\n");
    }

    /* did we hit all the code blocks? */
    if(!bTry || !bExcept)
    {
        Fail("");
    }


    /*
    ** test to make sure we get into the second exception block
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
        *p = 13;        /* causes an access violation exception */
        Fail("PAL_TRY_EXCEPT: ERROR -> code was executed after the "
            "access violation.\n");
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        if (bTestA)
        {
            Fail("PAL_TRY_EXCEPT: ERROR -> It appears"
                " the second except block was hit too early.\n");
        }
        bExcept = TRUE; /* indicate we hit the PAL_EXCEPT block */
    }
    PAL_ENDTRY;

    if (!bTry)
    {
        Trace("PAL_TRY_EXCEPT: ERROR -> It appears the code in the second"
            " PAL_TRY block was not executed.");
    }

    if (!bExcept)
    {
        Trace("PAL_TRY_EXCEPT: ERROR -> It appears the code in the PAL_EXCEPT"
            " block was not executed.");
    }

    /* did we hit all the code blocks? */
    if(!bTry || !bExcept)
    {
        Fail("\n");
    }

    PAL_Terminate();  
    return PASS;

}
