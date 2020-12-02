// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test7.c
**
** Purpose:  Tests sscanf_s with hex numbers (lowercase)
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"

PALTEST(c_runtime_sscanf_s_test7_paltest_sscanf_test7, "c_runtime/sscanf_s/test7/paltest_sscanf_test7")
{
    int n65535 = 65535; /* Walkaround compiler strictness */

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoNumTest("1234i", "%x", 0x1234);
    DoNumTest("1234i", "%2x", 0x12);
    DoNumTest("-1", "%x", -1);
    DoNumTest("0x1234", "%x", 0x1234);
    DoNumTest("012", "%x", 0x12);
    DoShortNumTest("-1", "%hx", n65535);
    DoShortNumTest("10000", "%hx", 0);
    DoNumTest("-1", "%lx", -1);
    DoNumTest("10000", "%lx", 65536);
    DoNumTest("-1", "%Lx", -1);
    DoNumTest("10000", "%Lx", 65536);
    DoI64NumTest("100000000", "%I64x", I64(4294967296));

    PAL_Terminate();
    return PASS;
}
