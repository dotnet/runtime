// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test12.c
**
** Purpose:   Test #12 for the _vsnprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnprintf_s.h"

/*
 * Notes: memcmp is used, as is strlen.
 */


PALTEST(c_runtime__vsnprintf_s_test12_paltest_vsnprintf_test12, "c_runtime/_vsnprintf_s/test12/paltest_vsnprintf_test12")
{
    int neg = -42;
    int pos = 0x1234ab;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return (FAIL);
    }

    DoNumTest("foo %x", pos, "foo 1234ab");
    DoNumTest("foo %x", neg, "foo ffffffd6");

    PAL_Terminate();
    return PASS;
}
