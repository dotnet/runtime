// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test6.c
**
** Purpose: Tests sscanf_s with octal numbers
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"

PALTEST(c_runtime_sscanf_s_test6_paltest_sscanf_test6, "c_runtime/sscanf_s/test6/paltest_sscanf_test6")
{
    int n65535 = 65535; /* Walkaround compiler strictness */

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoNumTest("1234d", "%o", 668);
    DoNumTest("1234d", "%2o", 10);
    DoNumTest("-1", "%o", -1);
    DoNumTest("0x1234", "%o", 0);
    DoNumTest("012", "%o", 10);
    DoShortNumTest("-1", "%ho", n65535);
    DoShortNumTest("200000", "%ho", 0);
    DoNumTest("-1", "%lo", -1);
    DoNumTest("200000", "%lo", 65536);
    DoNumTest("-1", "%Lo", -1);
    DoNumTest("200000", "%Lo", 65536);
    DoI64NumTest("40000000000", "%I64o", I64(4294967296));

    PAL_Terminate();
    return PASS;
}
