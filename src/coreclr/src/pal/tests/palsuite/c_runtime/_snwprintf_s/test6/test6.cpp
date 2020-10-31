// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test6.c
**
** Purpose: Tests swprintf_s with characters
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../_snwprintf_s.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */

PALTEST(c_runtime__snwprintf_s_test6_paltest_snwprintf_test6, "c_runtime/_snwprintf_s/test6/paltest_snwprintf_test6")
{
    WCHAR wc = (WCHAR) 'c';
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoWCharTest(convert("foo %c"), wc, convert("foo c"));
    DoCharTest(convert("foo %hc"), 'b', convert("foo b"));
    DoWCharTest(convert("foo %lc"), wc, convert("foo c"));
    DoWCharTest(convert("foo %Lc"), wc, convert("foo c"));
    DoWCharTest(convert("foo %I64c"), wc, convert("foo c"));
    DoWCharTest(convert("foo %5c"), wc, convert("foo     c"));
    DoWCharTest(convert("foo %.0c"), wc, convert("foo c"));
    DoWCharTest(convert("foo %-5c"), wc, convert("foo c    "));
    DoWCharTest(convert("foo %05c"), wc, convert("foo 0000c"));
    DoWCharTest(convert("foo % c"), wc, convert("foo c"));
    DoWCharTest(convert("foo %#c"), wc, convert("foo c"));

    PAL_Terminate();
    return PASS;
}
