// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test2.c
**
** Purpose:Tests sprintf_s with strings
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../_snprintf_s.h"
/*
 * Notes: memcmp is used, as is strlen.
 */

int __cdecl main(int argc, char *argv[])
{

    if (PAL_Initialize(argc, argv) != 0)
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
    DoStrTest("foo %s", NULL, "foo (null)");
    DoStrTest("foo %hs", NULL, "foo (null)");
    DoWStrTest("foo %ls", NULL, "foo (null)");
    DoWStrTest("foo %ws", NULL, "foo (null)");
    DoStrTest("foo %Ls", NULL, "foo (null)");
    DoStrTest("foo %I64s", NULL, "foo (null)");

    PAL_Terminate();
    return PASS;
}
