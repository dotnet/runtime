//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test9.c
**
** Purpose: Test #9 for the printf function. Tests the integer
**          specifier (%i).
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


    DoNumTest("foo %i", pos, "foo 42");
    DoNumTest("foo %li", 0xFFFF, "foo 65535");
    DoNumTest("foo %hi", 0xFFFF, "foo -1");
    DoNumTest("foo %Li", pos, "foo 42");
    DoI64Test("foo %I64i", l, "42", "foo 42");
    DoNumTest("foo %3i", pos, "foo  42");
    DoNumTest("foo %-3i", pos, "foo 42 ");
    DoNumTest("foo %.1i", pos, "foo 42");
    DoNumTest("foo %.3i", pos, "foo 042");
    DoNumTest("foo %03i", pos, "foo 042");
    DoNumTest("foo %#i", pos, "foo 42");
    DoNumTest("foo %+i", pos, "foo +42");
    DoNumTest("foo % i", pos, "foo  42");
    DoNumTest("foo %+i", neg, "foo -42");
    DoNumTest("foo % i", neg, "foo -42");

    PAL_Terminate();
    return PASS;
}

