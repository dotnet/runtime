// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:      test6.c
**
** Purpose:     Tests the char specifier (%c).
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
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoWCharTest(convert("foo %c"), wb, "foo b");
    DoCharTest(convert("foo %hc"), 'c', "foo c");
    DoWCharTest(convert("foo %lc"), wb, "foo b");
    DoWCharTest(convert("foo %Lc"), wb, "foo b");
    DoWCharTest(convert("foo %I64c"), wb, "foo b");
    DoWCharTest(convert("foo %5c"), wb, "foo     b");
    DoWCharTest(convert("foo %.0c"), wb, "foo b");
    DoWCharTest(convert("foo %-5c"), wb, "foo b    ");
    DoWCharTest(convert("foo %05c"), wb, "foo 0000b");
    DoWCharTest(convert("foo % c"), wb, "foo b");
    DoWCharTest(convert("foo %#c"), wb, "foo b");
    
    PAL_Terminate();
    return PASS;
}



