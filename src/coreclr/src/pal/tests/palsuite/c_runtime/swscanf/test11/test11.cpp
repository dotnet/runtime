// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test11.c
**
** Purpose: Tests swscanf with strings
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swscanf.h"

PALTEST(c_runtime_swscanf_test11_paltest_swscanf_test11, "c_runtime/swscanf/test11/paltest_swscanf_test11")
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoWStrTest(convert("foo bar"), convert("foo %s"), convert("bar"));
    DoWStrTest(convert("foo bar"), convert("foo %2s"), convert("ba"));
    DoStrTest(convert("foo bar"), convert("foo %hs"), "bar");
    DoWStrTest(convert("foo bar"), convert("foo %ls"), convert("bar"));
    DoWStrTest(convert("foo bar"), convert("foo %Ls"), convert("bar"));
    DoWStrTest(convert("foo bar"), convert("foo %I64s"), convert("bar"));

    PAL_Terminate();
    return PASS;
}
