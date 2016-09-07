// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Test #2 for the vprintf function. Tests the string specifier
**          (%s).
**
**
**==========================================================================*/


#include <palsuite.h>
#include "../vprintf.h"



int __cdecl main(int argc, char *argv[])
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

    PAL_Terminate();
    return PASS;    
}

