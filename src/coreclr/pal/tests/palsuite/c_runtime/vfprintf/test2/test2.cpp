// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Test #2 for the vfprintf function. Tests the string specifier
**          (%s).
**
**
**==========================================================================*/


#include <palsuite.h>
#include "../vfprintf.h"



PALTEST(c_runtime_vfprintf_test2_paltest_vfprintf_test2, "c_runtime/vfprintf/test2/paltest_vfprintf_test2")
{

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    DoStrTest("foo %s", "bar", "foo bar");
    DoStrTest("foo %hs", "bar", "foo bar");
    DoWStrTest("foo %ls", convert("bar"), "foo bar");
    DoWStrTest("foo %ws", convert("bar"), "foo bar");
    DoStrTest("foo %Ls", "bar", "foo bar");
    DoStrTest("foo %I64s", "bar", "foo bar");
    DoStrTest("foo %5s", "bar", "foo   bar");
    DoStrTest("foo %.2s", "bar", "foo ba");
    DoStrTest("foo %5.2s", "bar", "foo    ba");
    DoStrTest("foo %-5s", "bar", "foo bar  ");
    DoStrTest("foo %05s", "bar", "foo 00bar");
    DoStrTest("foo %s", NULL, "foo (null)");
    DoStrTest("foo %hs", NULL, "foo (null)");
    DoStrTest("foo %ls", NULL, "foo (null)");
    DoStrTest("foo %ws", NULL, "foo (null)");
    DoStrTest("foo %Ls", NULL, "foo (null)");
    DoStrTest("foo %I64s", NULL, "foo (null)");

    PAL_Terminate();
    return PASS;    
}

