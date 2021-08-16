// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: test2.c
**
** Purpose: Tests that GetLocaleInfoW will correctly return the amount of
**          buffer space required.  Also tests that it correctly handles a 
**          buffer of insufficient space.
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(locale_info_GetLocaleInfoW_test2_paltest_getlocaleinfow_test2, "locale_info/GetLocaleInfoW/test2/paltest_getlocaleinfow_test2")
{    
    WCHAR buffer[256] = { 0 };
    int ret;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    ret = GetLocaleInfoW(LOCALE_NEUTRAL, LOCALE_SDECIMAL, buffer, 0);
    if (ret != 2)
    {
        Fail("GetLocaleInfoW gave incorrect desired length for buffer.\n"
            "Expected 2, got %d.\n", ret);
    }

    ret = GetLocaleInfoW(LOCALE_NEUTRAL, LOCALE_SDECIMAL, buffer, 1);
    if (ret != 0)
    {
        Fail("GetLocaleInfoW expected to return 0, returned %d", ret);
    }

    if (GetLastError() != ERROR_INSUFFICIENT_BUFFER)
    {
        Fail("GetLocaleInfoW failed to set last error to "
            "ERROR_INSUFFICIENT_BUFFER!\n");
    }

    PAL_Terminate();

    return PASS;
}

