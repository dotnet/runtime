//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test6.c
**
** Purpose: Tests _snprintf with characters
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../_snprintf.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

int __cdecl main(int argc, char *argv[])
{
    WCHAR wc = (WCHAR) 'c';
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
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
