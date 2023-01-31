// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test11.c
**
** Purpose:   Test #11 for the _vsnprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnprintf_s.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

PALTEST(c_runtime__vsnprintf_s_test11_paltest_vsnprintf_test11, "c_runtime/_vsnprintf_s/test11/paltest_vsnprintf_test11")
{
    int neg = -42;
    int pos = 42;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoNumTest("foo %u", pos, "foo 42");
    DoNumTest("foo %u", neg, "foo 4294967254");

    PAL_Terminate();
    return PASS;
}
