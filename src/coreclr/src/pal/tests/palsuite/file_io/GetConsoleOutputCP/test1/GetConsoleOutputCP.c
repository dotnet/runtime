//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
