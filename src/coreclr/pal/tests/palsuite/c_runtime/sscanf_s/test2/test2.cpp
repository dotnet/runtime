// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Test to see if sscanf_s handles whitespace correctly
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"


/* 
 * Tests out how it handles whitespace. Seems to accept anything that qualifies 
 * as isspace (space, tab, vertical tab, line feed, carriage return and form 
 * feed), even if it says it only wants spaces tabs and newlines. 
 */

PALTEST(c_runtime_sscanf_s_test2_paltest_sscanf_test2, "c_runtime/sscanf_s/test2/paltest_sscanf_test2")
{

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoStrTest("foo bar", "foo %s", "bar");
    DoStrTest("foo\tbar", "foo %s", "bar");
    DoStrTest("foo\nbar", "foo %s", "bar");
    DoStrTest("foo\rbar", "foo %s", "bar");
    DoStrTest("foo\vbar", "foo %s", "bar");
    DoStrTest("foo\fbar", "foo %s", "bar");
    DoStrTest("foo \t\n\r\v\fbar", "foo %s", "bar");    

    PAL_Terminate();
    return PASS;
}
