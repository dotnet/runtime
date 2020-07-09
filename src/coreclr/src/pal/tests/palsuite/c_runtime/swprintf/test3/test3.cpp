// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test3.c
**
** Purpose: Tests swprintf with wide strings
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swprintf.h"

/*
 * Uses memcmp & wcslen
 */

int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    DoStrTest(convert("foo %S"), "bar", convert("foo bar"));
    DoStrTest(convert("foo %hS"), "bar", convert("foo bar"));
    DoWStrTest(convert("foo %lS"), convert("bar"), convert("foo bar"));
    DoWStrTest(convert("foo %wS"), convert("bar"), convert("foo bar"));
    DoStrTest(convert("foo %LS"), "bar", convert("foo bar"));
    DoStrTest(convert("foo %I64S"), "bar", convert("foo bar"));
    DoStrTest(convert("foo %5S"), "bar", convert("foo   bar"));
    DoStrTest(convert("foo %.2S"), "bar", convert("foo ba"));
    DoStrTest(convert("foo %5.2S"),"bar", convert("foo    ba"));
    DoStrTest(convert("foo %-5S"), "bar", convert("foo bar  "));
    DoStrTest(convert("foo %05S"), "bar", convert("foo 00bar"));
    DoStrTest(convert("foo %S"), NULL, convert("foo (null)"));
    DoStrTest(convert("foo %hS"), NULL, convert("foo (null)"));
    DoWStrTest(convert("foo %lS"), NULL, convert("foo (null)"));
    DoWStrTest(convert("foo %wS"), NULL, convert("foo (null)"));
    DoStrTest(convert("foo %LS"), NULL, convert("foo (null)"));
    DoStrTest(convert("foo %I64S"), NULL, convert("foo (null)"));
    PAL_Terminate();
    return PASS;
}
