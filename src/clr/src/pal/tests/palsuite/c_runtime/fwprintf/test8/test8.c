//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:      test8.c
**
** Purpose:     Tests the decimal specifier (%d).
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

    DoNumTest(convert("foo %d"), pos, "foo 42");
    DoNumTest(convert("foo %ld"), 0xFFFF, "foo 65535");
    DoNumTest(convert("foo %hd"), 0xFFFF, "foo -1");
    DoNumTest(convert("foo %Ld"), pos, "foo 42");
    DoI64Test(convert("foo %I64d"), l, "42", "foo 42", "foo 42");
    DoNumTest(convert("foo %3d"), pos, "foo  42");
    DoNumTest(convert("foo %-3d"), pos, "foo 42 ");
    DoNumTest(convert("foo %.1d"), pos, "foo 42");
    DoNumTest(convert("foo %.3d"), pos, "foo 042");
    DoNumTest(convert("foo %03d"), pos, "foo 042");
    DoNumTest(convert("foo %#d"), pos, "foo 42");
    DoNumTest(convert("foo %+d"), pos, "foo +42");
    DoNumTest(convert("foo % d"), pos, "foo  42");
    DoNumTest(convert("foo %+d"), neg, "foo -42");
    DoNumTest(convert("foo % d"), neg, "foo -42");

    PAL_Terminate();
    return PASS;
}
