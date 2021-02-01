// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test7.c
**
** Purpose: Tests swprintf_s with wide characters
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../_snwprintf_s.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */

PALTEST(c_runtime__snwprintf_s_test7_paltest_snwprintf_test7, "c_runtime/_snwprintf_s/test7/paltest_snwprintf_test7")
{
    WCHAR wc = (WCHAR) 'c';
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoCharTest(convert("foo %C"), 'b', convert("foo b"));
    DoWCharTest(convert("foo %hC"), wc, convert("foo c"));
    DoCharTest(convert("foo %lC"), 'b', convert("foo b"));
    DoCharTest(convert("foo %LC"), 'b', convert("foo b"));
    DoCharTest(convert("foo %I64C"), 'b', convert("foo b"));
    DoCharTest(convert("foo %5C"), 'b', convert("foo     b"));
    DoCharTest(convert("foo %.0C"), 'b', convert("foo b"));
    DoCharTest(convert("foo %-5C"), 'b', convert("foo b    "));
    DoCharTest(convert("foo %05C"), 'b', convert("foo 0000b"));
    DoCharTest(convert("foo % C"), 'b', convert("foo b"));
    DoCharTest(convert("foo %#C"), 'b', convert("foo b"));

    PAL_Terminate();
    return PASS;
}
