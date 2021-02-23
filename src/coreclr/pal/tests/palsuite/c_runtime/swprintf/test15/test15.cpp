// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test15.c
**
** Purpose:Tests swprintf with exponential format doubles (uppercase)
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swprintf.h"

/*
 * Uses memcmp & wcslen
 */

PALTEST(c_runtime_swprintf_test15_paltest_swprintf_test15, "c_runtime/swprintf/test15/paltest_swprintf_test15")
{
    double val = 256.0;
    double neg = -256.0;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoDoubleTest(convert("foo %E"), val,  convert("foo 2.560000E+002"),
                 convert("foo 2.560000E+02"));
    DoDoubleTest(convert("foo %lE"), val,  convert("foo 2.560000E+002"),
                 convert("foo 2.560000E+02"));
    DoDoubleTest(convert("foo %hE"), val,  convert("foo 2.560000E+002"),
                 convert("foo 2.560000E+02"));
    DoDoubleTest(convert("foo %LE"), val,  convert("foo 2.560000E+002"),
                 convert("foo 2.560000E+02"));
    DoDoubleTest(convert("foo %I64E"), val,  convert("foo 2.560000E+002"),
                 convert("foo 2.560000E+02"));
    DoDoubleTest(convert("foo %14E"), val,  convert("foo  2.560000E+002"),
                 convert("foo   2.560000E+02"));
    DoDoubleTest(convert("foo %-14E"), val,  convert("foo 2.560000E+002 "),
                 convert("foo 2.560000E+02  "));
    DoDoubleTest(convert("foo %.1E"), val,  convert("foo 2.6E+002"),
                 convert("foo 2.6E+02"));
    DoDoubleTest(convert("foo %.8E"), val,  convert("foo 2.56000000E+002"),
                 convert("foo 2.56000000E+02"));
    DoDoubleTest(convert("foo %014E"), val,  convert("foo 02.560000E+002"),
                 convert("foo 002.560000E+02"));
    DoDoubleTest(convert("foo %#E"), val,  convert("foo 2.560000E+002"),
                 convert("foo 2.560000E+02"));
    DoDoubleTest(convert("foo %+E"), val,  convert("foo +2.560000E+002"),
                 convert("foo +2.560000E+02"));
    DoDoubleTest(convert("foo % E"), val,  convert("foo  2.560000E+002"),
                 convert("foo  2.560000E+02"));
    DoDoubleTest(convert("foo %+E"), neg,  convert("foo -2.560000E+002"),
                 convert("foo -2.560000E+02"));
    DoDoubleTest(convert("foo % E"), neg,  convert("foo -2.560000E+002"),
                 convert("foo -2.560000E+02"));

    PAL_Terminate();
    return PASS;
}
