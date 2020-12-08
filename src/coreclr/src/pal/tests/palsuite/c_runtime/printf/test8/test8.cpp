// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test8.c
**
** Purpose: Test #8 for the printf function. Tests the decimal
**          specifier (%d).
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../printf.h"



PALTEST(c_runtime_printf_test8_paltest_printf_test8, "c_runtime/printf/test8/paltest_printf_test8")
{
    int neg = -42;
    int pos = 42;
    INT64 l = 42;
    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    DoNumTest("foo %d", pos, "foo 42");
    DoNumTest("foo %ld", 0xFFFF, "foo 65535");
    DoNumTest("foo %hd", 0xFFFF, "foo -1");
    DoNumTest("foo %Ld", pos, "foo 42");
    DoI64Test("foo %I64d", l, "42", "foo 42");
    DoNumTest("foo %3d", pos, "foo  42");
    DoNumTest("foo %-3d", pos, "foo 42 ");
    DoNumTest("foo %.1d", pos, "foo 42");
    DoNumTest("foo %.3d", pos, "foo 042");
    DoNumTest("foo %03d", pos, "foo 042");
    DoNumTest("foo %#d", pos, "foo 42");
    DoNumTest("foo %+d", pos, "foo +42");
    DoNumTest("foo % d", pos, "foo  42");
    DoNumTest("foo %+d", neg, "foo -42");
    DoNumTest("foo % d", neg, "foo -42");

    PAL_Terminate();
    return PASS;
}

