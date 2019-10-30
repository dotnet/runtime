// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test7.c
**
** Purpose: Tests swprintf with wide characters
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


    DoCharTest(convert("foo %C"), 'c', convert("foo c"));
    DoWCharTest(convert("foo %hc"), wb, convert("foo b"));
    DoCharTest(convert("foo %lC"), 'c', convert("foo c"));
    DoCharTest(convert("foo %LC"), 'c', convert("foo c"));
    DoCharTest(convert("foo %I64C"), 'c', convert("foo c"));
    DoCharTest(convert("foo %5C"), 'c', convert("foo     c"));
    DoCharTest(convert("foo %.0C"), 'c', convert("foo c"));
    DoCharTest(convert("foo %-5C"), 'c', convert("foo c    "));
    DoCharTest(convert("foo %05C"), 'c', convert("foo 0000c"));
    DoCharTest(convert("foo % C"), 'c', convert("foo c"));
    DoCharTest(convert("foo %#C"), 'c', convert("foo c"));

    PAL_Terminate();
    return PASS;
}
