//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:    test11.c
**
** Purpose:   Test #11 for the _vsnwprintf function.
**
**
**===================================================================*/
 
#include <palsuite.h>
#include "../_vsnwprintf.h"

/* memcmp is used to verify the results, so this test is dependent on it. */
/* ditto with wcslen */

int __cdecl main(int argc, char *argv[])
{
    int neg = -42;
    int pos = 42;
    INT64 l = 42;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoNumTest(convert("foo %u"), pos, convert("foo 42"));
    DoNumTest(convert("foo %lu"), 0xFFFF, convert("foo 65535"));
    DoNumTest(convert("foo %hu"), 0xFFFF, convert("foo 65535"));
    DoNumTest(convert("foo %Lu"), pos, convert("foo 42"));
    DoI64NumTest(convert("foo %I64u"), l, "42", convert("foo 42"));
    DoNumTest(convert("foo %3u"), pos, convert("foo  42"));
    DoNumTest(convert("foo %-3u"), pos, convert("foo 42 "));
    DoNumTest(convert("foo %.1u"), pos, convert("foo 42"));
    DoNumTest(convert("foo %.3u"), pos, convert("foo 042"));
    DoNumTest(convert("foo %03u"), pos, convert("foo 042"));
    DoNumTest(convert("foo %#u"), pos, convert("foo 42"));
    DoNumTest(convert("foo %+u"), pos, convert("foo 42"));
    DoNumTest(convert("foo % u"), pos, convert("foo 42"));
    DoNumTest(convert("foo %+u"), neg, convert("foo 4294967254"));
    DoNumTest(convert("foo % u"), neg, convert("foo 4294967254"));


    PAL_Terminate();
    return PASS;
}
