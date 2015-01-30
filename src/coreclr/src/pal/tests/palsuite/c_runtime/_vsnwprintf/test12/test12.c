//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:    test12.c
**
** Purpose:   Test #12 for the _vsnwprintf function.
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
    int pos = 0x1234ab;
    INT64 l = I64(0x1234567887654321);

    if (PAL_Initialize(argc, argv) != 0)
    {
        return(FAIL);
    }

    DoNumTest(convert("foo %x"), pos, convert("foo 1234ab"));
    DoNumTest(convert("foo %lx"), pos, convert("foo 1234ab"));
    DoNumTest(convert("foo %hx"), pos, convert("foo 34ab"));
    DoNumTest(convert("foo %Lx"), pos, convert("foo 1234ab"));
    DoI64NumTest(convert("foo %I64x"), l, "0x1234567887654321",
        convert("foo 1234567887654321"));
    DoNumTest(convert("foo %7x"), pos, convert("foo  1234ab"));
    DoNumTest(convert("foo %-7x"), pos, convert("foo 1234ab "));
    DoNumTest(convert("foo %.1x"), pos, convert("foo 1234ab"));
    DoNumTest(convert("foo %.7x"), pos, convert("foo 01234ab"));
    DoNumTest(convert("foo %07x"), pos, convert("foo 01234ab"));
    DoNumTest(convert("foo %#x"), pos, convert("foo 0x1234ab"));
    DoNumTest(convert("foo %+x"), pos, convert("foo 1234ab"));
    DoNumTest(convert("foo % x"), pos, convert("foo 1234ab"));
    DoNumTest(convert("foo %+x"), neg, convert("foo ffffffd6"));
    DoNumTest(convert("foo % x"), neg, convert("foo ffffffd6"));

    PAL_Terminate();
    return PASS;
}
