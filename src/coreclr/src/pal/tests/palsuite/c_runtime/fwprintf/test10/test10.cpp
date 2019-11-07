// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:      test10.c
**
** Purpose:     Tests the octal specifier (%o).
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

    DoNumTest(convert("foo %o"), pos, "foo 52");
    DoNumTest(convert("foo %lo"), 0xFFFF, "foo 177777");
    DoNumTest(convert("foo %ho"), 0xFFFF, "foo 177777");
    DoNumTest(convert("foo %Lo"), pos, "foo 52");
    DoI64Test(convert("foo %I64o"), l, "42", "foo 52", "foo 52");
    DoNumTest(convert("foo %3o"), pos, "foo  52");
    DoNumTest(convert("foo %-3o"), pos, "foo 52 ");
    DoNumTest(convert("foo %.1o"), pos, "foo 52");
    DoNumTest(convert("foo %.3o"), pos, "foo 052");
    DoNumTest(convert("foo %03o"), pos, "foo 052");
    DoNumTest(convert("foo %#o"), pos, "foo 052");
    DoNumTest(convert("foo %+o"), pos, "foo 52");
    DoNumTest(convert("foo % o"), pos, "foo 52");
    DoNumTest(convert("foo %+o"), neg, "foo 37777777726");
    DoNumTest(convert("foo % o"), neg, "foo 37777777726");
  
    PAL_Terminate();
    return PASS;
}
