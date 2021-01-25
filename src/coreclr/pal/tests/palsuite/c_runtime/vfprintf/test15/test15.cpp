// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test15.c
**
** Purpose: Test #15 for the vfprintf function. Tests the uppercase
**          exponential notation double specifier (%E)
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../vfprintf.h"



PALTEST(c_runtime_vfprintf_test15_paltest_vfprintf_test15, "c_runtime/vfprintf/test15/paltest_vfprintf_test15")
{
    double val = 256.0;
    double neg = -256.0;
    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    DoDoubleTest("foo %E", val,  "foo 2.560000E+002", "foo 2.560000E+02");
    DoDoubleTest("foo %lE", val,  "foo 2.560000E+002", "foo 2.560000E+02");
    DoDoubleTest("foo %hE", val,  "foo 2.560000E+002", "foo 2.560000E+02");
    DoDoubleTest("foo %LE", val,  "foo 2.560000E+002", "foo 2.560000E+02");
    DoDoubleTest("foo %I64E", val,  "foo 2.560000E+002", "foo 2.560000E+02");
    DoDoubleTest("foo %14E", val,  "foo  2.560000E+002", "foo   2.560000E+02");
    DoDoubleTest("foo %-14E", val,  "foo 2.560000E+002 ", 
        "foo 2.560000E+02  ");
    DoDoubleTest("foo %.1E", val,  "foo 2.6E+002", "foo 2.6E+02");
    DoDoubleTest("foo %.8E", val,  "foo 2.56000000E+002", 
        "foo 2.56000000E+02");
    DoDoubleTest("foo %014E", val,  "foo 02.560000E+002", 
        "foo 002.560000E+02");
    DoDoubleTest("foo %#E", val,  "foo 2.560000E+002", "foo 2.560000E+02");
    DoDoubleTest("foo %+E", val,  "foo +2.560000E+002", "foo +2.560000E+02");
    DoDoubleTest("foo % E", val,  "foo  2.560000E+002", "foo  2.560000E+02");
    DoDoubleTest("foo %+E", neg,  "foo -2.560000E+002", "foo -2.560000E+02");
    DoDoubleTest("foo % E", neg,  "foo -2.560000E+002", "foo -2.560000E+02");

    PAL_Terminate();
    return PASS;
}
