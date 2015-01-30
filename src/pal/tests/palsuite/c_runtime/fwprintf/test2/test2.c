//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:      test2.c
**
** Purpose:     Tests the string specifier (%s).
**              This test is modeled after the sprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fwprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

int __cdecl main(int argc, char *argv[])
{

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoWStrTest(convert("foo %s"), convert("bar"), "foo bar");
    DoStrTest(convert("foo %hs"), "bar", "foo bar");
    DoWStrTest(convert("foo %ls"), convert("bar"), "foo bar");
    DoWStrTest(convert("foo %ws"), convert("bar"), "foo bar");
    DoWStrTest(convert("foo %Ls"), convert("bar"), "foo bar");
    DoWStrTest(convert("foo %I64s"), convert("bar"), "foo bar");
    DoWStrTest(convert("foo %5s"), convert("bar"), "foo   bar");
    DoWStrTest(convert("foo %.2s"), convert("bar"), "foo ba");
    DoWStrTest(convert("foo %5.2s"), convert("bar"), "foo    ba");
    DoWStrTest(convert("foo %-5s"), convert("bar"), "foo bar  ");
    DoWStrTest(convert("foo %05s"), convert("bar"), "foo 00bar");

    PAL_Terminate();
    return PASS;    
}

