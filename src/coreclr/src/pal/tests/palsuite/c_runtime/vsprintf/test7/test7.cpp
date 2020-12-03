// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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


PALTEST(c_runtime_vsprintf_test7_paltest_vsprintf_test7, "c_runtime/vsprintf/test7/paltest_vsprintf_test7")
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
