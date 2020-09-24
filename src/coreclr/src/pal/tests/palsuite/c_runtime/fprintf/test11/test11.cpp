// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      test11.c (fprintf)
**
** Purpose:     Test the unsigned int specifier (%u).
**              This test is modeled after the fprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

PALTEST(c_runtime_fprintf_test11_paltest_fprintf_test11, "c_runtime/fprintf/test11/paltest_fprintf_test11")
{
    int neg = -42;
    int pos = 42;
    INT64 l = 42;
    
    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);

    DoNumTest("foo %u", pos, "foo 42");
    DoNumTest("foo %lu", 0xFFFF, "foo 65535");
    DoNumTest("foo %hu", 0xFFFF, "foo 65535");
    DoNumTest("foo %Lu", pos, "foo 42");
    DoI64Test("foo %I64u", l, "42", "foo 42", "foo 42");
    DoNumTest("foo %3u", pos, "foo  42");
    DoNumTest("foo %-3u", pos, "foo 42 ");
    DoNumTest("foo %.1u", pos, "foo 42");
    DoNumTest("foo %.3u", pos, "foo 042");
    DoNumTest("foo %03u", pos, "foo 042");
    DoNumTest("foo %#u", pos, "foo 42");
    DoNumTest("foo %+u", pos, "foo 42");
    DoNumTest("foo % u", pos, "foo 42");
    DoNumTest("foo %+u", neg, "foo 4294967254");
    DoNumTest("foo % u", neg, "foo 4294967254");

    PAL_Terminate();
    return PASS;
}
