//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Test to see if sscanf handles whitespace correctly
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf.h"


/* 
 * Tests out how it handles whitespace. Seems to accept anything that qualifies 
 * as isspace (space, tab, vertical tab, line feed, carriage return and form 
 * feed), even if it says it only wants spaces tabs and newlines. 
 */

int __cdecl main(int argc, char *argv[])
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
