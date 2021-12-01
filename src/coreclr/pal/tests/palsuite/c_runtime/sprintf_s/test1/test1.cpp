// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test #1 for the sprintf_s function. A single, basic, test
**          case with no formatting.
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sprintf_s.h"

/*
 * Depends on memcmp and strlen
 */

PALTEST(c_runtime_sprintf_s_test1_paltest_sprintf_test1, "c_runtime/sprintf_s/test1/paltest_sprintf_test1")
{
    char checkstr[] = "hello world";
    char buf[256];

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    sprintf_s(buf, ARRAY_SIZE(buf), "hello world");

    if (memcmp(checkstr, buf, strlen(checkstr)+1) != 0)
    {
        Fail("ERROR: expected %s, got %s\n", checkstr, buf);
    }

    PAL_Terminate();
    return PASS;
}

