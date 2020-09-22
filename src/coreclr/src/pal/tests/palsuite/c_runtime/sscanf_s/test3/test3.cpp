// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test3.c
**
** Purpose: Tests sscanf_s with bracketed set strings
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"

PALTEST(c_runtime_sscanf_s_test3_paltest_sscanf_test3, "c_runtime/sscanf_s/test3/paltest_sscanf_test3")
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoStrTest("bar1", "%[a-z]", "bar");
    DoStrTest("bar1", "%[z-a]", "bar");
    DoStrTest("bar1", "%[ab]", "ba");
    DoStrTest("bar1", "%[ar1b]", "bar1");
    DoStrTest("bar1", "%[^4]", "bar1");
    DoStrTest("bar1", "%[^4a]", "b");

    PAL_Terminate();
    return PASS;
}
