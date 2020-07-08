// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:      test7.c
**
** Purpose:     Tests the wide char specifier (%C).
**              This test is modeled after the sprintf series.
**
**
**==========================================================================*/

#include <palsuite.h>
#include "../fwprintf.h"

/* 
 * Depends on memcmp, strlen, fopen, fseek and fgets.
 */

int __cdecl main(int argc, char *argv[])
{
    WCHAR wb = (WCHAR) 'b';
    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    DoCharTest(convert("foo %C"), 'c', "foo c");
    DoWCharTest(convert("foo %hc"), wb, "foo b");
    DoCharTest(convert("foo %lC"), 'c', "foo c");
    DoCharTest(convert("foo %LC"), 'c', "foo c");
    DoCharTest(convert("foo %I64C"), 'c', "foo c");
    DoCharTest(convert("foo %5C"), 'c', "foo     c");
    DoCharTest(convert("foo %.0C"), 'c', "foo c");
    DoCharTest(convert("foo %-5C"), 'c', "foo c    ");
    DoCharTest(convert("foo %05C"), 'c', "foo 0000c");
    DoCharTest(convert("foo % C"), 'c', "foo c");
    DoCharTest(convert("foo %#C"), 'c', "foo c");

    PAL_Terminate();
    return PASS;
}

