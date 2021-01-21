// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test12.c
**
** Purpose:  Tests sscanf_s with wide strings
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"

PALTEST(c_runtime_sscanf_s_test12_paltest_sscanf_test12, "c_runtime/sscanf_s/test12/paltest_sscanf_test12")
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoWStrTest("foo bar", "foo %S", convert("bar"));
    DoWStrTest("foo bar", "foo %2S", convert("ba"));
    DoStrTest("foo bar", "foo %hS", "bar");
    DoWStrTest("foo bar", "foo %lS", convert("bar"));
    DoWStrTest("foo bar", "foo %LS", convert("bar"));
    DoWStrTest("foo bar", "foo %I64S", convert("bar"));

    PAL_Terminate();
    return PASS;
}
