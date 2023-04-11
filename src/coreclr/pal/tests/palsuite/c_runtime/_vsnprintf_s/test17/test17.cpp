// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test17.c
**
** Purpose:   Test #17 for the _vsnprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnprintf_s.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

PALTEST(c_runtime__vsnprintf_s_test17_paltest_vsnprintf_test17, "c_runtime/_vsnprintf_s/test17/paltest_vsnprintf_test17")
{
    double val = 2560.001;
    double neg = -2560.001;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoDoubleTest("foo %g", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %g", neg,  "foo -2560", "foo -2560");

    PAL_Terminate();
    return PASS;
}
