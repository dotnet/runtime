// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test18.c
**
** Purpose: Test #18 for the vprintf function. Tests the uppercase
**          shorthand notation double specifier (%G)
**
**
**==========================================================================*/


#include <palsuite.h>
#include "../vprintf.h"



PALTEST(c_runtime_vprintf_test18_paltest_vprintf_test18, "c_runtime/vprintf/test18/paltest_vprintf_test18")
{
    double val = 2560.001;
    double neg = -2560.001;
    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    DoDoubleTest("foo %G", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %lG", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %hG", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %LG", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %I64G", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %5G", val,  "foo  2560", "foo  2560");
    DoDoubleTest("foo %-5G", val,  "foo 2560 ", "foo 2560 ");
    DoDoubleTest("foo %.1G", val,  "foo 3E+003", "foo 3E+03");
    DoDoubleTest("foo %.2G", val,  "foo 2.6E+003", "foo 2.6E+03");
    DoDoubleTest("foo %.12G", val,  "foo 2560.001", "foo 2560.001");
    DoDoubleTest("foo %06G", val,  "foo 002560", "foo 002560");
    DoDoubleTest("foo %#G", val,  "foo 2560.00", "foo 2560.00");
    DoDoubleTest("foo %+G", val,  "foo +2560", "foo +2560");
    DoDoubleTest("foo % G", val,  "foo  2560", "foo  2560");
    DoDoubleTest("foo %+G", neg,  "foo -2560", "foo -2560");
    DoDoubleTest("foo % G", neg,  "foo -2560", "foo -2560");

    PAL_Terminate();
    return PASS;
}
