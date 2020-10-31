// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      test12.c
**
** Purpose:     Tests the (lowercase) hexadecimal specifier (%x).
**              This test is modeled after the sprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fwprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

PALTEST(c_runtime_fwprintf_test12_paltest_fwprintf_test12, "c_runtime/fwprintf/test12/paltest_fwprintf_test12")
{
    int neg = -42;
    int pos = 0x1234ab;
    INT64 l = I64(0x1234567887654321);
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoNumTest(convert("foo %x"), pos, "foo 1234ab");
    DoNumTest(convert("foo %lx"), pos, "foo 1234ab");
    DoNumTest(convert("foo %hx"), pos, "foo 34ab");
    DoNumTest(convert("foo %Lx"), pos, "foo 1234ab");
    DoI64Test(convert("foo %I64x"), l, "0x1234567887654321",
        "foo 1234567887654321", "foo 0x1234567887654321");
    DoNumTest(convert("foo %7x"), pos, "foo  1234ab");
    DoNumTest(convert("foo %-7x"), pos, "foo 1234ab ");
    DoNumTest(convert("foo %.1x"), pos, "foo 1234ab");
    DoNumTest(convert("foo %.7x"), pos, "foo 01234ab");
    DoNumTest(convert("foo %07x"), pos, "foo 01234ab");
    DoNumTest(convert("foo %#x"), pos, "foo 0x1234ab");
    DoNumTest(convert("foo %+x"), pos, "foo 1234ab");
    DoNumTest(convert("foo % x"), pos, "foo 1234ab");
    DoNumTest(convert("foo %+x"), neg, "foo ffffffd6");
    DoNumTest(convert("foo % x"), neg, "foo ffffffd6");

    PAL_Terminate();
    return PASS;
}
