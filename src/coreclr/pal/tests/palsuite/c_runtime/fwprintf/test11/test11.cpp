// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      test11.c
**
** Purpose:     Test the unsigned int specifier (%u).
**              This test is modeled after the sprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fwprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

PALTEST(c_runtime_fwprintf_test11_paltest_fwprintf_test11, "c_runtime/fwprintf/test11/paltest_fwprintf_test11")
{
    int neg = -42;
    int pos = 42;
    INT64 l = 42;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoNumTest(convert("foo %u"), pos, "foo 42");
    DoNumTest(convert("foo %lu"), 0xFFFF, "foo 65535");
    DoNumTest(convert("foo %hu"), 0xFFFF, "foo 65535");
    DoNumTest(convert("foo %Lu"), pos, "foo 42");
    DoI64Test(convert("foo %I64u"), l, "42", "foo 42", "foo 42");
    DoNumTest(convert("foo %3u"), pos, "foo  42");
    DoNumTest(convert("foo %-3u"), pos, "foo 42 ");
    DoNumTest(convert("foo %.1u"), pos, "foo 42");
    DoNumTest(convert("foo %.3u"), pos, "foo 042");
    DoNumTest(convert("foo %03u"), pos, "foo 042");
    DoNumTest(convert("foo %#u"), pos, "foo 42");
    DoNumTest(convert("foo %+u"), pos, "foo 42");
    DoNumTest(convert("foo % u"), pos, "foo 42");
    DoNumTest(convert("foo %+u"), neg, "foo 4294967254");
    DoNumTest(convert("foo % u"), neg, "foo 4294967254");

    PAL_Terminate();
    return PASS;
}
