// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:    test6.c
**
** Purpose:   Test #6 for the _vsnwprintf_s function.
**
**
**===================================================================*/

#include <palsuite.h>
#include "../_vsnwprintf_s.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */

int __cdecl main(int argc, char *argv[])
{
    WCHAR wc = (WCHAR) 'c';
    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
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
