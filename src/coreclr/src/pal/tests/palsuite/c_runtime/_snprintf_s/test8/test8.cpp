// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test8.c
**
** Purpose: Tests sprintf_s with decimal numbers
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
    int neg = -42;
    int pos = 42;
    INT64 l = 42;
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoNumTest("foo %d", pos, "foo 42");
    DoNumTest("foo %ld", 0xFFFF, "foo 65535");
    DoNumTest("foo %hd", 0xFFFF, "foo -1");
    DoNumTest("foo %Ld", pos, "foo 42");
    DoI64Test("foo %I64d", l, "42", "foo 42");
    DoNumTest("foo %3d", pos, "foo  42");
    DoNumTest("foo %-3d", pos, "foo 42 ");
    DoNumTest("foo %.1d", pos, "foo 42");
    DoNumTest("foo %.3d", pos, "foo 042");
    DoNumTest("foo %03d", pos, "foo 042");
    DoNumTest("foo %#d", pos, "foo 42");
    DoNumTest("foo %+d", pos, "foo +42");
    DoNumTest("foo % d", pos, "foo  42");
    DoNumTest("foo %+d", neg, "foo -42");
    DoNumTest("foo % d", neg, "foo -42");

  
    PAL_Terminate();
    return PASS;
}

