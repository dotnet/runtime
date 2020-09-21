// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test13.c
**
** Purpose:   Test #13 for the vsprintf function.
**
**
**===================================================================*/   

#include <palsuite.h>
#include "../vsprintf.h"

/*
 * Notes: memcmp is used, as is strlen.
 */


PALTEST(c_runtime_vsprintf_test13_paltest_vsprintf_test13, "c_runtime/vsprintf/test13/paltest_vsprintf_test13")
{
    int neg = -42;
    int pos = 0x1234AB;
    INT64 l = I64(0x1234567887654321);
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoNumTest("foo %X", pos, "foo 1234AB");
    DoNumTest("foo %lX", pos, "foo 1234AB");
    DoNumTest("foo %hX", pos, "foo 34AB");
    DoNumTest("foo %LX", pos, "foo 1234AB");
    DoI64Test("foo %I64X", l, "0x1234567887654321",
              "foo 1234567887654321");
    DoNumTest("foo %7X", pos, "foo  1234AB");
    DoNumTest("foo %-7X", pos, "foo 1234AB ");
    DoNumTest("foo %.1X", pos, "foo 1234AB");
    DoNumTest("foo %.7X", pos, "foo 01234AB");
    DoNumTest("foo %07X", pos, "foo 01234AB");
    DoNumTest("foo %#X", pos, "foo 0X1234AB");
    DoNumTest("foo %+X", pos, "foo 1234AB");
    DoNumTest("foo % X", pos, "foo 1234AB");
    DoNumTest("foo %+X", neg, "foo FFFFFFD6");
    DoNumTest("foo % X", neg, "foo FFFFFFD6");

    PAL_Terminate();
    return PASS;
}
