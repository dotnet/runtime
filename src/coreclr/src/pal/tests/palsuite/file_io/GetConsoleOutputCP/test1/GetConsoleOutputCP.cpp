// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  GetConsoleOutputCP.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetConsoleOutputCP function.
**
**
**===================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    UINT uiCP = 0;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    uiCP = GetConsoleOutputCP();
    if ((uiCP != CP_ACP) && (uiCP != GetACP()) && (uiCP != 437)) /*437 for MSDOS*/
    {
        Fail("GetConsoleOutputCP: ERROR -> The invalid code page %d was returned.\n", 
            uiCP);
    }

    PAL_Terminate();
    return PASS;
}
