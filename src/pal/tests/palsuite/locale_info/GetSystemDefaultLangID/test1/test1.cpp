// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source: test1.c
**
** Purpose: Checks that GetSystemDefaultLangID can be used to make a valid
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

    LangID = GetSystemDefaultLangID();
    if (LangID == 0)
    {
        Fail("GetSystemDefaultLangID failed!\n");
    }

    /* Try using the langid (with default sort) as a locale */
    if (!SetThreadLocale(MAKELCID(LangID, SORT_DEFAULT)))
    {
        Fail("Unable to use GetSystemDefaultLangID as a locale!\n");
    }
    lcid = GetThreadLocale();
    if (!IsValidLocale(lcid, LCID_INSTALLED))
    {
        Fail("Unable to use GetSystemDefaultLangID as a locale!\n");
    }

    /* Make sure results consistent with using LOCALE_USER_DEFAULT */
    if (!SetThreadLocale(LOCALE_USER_DEFAULT))
    {
        Fail("Unexpected error testing GetSystemDefaultLangID!\n");
    }
    if (GetThreadLocale() != lcid)
    {
        Fail("Results from GetSystemDefaultLangID inconsistent with "
            "LOCALE_USER_DEFAULT!\n");
    }

    PAL_Terminate();

    return PASS;
}
