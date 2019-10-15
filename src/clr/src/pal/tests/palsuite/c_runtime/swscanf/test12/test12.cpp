// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test12.c
**
** Purpose: Tests swscanf with wide strings
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swscanf.h"

int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoStrTest(convert("foo bar"), convert("foo %S"), "bar");
    DoStrTest(convert("foo bar"), convert("foo %2S"), "ba");
    DoStrTest(convert("foo bar"), convert("foo %hS"), "bar");
    DoWStrTest(convert("foo bar"), convert("foo %lS"), convert("bar"));
    DoStrTest(convert("foo bar"), convert("foo %LS"), "bar");
    DoStrTest(convert("foo bar"), convert("foo %I64S"), "bar");

    PAL_Terminate();
    return PASS;
}
