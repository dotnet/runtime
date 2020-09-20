// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test8.c
**
** Purpose:Tests sscanf_s with unsigned number 
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"

PALTEST(c_runtime_sscanf_s_test8_paltest_sscanf_test8, "c_runtime/sscanf_s/test8/paltest_sscanf_test8")
{
    int n65535 = 65535; /* Walkaround compiler strictness */

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoNumTest("1234d", "%u", 1234);
    DoNumTest("1234d", "%2u", 12);
    DoNumTest("-1", "%u", -1);
    DoNumTest("0x1234", "%u", 0);
    DoNumTest("012", "%u", 12);
    DoShortNumTest("-1", "%hu", n65535);
    DoShortNumTest("65536", "%hu", 0);
    DoNumTest("-1", "%lu", -1);
    DoNumTest("65536", "%lu", 65536);
    DoNumTest("-1", "%Lu", -1);
    DoNumTest("65536", "%Lu", 65536);
    DoI64NumTest("4294967296", "%I64u", I64(4294967296));

    PAL_Terminate();
    return PASS;
}
