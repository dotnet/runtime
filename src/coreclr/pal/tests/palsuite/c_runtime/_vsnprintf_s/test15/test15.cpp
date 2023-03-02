// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test15.c
**
** Purpose:   Test #15 for the _vsnprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnprintf_s.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

PALTEST(c_runtime__vsnprintf_s_test15_paltest_vsnprintf_test15, "c_runtime/_vsnprintf_s/test15/paltest_vsnprintf_test15")
{
    double val = 256.0;
    double neg = -256.0;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoDoubleTest("foo %E", val,  "foo 2.560000E+002", "foo 2.560000E+02");
    DoDoubleTest("foo %E", neg,  "foo -2.560000E+002", "foo -2.560000E+02");


    PAL_Terminate();
    return PASS;
}
