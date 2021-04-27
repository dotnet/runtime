// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test9.c
**
** Purpose:   Test #9 for the _vsnwprintf_s function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnwprintf_s.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */

PALTEST(c_runtime__vsnwprintf_s_test9_paltest_vsnwprintf_test9, "c_runtime/_vsnwprintf_s/test9/paltest_vsnwprintf_test9")
{
    int neg = -42;
    int pos = 42;
    INT64 l = 42;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoNumTest(convert("foo %i"), pos, convert("foo 42"));
    DoNumTest(convert("foo %li"), 0xFFFF, convert("foo 65535"));
    DoNumTest(convert("foo %hi"), 0xFFFF, convert("foo -1"));
    DoNumTest(convert("foo %Li"), pos, convert("foo 42"));
    DoI64NumTest(convert("foo %I64i"), l, "42", convert("foo 42"));
    DoNumTest(convert("foo %3i"), pos, convert("foo  42"));
    DoNumTest(convert("foo %-3i"), pos, convert("foo 42 "));
    DoNumTest(convert("foo %.1i"), pos, convert("foo 42"));
    DoNumTest(convert("foo %.3i"), pos, convert("foo 042"));
    DoNumTest(convert("foo %03i"), pos, convert("foo 042"));
    DoNumTest(convert("foo %#i"), pos, convert("foo 42"));
    DoNumTest(convert("foo %+i"), pos, convert("foo +42"));
    DoNumTest(convert("foo % i"), pos, convert("foo  42"));
    DoNumTest(convert("foo %+i"), neg, convert("foo -42"));
    DoNumTest(convert("foo % i"), neg, convert("foo -42"));

    PAL_Terminate();
    return PASS;
}
