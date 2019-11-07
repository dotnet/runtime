// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Tests swprintf with strings
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

    DoWStrTest(convert("foo %s"), convert("bar"), convert("foo bar"));
    DoStrTest(convert("foo %hs"), "bar", convert("foo bar"));
    DoWStrTest(convert("foo %ws"), convert("bar"), convert("foo bar"));
    DoWStrTest(convert("foo %ls"), convert("bar"), convert("foo bar"));
    DoWStrTest(convert("foo %Ls"), convert("bar"), convert("foo bar"));
    DoWStrTest(convert("foo %I64s"), convert("bar"), convert("foo bar"));
    DoWStrTest(convert("foo %5s"), convert("bar"), convert("foo   bar"));
    DoWStrTest(convert("foo %.2s"), convert("bar"), convert("foo ba"));
    DoWStrTest(convert("foo %5.2s"), convert("bar"), convert("foo    ba"));
    DoWStrTest(convert("foo %-5s"), convert("bar"), convert("foo bar  "));
    DoWStrTest(convert("foo %05s"), convert("bar"), convert("foo 00bar"));
    DoWStrTest(convert("foo %s"), NULL, convert("foo (null)"));
    DoStrTest(convert("foo %hs"), NULL, convert("foo (null)"));
    DoWStrTest(convert("foo %ls"), NULL, convert("foo (null)"));
    DoWStrTest(convert("foo %ws"), NULL, convert("foo (null)"));
    DoWStrTest(convert("foo %Ls"), NULL, convert("foo (null)"));
    DoWStrTest(convert("foo %I64s"), NULL, convert("foo (null)"));

    PAL_Terminate();
    return PASS;
}
