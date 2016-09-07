// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source: test1.c
**
** Purpose: Tests IsValidLocale with the current locale, -1, and 
**          LOCALE_USER_DEFAULT (which actually isn't valid).
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

    
    /*
     * Passing LOCALE_USER_DEFAULT to IsValidLocale will fail, so instead
     * the current thread localed is changed to it, and that lcid is passed
     * to IsValidLocale (which should always pass)
     */
    if (!SetThreadLocale(LOCALE_USER_DEFAULT))
    {
        Fail("Unable to set locale to LOCALE_USER_DEFAULT!\n");
    }

    lcid = GetThreadLocale();

    if (!IsValidLocale(lcid, LCID_SUPPORTED))
    {
        Fail("IsValidLocale found the default user locale unsupported!\n");
    }
    if (!IsValidLocale(lcid, LCID_INSTALLED))
    {
        Fail("IsValidLocale found the default user locale uninstalled!\n");
    }

    /*
     * Test out bad parameters
     */
    if (IsValidLocale(-1, LCID_SUPPORTED))
    {
        Fail("IsValideLocale passed with an invalid LCID!\n");
    }    
    if (IsValidLocale(-1, LCID_INSTALLED))
    {
        Fail("IsValideLocale passed with an invalid LCID!\n");
    }    

    if (IsValidLocale(LOCALE_USER_DEFAULT, LCID_SUPPORTED))
    {
        Fail("IsValidLocale passed with LOCALE_USER_DEFAULT!\n");
    }
    if (IsValidLocale(LOCALE_USER_DEFAULT, LCID_INSTALLED))
    {
        Fail("IsValidLocale passed with LOCALE_USER_DEFAULT!\n");
    }

    PAL_Terminate();

    return PASS;
}

