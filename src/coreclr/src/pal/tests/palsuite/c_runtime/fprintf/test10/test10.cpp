// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      test10.c (fprintf)
**
** Purpose:     Tests the octal specifier (%o).
**              This test is modeled after the fprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

PALTEST(c_runtime_fprintf_test10_paltest_fprintf_test10, "c_runtime/fprintf/test10/paltest_fprintf_test10")
{
    int neg = -42;
    int pos = 42;
    INT64 l = 42;
    
    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);

    DoNumTest("foo %o", pos, "foo 52");
    DoNumTest("foo %lo", 0xFFFF, "foo 177777");
    DoNumTest("foo %ho", 0xFFFF, "foo 177777");
    DoNumTest("foo %Lo", pos, "foo 52");
    DoI64Test("foo %I64o", l, "42", "foo 52", "foo 52");
    DoNumTest("foo %3o", pos, "foo  52");
    DoNumTest("foo %-3o", pos, "foo 52 ");
    DoNumTest("foo %.1o", pos, "foo 52");
    DoNumTest("foo %.3o", pos, "foo 052");
    DoNumTest("foo %03o", pos, "foo 052");
    DoNumTest("foo %#o", pos, "foo 052");
    DoNumTest("foo %+o", pos, "foo 52");
    DoNumTest("foo % o", pos, "foo 52");
    DoNumTest("foo %+o", neg, "foo 37777777726");
    DoNumTest("foo % o", neg, "foo 37777777726");
  
    PAL_Terminate();
    return PASS;
}
