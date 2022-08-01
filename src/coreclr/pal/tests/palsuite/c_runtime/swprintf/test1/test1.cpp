// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: General test to see if swprintf works correctly
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swprintf.h"

/*
 * Uses memcmp & wcslen
 */

PALTEST(c_runtime_swprintf_test1_paltest_swprintf_test1, "c_runtime/swprintf/test1/paltest_swprintf_test1")
{
    WCHAR *checkstr;
    WCHAR buf[256];

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    checkstr = convert("hello world");
    swprintf_s(buf, ARRAY_SIZE(buf), convert("hello world"));

    if (memcmp(checkstr, buf, wcslen(checkstr)*2+2) != 0)
    {
        Fail("ERROR: Expected \"%s\", got \"%s\".\n", "hello world",
             convertC(buf));
    }

    PAL_Terminate();
    return PASS;
}
