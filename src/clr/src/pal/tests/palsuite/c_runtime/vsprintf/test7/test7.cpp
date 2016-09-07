// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:    test7.c
**
** Purpose:   Test #7 for the vsprintf function.
**
**
**===================================================================*/
    
#include <palsuite.h>
#include "../vsprintf.h"
/*
 * Notes: memcmp is used, as is strlen.
 */


int __cdecl main(int argc, char *argv[])
{
    WCHAR wb = (WCHAR) 'b';
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoWCharTest("foo %c", wb, "foo b");
    DoWCharTest("foo %hc", wb, "foo b");
    DoCharTest("foo %lc", 'c', "foo c");
    DoWCharTest("foo %Lc", wb, "foo b");
    DoWCharTest("foo %I64c", wb, "foo b");
    DoWCharTest("foo %5c", wb, "foo     b");
    DoWCharTest("foo %.0c", wb, "foo b");
    DoWCharTest("foo %-5c", wb, "foo b    ");
    DoWCharTest("foo %05c", wb, "foo 0000b");
    DoWCharTest("foo % c", wb, "foo b");
    DoWCharTest("foo %#c", wb, "foo b");

    PAL_Terminate();
    return PASS;
}
