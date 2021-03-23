// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test6.c
**
** Purpose:   Test #6 for the vsprintf function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../vsprintf.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

PALTEST(c_runtime_vsprintf_test6_paltest_vsprintf_test6, "c_runtime/vsprintf/test6/paltest_vsprintf_test6")
{
    WCHAR wc = (WCHAR) 'c';
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoCharTest("foo %c", 'b', "foo b");
    DoCharTest("foo %hc", 'b', "foo b");
    DoWCharTest("foo %lc", wc, "foo c");
    DoCharTest("foo %Lc", 'b', "foo b");
    DoCharTest("foo %I64c", 'b', "foo b");
    DoCharTest("foo %5c", 'b', "foo     b");
    DoCharTest("foo %.0c", 'b', "foo b");
    DoCharTest("foo %-5c", 'b', "foo b    ");
    DoCharTest("foo %05c", 'b', "foo 0000b");
    DoCharTest("foo % c", 'b', "foo b");
    DoCharTest("foo %#c", 'b', "foo b");
    
    PAL_Terminate();
    return PASS;
}
