// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test7.c
**
** Purpose: Tests sprintf_s with wide characters
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../_snprintf_s.h"
/*
 * Notes: memcmp is used, as is strlen.
 */


PALTEST(c_runtime__snprintf_s_test7_paltest_snprintf_test7, "c_runtime/_snprintf_s/test7/paltest_snprintf_test7")
{
    WCHAR wb = (WCHAR) 'b';
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoWCharTest("foo %C", wb, "foo b");
    DoWCharTest("foo %hC", wb, "foo b");
    DoCharTest("foo %lC", 'c', "foo c");
    DoWCharTest("foo %LC", wb, "foo b");
    DoWCharTest("foo %I64C", wb, "foo b");
    DoWCharTest("foo %5C", wb, "foo     b");
    DoWCharTest("foo %.0C", wb, "foo b");
    DoWCharTest("foo %-5C", wb, "foo b    ");
    DoWCharTest("foo %05C", wb, "foo 0000b");
    DoWCharTest("foo % C", wb, "foo b");
    DoWCharTest("foo %#C", wb, "foo b");

    PAL_Terminate();
    return PASS;
}
