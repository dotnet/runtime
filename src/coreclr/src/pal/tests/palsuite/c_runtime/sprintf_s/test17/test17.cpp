// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test17.c
**
** Purpose: Test #17 for the sprintf_s function. Tests the lowercase
**          shorthand notation double specifier (%g)
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sprintf_s.h"

/* 
 * Depends on memcmp and strlen
 */

PALTEST(c_runtime_sprintf_s_test17_paltest_sprintf_test17, "c_runtime/sprintf_s/test17/paltest_sprintf_test17")
{
    double val = 2560.001;
    double neg = -2560.001;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoDoubleTest("foo %g", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %lg", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %hg", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %Lg", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %I64g", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %5g", val,  "foo  2560", "foo  2560");
    DoDoubleTest("foo %-5g", val,  "foo 2560 ", "foo 2560 ");
    DoDoubleTest("foo %.1g", val,  "foo 3e+003", "foo 3e+03");
    DoDoubleTest("foo %.2g", val,  "foo 2.6e+003", "foo 2.6e+03");
    DoDoubleTest("foo %.12g", val,  "foo 2560.001", "foo 2560.001");
    DoDoubleTest("foo %06g", val,  "foo 002560", "foo 002560");
    DoDoubleTest("foo %#g", val,  "foo 2560.00", "foo 2560.00");
    DoDoubleTest("foo %+g", val,  "foo +2560", "foo +2560");
    DoDoubleTest("foo % g", val,  "foo  2560", "foo  2560");
    DoDoubleTest("foo %+g", neg,  "foo -2560", "foo -2560");
    DoDoubleTest("foo % g", neg,  "foo -2560", "foo -2560");

    PAL_Terminate();
    return PASS;
}
