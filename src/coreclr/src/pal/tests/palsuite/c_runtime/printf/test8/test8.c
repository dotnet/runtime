//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test8.c
**
** Purpose: Test #8 for the printf function. Tests the decimal
**          specifier (%d).
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../printf.h"



int __cdecl main(int argc, char *argv[])
{
    int neg = -42;
    int pos = 42;
    INT64 l = 42;
    
    if (PAL_Initialize(argc, argv))
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

