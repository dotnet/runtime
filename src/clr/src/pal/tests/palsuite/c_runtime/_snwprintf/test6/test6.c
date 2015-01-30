//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test6.c
**
** Purpose: Tests _snwprintf with characters
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../_snwprintf.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */

int __cdecl main(int argc, char *argv[])
{
    WCHAR wc = (WCHAR) 'c';
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    DoWCharTest(convert("foo %c"), wc, convert("foo c"));
    DoCharTest(convert("foo %hc"), 'b', convert("foo b"));
    DoWCharTest(convert("foo %lc"), wc, convert("foo c"));
    DoWCharTest(convert("foo %Lc"), wc, convert("foo c"));
    DoWCharTest(convert("foo %I64c"), wc, convert("foo c"));
    DoWCharTest(convert("foo %5c"), wc, convert("foo     c"));
    DoWCharTest(convert("foo %.0c"), wc, convert("foo c"));
    DoWCharTest(convert("foo %-5c"), wc, convert("foo c    "));
    DoWCharTest(convert("foo %05c"), wc, convert("foo 0000c"));
    DoWCharTest(convert("foo % c"), wc, convert("foo c"));
    DoWCharTest(convert("foo %#c"), wc, convert("foo c"));

    PAL_Terminate();
    return PASS;
}
