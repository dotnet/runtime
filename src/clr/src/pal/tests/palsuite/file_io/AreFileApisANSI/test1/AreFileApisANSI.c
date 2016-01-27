// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  AreFileApisANSI.c
**
** Purpose: Tests the PAL implementation of the AreFileApisANSI function.
**          The only possible return is TRUE.
**
**
**===================================================================*/



#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    BOOL bRc = FALSE;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    bRc = AreFileApisANSI();


    if (bRc == FALSE)
    {
        Fail("AreFileApisANSI: ERROR: Function returned FALSE whereas only TRUE "
            "is acceptable.\n");
    }

    PAL_Terminate();  
    return PASS;
}
