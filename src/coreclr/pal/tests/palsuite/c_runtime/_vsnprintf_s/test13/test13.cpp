// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test13.c
**
** Purpose:   Test #13 for the _vsnprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnprintf_s.h"

/*
 * Notes: memcmp is used, as is strlen.
 */


PALTEST(c_runtime__vsnprintf_s_test13_paltest_vsnprintf_test13, "c_runtime/_vsnprintf_s/test13/paltest_vsnprintf_test13")
{
    int neg = -42;
    int pos = 0x1234AB;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoNumTest("foo %X", pos, "foo 1234AB");
    DoNumTest("foo %X", neg, "foo FFFFFFD6");

    PAL_Terminate();
    return PASS;
}
