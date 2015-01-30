//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test7.c
**
** Purpose: Test #7 for the vprintf function. Tests the wide char
**          specifier (%C).
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../vprintf.h"



int __cdecl main(int argc, char *argv[])
{
    WCHAR wb = (WCHAR) 'b';
    
    if (PAL_Initialize(argc, argv))
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

