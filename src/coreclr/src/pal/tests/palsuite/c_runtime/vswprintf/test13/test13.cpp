// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test13.c
**
** Purpose:   Test #13 for the vswprintf function.
**
**
**===================================================================*/
 
#include <palsuite.h>
#include "../vswprintf.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */

int __cdecl main(int argc, char *argv[])
{
    int neg = -42;
    int pos = 0x1234ab;
    INT64 l = I64(0x1234567887654321);

    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);

    DoNumTest(convert("foo %X"), pos, convert("foo 1234AB"));
    DoNumTest(convert("foo %lX"), pos, convert("foo 1234AB"));
    DoNumTest(convert("foo %hX"), pos, convert("foo 34AB"));
    DoNumTest(convert("foo %LX"), pos, convert("foo 1234AB"));
    DoI64NumTest(convert("foo %I64X"), l, "0x1234567887654321", 
        convert("foo 1234567887654321"));
    DoNumTest(convert("foo %7X"), pos, convert("foo  1234AB"));
    DoNumTest(convert("foo %-7X"), pos, convert("foo 1234AB "));
    DoNumTest(convert("foo %.1X"), pos, convert("foo 1234AB"));
    DoNumTest(convert("foo %.7X"), pos, convert("foo 01234AB"));
    DoNumTest(convert("foo %07X"), pos, convert("foo 01234AB"));
    DoNumTest(convert("foo %#X"), pos, convert("foo 0X1234AB"));
    DoNumTest(convert("foo %+X"), pos, convert("foo 1234AB"));
    DoNumTest(convert("foo % X"), pos, convert("foo 1234AB"));
    DoNumTest(convert("foo %+X"), neg, convert("foo FFFFFFD6"));
    DoNumTest(convert("foo % X"), neg, convert("foo FFFFFFD6"));

    PAL_Terminate();
    return PASS;
}
