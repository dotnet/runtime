//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main(int argc, char *argv[])
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

