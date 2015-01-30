//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test3.c
**
** Purpose: Test that passes CP_ACP to GetCPInfo, verifying the results.
**
**
**==========================================================================*/

#include <palsuite.h>

/* Currently only one CodePage "CP_ACP" is supported by the PAL */

int __cdecl main(int argc, char *argv[])
{
    CPINFO cpinfo;
    
    /* Initialize the PAL.
     */
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Test GetCPInfo with CP_ACP.
     */
    if (!GetCPInfo(CP_ACP, &cpinfo))
    {
        Fail("GetCPInfo() unable to get info for code page %d!\n", CP_ACP);
    }

    /* Terminate the PAL.
     */
    PAL_Terminate();
    return PASS;
}

