// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test6.c
**
** Purpose: Tests swprintf with character
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
    WCHAR wb = (WCHAR) 'b';
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoWCharTest(convert("foo %c"), wb, convert("foo b"));
    DoCharTest(convert("foo %hc"), 'c', convert("foo c"));
    DoWCharTest(convert("foo %lc"), wb, convert("foo b"));
    DoWCharTest(convert("foo %Lc"), wb, convert("foo b"));
    DoWCharTest(convert("foo %I64c"), wb, convert("foo b"));
    DoWCharTest(convert("foo %5c"), wb, convert("foo     b"));
    DoWCharTest(convert("foo %.0c"), wb, convert("foo b"));
    DoWCharTest(convert("foo %-5c"), wb, convert("foo b    "));
    DoWCharTest(convert("foo %05c"), wb, convert("foo 0000b"));
    DoWCharTest(convert("foo % c"), wb, convert("foo b"));
    DoWCharTest(convert("foo %#c"), wb, convert("foo b"));

    PAL_Terminate();
    return PASS;
}
