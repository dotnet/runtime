// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test3.c
**
** Purpose:   Test #3 for the _vsnwprintf_s function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnwprintf_s.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */


PALTEST(c_runtime__vsnwprintf_s_test3_paltest_vsnwprintf_test3, "c_runtime/_vsnwprintf_s/test3/paltest_vsnwprintf_test3")
{
    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);

    DoStrTest(convert("foo %S"), "bar", convert("foo bar"));
    DoStrTest(convert("foo %hS"), "bar", convert("foo bar"));
    DoWStrTest(convert("foo %lS"), convert("bar"), convert("foo bar"));
    DoWStrTest(convert("foo %wS"), convert("bar"), convert("foo bar"));
    DoStrTest(convert("foo %LS"), "bar", convert("foo bar"));
    DoStrTest(convert("foo %I64S"), "bar", convert("foo bar"));
    DoStrTest(convert("foo %5S"), "bar", convert("foo   bar"));
    DoStrTest(convert("foo %.2S"), "bar", convert("foo ba"));
    DoStrTest(convert("foo %5.2S"), "bar", convert("foo    ba"));
    DoStrTest(convert("foo %-5S"), "bar", convert("foo bar  "));
    DoStrTest(convert("foo %05S"), "bar", convert("foo 00bar"));
    DoStrTest(convert("foo %S"), NULL, convert("foo (null)"));
    DoStrTest(convert("foo %hS"), NULL, convert("foo (null)"));
    DoWStrTest(convert("foo %lS"), NULL, convert("foo (null)"));
    DoWStrTest(convert("foo %wS"), NULL, convert("foo (null)"));
    DoStrTest(convert("foo %LS"), NULL, convert("foo (null)"));
    DoStrTest(convert("foo %I64S"), NULL, convert("foo (null)"));

    PAL_Terminate();
    return PASS;
}
