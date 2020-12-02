// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: test1.c
**
** Purpose: Tests that DebugBreak works in the grossest fashion.
**
**
**============================================================*/

#include <palsuite.h>

PALTEST(debug_api_DebugBreak_test1_paltest_debugbreak_test1, "debug_api/DebugBreak/test1/paltest_debugbreak_test1")
{
    BOOL bTry = FALSE;

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    
    PAL_TRY 
    {
        DebugBreak();
        if (!bTry)
        {
            Fail("DebugBreak: Continued in Try block.\n");
        }
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        bTry = TRUE;
    }
    PAL_ENDTRY;

    if (!bTry)
    {
         Fail("DebugBreak: Did not reach the exception block.\n");
    }


    PAL_Terminate();
    return PASS;
}
