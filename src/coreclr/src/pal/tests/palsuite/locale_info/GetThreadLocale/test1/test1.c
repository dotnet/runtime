//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source: test1.c
**
** Purpose: Tests that GetThreadLocale returns a valid locale.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    LCID lcid;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    lcid = GetThreadLocale();
    
    if (!IsValidLocale(lcid, LCID_INSTALLED))
    {
        Fail("GetThreadLocale returned a locale that is not installed!\n");
    }

    PAL_Terminate();

    return PASS;
}

