// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      test9.c
**
** Purpose:     Tests the integer specifier (%i).
**              This test is modeled after the sprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fwprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

int __cdecl main(int argc, char *argv[])
{
    int neg = -42;
    int pos = 42;
    INT64 l = 42;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoNumTest(convert("foo %i"), pos, "foo 42");
    DoNumTest(convert("foo %li"), 0xFFFF, "foo 65535");
    DoNumTest(convert("foo %hi"), 0xFFFF, "foo -1");
    DoNumTest(convert("foo %Li"), pos, "foo 42");
    DoI64Test(convert("foo %I64i"), l, "42", "foo 42", "foo 42");
    DoNumTest(convert("foo %3i"), pos, "foo  42");
    DoNumTest(convert("foo %-3i"), pos, "foo 42 ");
    DoNumTest(convert("foo %.1i"), pos, "foo 42");
    DoNumTest(convert("foo %.3i"), pos, "foo 042");
    DoNumTest(convert("foo %03i"), pos, "foo 042");
    DoNumTest(convert("foo %#i"), pos, "foo 42");
    DoNumTest(convert("foo %+i"), pos, "foo +42");
    DoNumTest(convert("foo % i"), pos, "foo  42");
    DoNumTest(convert("foo %+i"), neg, "foo -42");
    DoNumTest(convert("foo % i"), neg, "foo -42");

    PAL_Terminate();
    return PASS;
}
