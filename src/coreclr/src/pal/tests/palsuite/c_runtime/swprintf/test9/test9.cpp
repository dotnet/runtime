// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test9.c
**
** Purpose: Tests swprintf with integer numbers
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swprintf.h"

/*
 * Uses memcmp & wcslen
 */

PALTEST(c_runtime_swprintf_test9_paltest_swprintf_test9, "c_runtime/swprintf/test9/paltest_swprintf_test9")
{
    int neg = -42;
    int pos = 42;
    INT64 l = 42;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoNumTest(convert("foo %i"), pos, convert("foo 42"));
    DoNumTest(convert("foo %li"), 0xFFFF, convert("foo 65535"));
    DoNumTest(convert("foo %hi"), 0xFFFF, convert("foo -1"));
    DoNumTest(convert("foo %Li"), pos, convert("foo 42"));
    DoI64Test(convert("foo %I64i"), l, "0x0000000000000042",
              convert("foo 42"));
    DoNumTest(convert("foo %3i"), pos, convert("foo  42"));
    DoNumTest(convert("foo %-3i"), pos, convert("foo 42 "));
    DoNumTest(convert("foo %.1i"), pos, convert("foo 42"));
    DoNumTest(convert("foo %.3i"), pos, convert("foo 042"));
    DoNumTest(convert("foo %03i"), pos, convert("foo 042"));
    DoNumTest(convert("foo %#i"), pos, convert("foo 42"));
    DoNumTest(convert("foo %+i"), pos, convert("foo +42"));
    DoNumTest(convert("foo % i"), pos, convert("foo  42"));
    DoNumTest(convert("foo %+i"), neg, convert("foo -42"));
    DoNumTest(convert("foo % i"), neg, convert("foo -42"));

    PAL_Terminate();
    return PASS;
}

