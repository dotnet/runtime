//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:    test3.c
**
** Purpose:   Test #3 for the vswprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../vswprintf.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */


int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv) != 0)
        return(FAIL);

    DoStrTest(convert("foo %S"), "bar", convert("foo bar"));
    DoStrTest(convert("foo %hS"), "bar", convert("foo bar"));
    DoWStrTest(convert("foo %lS"), convert("bar"), convert("foo bar"));
    DoWStrTest(convert("foo %wS"), convert("bar"), convert("foo bar"));
    DoStrTest(convert("foo %LS"), "bar", convert("foo bar"));
    DoStrTest(convert("foo %I64S"), "bar", convert("foo bar"));
    DoStrTest(convert("foo %5S"), "bar", convert("foo   bar"));
    DoStrTest(convert("foo %.2S"), "bar", convert("foo ba"));
    DoStrTest(convert("foo %5.2S"), "bar", convert("foo    ba"));
    DoStrTest(convert("foo %-5S"), "bar", convert("foo bar  "));
    DoStrTest(convert("foo %05S"), "bar", convert("foo 00bar"));

    PAL_Terminate();
    return PASS;
}
