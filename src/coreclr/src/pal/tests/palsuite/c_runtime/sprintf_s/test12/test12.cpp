// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test12.c
**
** Purpose: Test #12 for the sprintf_s function. Tests the (lowercase)
**          hexadecimal specifier (%x)
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sprintf_s.h"

/* 
 * Depends on memcmp and strlen
 */

PALTEST(c_runtime_sprintf_s_test12_paltest_sprintf_test12, "c_runtime/sprintf_s/test12/paltest_sprintf_test12")
{
    int neg = -42;
    int pos = 0x1234ab;
    INT64 l = I64(0x1234567887654321);
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoNumTest("foo %x", pos, "foo 1234ab");
    DoNumTest("foo %lx", pos, "foo 1234ab");
    DoNumTest("foo %hx", pos, "foo 34ab");
    DoNumTest("foo %Lx", pos, "foo 1234ab");
    DoI64Test("foo %I64x", l, "0x1234567887654321",
              "foo 1234567887654321");
    DoNumTest("foo %7x", pos, "foo  1234ab");
    DoNumTest("foo %-7x", pos, "foo 1234ab ");
    DoNumTest("foo %.1x", pos, "foo 1234ab");
    DoNumTest("foo %.7x", pos, "foo 01234ab");
    DoNumTest("foo %07x", pos, "foo 01234ab");
    DoNumTest("foo %#x", pos, "foo 0x1234ab");
    DoNumTest("foo %+x", pos, "foo 1234ab");
    DoNumTest("foo % x", pos, "foo 1234ab");
    DoNumTest("foo %+x", neg, "foo ffffffd6");
    DoNumTest("foo % x", neg, "foo ffffffd6");

    PAL_Terminate();
    return PASS;
}

