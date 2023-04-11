// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test18.c
**
** Purpose:   Test #18 for the _vsnprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnprintf_s.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

PALTEST(c_runtime__vsnprintf_s_test18_paltest_vsnprintf_test18, "c_runtime/_vsnprintf_s/test18/paltest_vsnprintf_test18")
{
    double val = 2560.001;
    double neg = -2560.001;

    if (PAL_Initialize(argc, argv) != 0)
    {
         return(FAIL);
    }

    DoDoubleTest("foo %G", val,  "foo 2560", "foo 2560");
    DoDoubleTest("foo %G", neg,  "foo -2560", "foo -2560");

    PAL_Terminate();
    return PASS;
}
