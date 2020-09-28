// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test11.c
**
** Purpose: Tests swprintf with unsigned numbers
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swprintf.h"

/*
 * Uses memcmp & wcslen
 */

PALTEST(c_runtime_swprintf_test11_paltest_swprintf_test11, "c_runtime/swprintf/test11/paltest_swprintf_test11")
{
    int neg = -42;
    int pos = 42;
    INT64 l = 42;
        
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoNumTest(convert("foo %u"), pos, convert("foo 42"));
    DoNumTest(convert("foo %lu"), 0xFFFF, convert("foo 65535"));
    DoNumTest(convert("foo %hu"), 0xFFFF, convert("foo 65535"));
    DoNumTest(convert("foo %Lu"), pos, convert("foo 42"));
    DoI64Test(convert("foo %I64u"), l, "0x0000000000000042",
                      convert("foo 42"));
    DoNumTest(convert("foo %3u"), pos, convert("foo  42"));
    DoNumTest(convert("foo %-3u"), pos, convert("foo 42 "));
    DoNumTest(convert("foo %.1u"), pos, convert("foo 42"));
    DoNumTest(convert("foo %.3u"), pos, convert("foo 042"));
    DoNumTest(convert("foo %03u"), pos, convert("foo 042"));
    DoNumTest(convert("foo %#u"), pos, convert("foo 42"));
    DoNumTest(convert("foo %+u"), pos, convert("foo 42"));
    DoNumTest(convert("foo % u"), pos, convert("foo 42"));
    DoNumTest(convert("foo %+u"), neg, convert("foo 4294967254"));
    DoNumTest(convert("foo % u"), neg, convert("foo 4294967254"));

    PAL_Terminate();
    return PASS;
}

