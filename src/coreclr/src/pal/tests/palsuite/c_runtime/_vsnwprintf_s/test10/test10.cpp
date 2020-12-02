// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test10.c
**
** Purpose:   Test #10 for the _vsnwprintf_s function.
**
**
**===================================================================*/
 
#include <palsuite.h>
#include "../_vsnwprintf_s.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */

PALTEST(c_runtime__vsnwprintf_s_test10_paltest_vsnwprintf_test10, "c_runtime/_vsnwprintf_s/test10/paltest_vsnwprintf_test10")
{
    int neg = -42;
    int pos = 42;
    INT64 l = 42;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoNumTest(convert("foo %o"), pos, convert("foo 52"));
    DoNumTest(convert("foo %lo"), 0xFFFF, convert("foo 177777"));
    DoNumTest(convert("foo %ho"), 0xFFFF, convert("foo 177777"));
    DoNumTest(convert("foo %Lo"), pos, convert("foo 52"));
    DoI64NumTest(convert("foo %I64o"), l, "42", convert("foo 52"));
    DoNumTest(convert("foo %3o"), pos, convert("foo  52"));
    DoNumTest(convert("foo %-3o"), pos, convert("foo 52 "));
    DoNumTest(convert("foo %.1o"), pos, convert("foo 52"));
    DoNumTest(convert("foo %.3o"), pos, convert("foo 052"));
    DoNumTest(convert("foo %03o"), pos, convert("foo 052"));
    DoNumTest(convert("foo %#o"), pos, convert("foo 052"));
    DoNumTest(convert("foo %+o"), pos, convert("foo 52"));
    DoNumTest(convert("foo % o"), pos, convert("foo 52"));
    DoNumTest(convert("foo %+o"), neg, convert("foo 37777777726"));
    DoNumTest(convert("foo % o"), neg, convert("foo 37777777726"));


    PAL_Terminate();
    return PASS;
}
