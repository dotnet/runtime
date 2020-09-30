// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test18.c
**
** Purpose: Tests swprintf_s with compact format doubles (uppercase)
**
**
**==========================================================================*/




#include <palsuite.h>
#include "../_snwprintf_s.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */

PALTEST(c_runtime__snwprintf_s_test18_paltest_snwprintf_test18, "c_runtime/_snwprintf_s/test18/paltest_snwprintf_test18")
{
    double val = 2560.001;
    double neg = -2560.001;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoDoubleTest(convert("foo %G"), val, convert("foo 2560"),
                 convert("foo 2560"));
    DoDoubleTest(convert("foo %lG"), val, convert("foo 2560"),
                 convert("foo 2560"));
    DoDoubleTest(convert("foo %hG"), val, convert("foo 2560"),
                 convert("foo 2560"));
    DoDoubleTest(convert("foo %LG"), val, convert("foo 2560"),
                 convert("foo 2560"));
    DoDoubleTest(convert("foo %I64G"), val, convert("foo 2560"),
                 convert("foo 2560"));
    DoDoubleTest(convert("foo %5G"), val, convert("foo  2560"),
                 convert("foo  2560"));
    DoDoubleTest(convert("foo %-5G"), val, convert("foo 2560 "),
                 convert("foo 2560 "));
    DoDoubleTest(convert("foo %.1G"), val, convert("foo 3E+003"),
                 convert("foo 3E+03"));
    DoDoubleTest(convert("foo %.2G"), val, convert("foo 2.6E+003"),
                 convert("foo 2.6E+03"));
    DoDoubleTest(convert("foo %.12G"), val, convert("foo 2560.001"),
                 convert("foo 2560.001"));
    DoDoubleTest(convert("foo %06G"), val, convert("foo 002560"),
                 convert("foo 002560"));
    DoDoubleTest(convert("foo %#G"), val, convert("foo 2560.00"),
                 convert("foo 2560.00"));
    DoDoubleTest(convert("foo %+G"), val, convert("foo +2560"),
                 convert("foo +2560"));
    DoDoubleTest(convert("foo % G"), val, convert("foo  2560"),
                 convert("foo  2560"));
    DoDoubleTest(convert("foo %+G"), neg, convert("foo -2560"),
                 convert("foo -2560"));
    DoDoubleTest(convert("foo % G"), neg, convert("foo -2560"),
                 convert("foo -2560"));

    PAL_Terminate();
    return PASS;
}
