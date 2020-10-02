// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      test2.c (fprintf)
**
** Purpose:     Tests the string specifier (%s).
**              This test is modeled after the fprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

PALTEST(c_runtime_fprintf_test2_paltest_fprintf_test2, "c_runtime/fprintf/test2/paltest_fprintf_test2")
{

    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);

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
    DoWStrTest("foo %ls", NULL, "foo (null)");
    DoWStrTest("foo %ws", NULL, "foo (null)");
    DoStrTest("foo %Ls", NULL, "foo (null)");
    DoStrTest("foo %I64s", NULL, "foo (null)");

    PAL_Terminate();
    return PASS;    
}

