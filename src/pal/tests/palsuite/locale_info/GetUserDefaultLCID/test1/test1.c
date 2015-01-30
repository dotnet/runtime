//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source: test1.c
**
** Purpose: Tests that GetUserDefaultLCID returns a valid locale that is 
**          consistent with LOCALE_USER_DEFAULT.
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
    
    lcid = GetUserDefaultLCID();
    if (lcid == 0)
    {
        Fail("GetUserDefaultLCID failed!\n");
    }

    if (!IsValidLocale(lcid, LCID_INSTALLED))
    {
        Fail("GetUserDefaultLCID gave an invalid locale!\n");
    }

    /* Make sure results consistent with using LOCALE_USER_DEFAULT */
    if (!SetThreadLocale(LOCALE_USER_DEFAULT))
    {
        Fail("Unexpected error testing GetUserDefaultLCID!\n");
    }
    if (GetThreadLocale() != lcid)
    {
        Fail("Results from GetUserDefaultLCID inconsistent with "
            "LOCALE_USER_DEFAULT!\n");
    }

    PAL_Terminate();

    return PASS;
}

