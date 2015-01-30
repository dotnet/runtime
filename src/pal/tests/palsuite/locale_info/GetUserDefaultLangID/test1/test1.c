//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source: test1.c
**
** Purpose: Checks that GetUserDefaultLangID can be used to make a valid
**          locale, and that it is consistent with LOCALE_USER_DEFAULT.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{    

    LCID lcid;
    LANGID LangID;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    LangID = GetUserDefaultLangID();
    if (LangID == 0)
    {
        Fail("GetUserDefaultLangID failed!\n");
    }

    /* Try using the langid (with default sort) as a locale */
    if (!SetThreadLocale(MAKELCID(LangID, SORT_DEFAULT)))
    {
        Fail("Unable to use GetUserDefaultLangID as a locale!\n");
    }
    lcid = GetThreadLocale();
    if (!IsValidLocale(lcid, LCID_INSTALLED))
    {
        Fail("Unable to use GetUserDefaultLangID as a locale!\n");
    }

    /* Make sure results consistent with using LOCALE_USER_DEFAULT */
    if (!SetThreadLocale(LOCALE_USER_DEFAULT))
    {
        Fail("Unexpected error testing GetUserDefaultLangID!\n");
    }
    if (GetThreadLocale() != lcid)
    {
        Fail("Results from GetUserDefaultLangID inconsistent with "
            "LOCALE_USER_DEFAULT!\n");
    }

    PAL_Terminate();

    return PASS;
}

