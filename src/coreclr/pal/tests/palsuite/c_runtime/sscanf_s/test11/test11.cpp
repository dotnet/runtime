// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test11.c
**
** Purpose: Tests sscanf_s with strings
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"

PALTEST(c_runtime_sscanf_s_test11_paltest_sscanf_test11, "c_runtime/sscanf_s/test11/paltest_sscanf_test11")
{

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoStrTest("foo bar", "foo %s", "bar");
    DoStrTest("foo bar", "foo %2s", "ba");
    DoStrTest("foo bar", "foo %hs", "bar");
    DoWStrTest("foo bar", "foo %ls", convert("bar"));
    DoStrTest("foo bar", "foo %Ls", "bar");
    DoStrTest("foo bar", "foo %I64s", "bar");

    PAL_Terminate();
    return PASS;
}
