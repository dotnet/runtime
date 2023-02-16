// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test8.c
**
** Purpose:   Test #8 for the _vsnprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnprintf_s.h"

/*
 * Notes: memcmp is used, as is strlen.
 */


PALTEST(c_runtime__vsnprintf_s_test8_paltest_vsnprintf_test8, "c_runtime/_vsnprintf_s/test8/paltest_vsnprintf_test8")
{
    int neg = -42;
    int pos = 42;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoNumTest("foo %d", pos, "foo 42");
    DoNumTest("foo %d", neg, "foo -42");


    PAL_Terminate();
    return PASS;
}
