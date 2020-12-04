// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test16.c
**
** Purpose:Tests swprintf with decimal point format doubles 
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swprintf.h"

/*
 * Uses memcmp & wcslen
 */

PALTEST(c_runtime_swprintf_test16_paltest_swprintf_test16, "c_runtime/swprintf/test16/paltest_swprintf_test16")
{
    double val = 2560.001;
    double neg = -2560.001;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoDoubleTest(convert("foo %f"), val,  convert("foo 2560.001000"),
                 convert("foo 2560.001000"));
    DoDoubleTest(convert("foo %lf"), val,  convert("foo 2560.001000"),
                 convert("foo 2560.001000"));
    DoDoubleTest(convert("foo %hf"), val,  convert("foo 2560.001000"),
                 convert("foo 2560.001000"));
    DoDoubleTest(convert("foo %Lf"), val,  convert("foo 2560.001000"),
                 convert("foo 2560.001000"));
    DoDoubleTest(convert("foo %I64f"), val,  convert("foo 2560.001000"),
                 convert("foo 2560.001000"));
    DoDoubleTest(convert("foo %12f"), val,  convert("foo  2560.001000"),
                 convert("foo  2560.001000"));
    DoDoubleTest(convert("foo %-12f"), val,  convert("foo 2560.001000 "),
                 convert("foo 2560.001000 "));
    DoDoubleTest(convert("foo %.1f"), val,  convert("foo 2560.0"),
                 convert("foo 2560.0"));
    DoDoubleTest(convert("foo %.8f"), val,  convert("foo 2560.00100000"),
                 convert("foo 2560.00100000"));
    DoDoubleTest(convert("foo %012f"), val,  convert("foo 02560.001000"),
                 convert("foo 02560.001000"));
    DoDoubleTest(convert("foo %#f"), val,  convert("foo 2560.001000"),
                 convert("foo 2560.001000"));
    DoDoubleTest(convert("foo %+f"), val,  convert("foo +2560.001000"),
                 convert("foo +2560.001000"));
    DoDoubleTest(convert("foo % f"), val,  convert("foo  2560.001000"),
                 convert("foo  2560.001000"));
    DoDoubleTest(convert("foo %+f"), neg,  convert("foo -2560.001000"),
                 convert("foo -2560.001000"));
    DoDoubleTest(convert("foo % f"), neg,  convert("foo -2560.001000"),
                 convert("foo -2560.001000"));

    PAL_Terminate();
    return PASS;
}
