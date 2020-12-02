// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test4.c
**
** Purpose: Tests sscanf_s with decimal numbers
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"


PALTEST(c_runtime_sscanf_s_test4_paltest_sscanf_test4, "c_runtime/sscanf_s/test4/paltest_sscanf_test4")
{
    int n65535 = 65535; /* Walkaround compiler strictness */

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoNumTest("1234d", "%d", 1234);
    DoNumTest("1234d", "%2d", 12);
    DoNumTest("-1", "%d", -1);
    DoNumTest("0x1234", "%d", 0);
    DoNumTest("012", "%d", 12);
    DoShortNumTest("-1", "%hd", n65535);
    DoShortNumTest("65536", "%hd", 0);
    DoNumTest("-1", "%ld", -1);
    DoNumTest("65536", "%ld", 65536);
    DoNumTest("-1", "%Ld", -1);
    DoNumTest("65536", "%Ld", 65536);
    DoI64NumTest("4294967296", "%I64d", I64(4294967296));
    
    PAL_Terminate();
    return PASS;
}
