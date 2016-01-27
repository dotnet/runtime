// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

