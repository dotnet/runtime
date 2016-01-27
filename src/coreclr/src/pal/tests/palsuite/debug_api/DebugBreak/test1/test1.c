// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test1.c
**
** Purpose: Tests that DebugBreak works in the grossest fashion.
**
**
**============================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
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
