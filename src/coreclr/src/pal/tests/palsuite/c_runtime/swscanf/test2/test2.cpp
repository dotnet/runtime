// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Test to see if swscanf handles whitespace correctly
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swscanf.h"

/* 
 * Tests out how it handles whitespace. Seems to accept anything that qualifies 
 * as isspace (space, tab, vertical tab, line feed, carriage return and form 
 * feed), even if it says it only wants spaces tabs and newlines. 
 */

PALTEST(c_runtime_swscanf_test2_paltest_swscanf_test2, "c_runtime/swscanf/test2/paltest_swscanf_test2")
{

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoStrTest(convert("foo bar"), convert("foo %S"), "bar");
    DoStrTest(convert("foo\tbar"), convert("foo %S"), "bar");
    DoStrTest(convert("foo\nbar"), convert("foo %S"), "bar");
    DoStrTest(convert("foo\rbar"), convert("foo %S"), "bar");
    DoStrTest(convert("foo\vbar"), convert("foo %S"), "bar");
    DoStrTest(convert("foo\fbar"), convert("foo %S"), "bar");
    DoStrTest(convert("foo \t\n\r\v\fbar"), convert("foo %S"), "bar");

    PAL_Terminate();
    return PASS;
}
