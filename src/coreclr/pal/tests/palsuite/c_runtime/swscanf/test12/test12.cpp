// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test12.c
**
** Purpose: Tests swscanf with wide strings
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swscanf.h"

PALTEST(c_runtime_swscanf_test12_paltest_swscanf_test12, "c_runtime/swscanf/test12/paltest_swscanf_test12")
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoStrTest(convert("foo bar"), convert("foo %S"), "bar");
    DoStrTest(convert("foo bar"), convert("foo %2S"), "ba");
    DoStrTest(convert("foo bar"), convert("foo %hS"), "bar");
    DoWStrTest(convert("foo bar"), convert("foo %lS"), convert("bar"));
    DoStrTest(convert("foo bar"), convert("foo %LS"), "bar");
    DoStrTest(convert("foo bar"), convert("foo %I64S"), "bar");

    PAL_Terminate();
    return PASS;
}
