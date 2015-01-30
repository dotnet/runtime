//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test5.c
**
** Purpose:Tests swprintf with the count specifier
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swprintf.h"

/*
 * Uses memcmp & wcslen
 */

int __cdecl main(int argc, char *argv[])
{
    WCHAR *longStr;
    WCHAR *longResult;

    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    longStr = 
        convert("really-long-string-that-just-keeps-going-on-and-on-and-on.."
        "..................useless-filler.................................."
        "..................useless-filler.................................."
        "..................useless-filler.................................."
        "%n bar");
    longResult = 
        convert("really-long-string-that-just-keeps-going-on-and-on-and-on.."
        "..................useless-filler.................................."
        "..................useless-filler.................................."
        "..................useless-filler.................................."
        " bar");

    DoCountTest(convert("foo %n bar"), 4, convert("foo  bar"));
    DoCountTest(longStr, 257, longResult);
    DoCountTest(convert("fo%n bar"), 2, convert("fo bar"));
    DoCountTest(convert("%n"), 0, convert(""));
    DoCountTest(convert("foo %#n bar"), 4, convert("foo  bar"));
    DoCountTest(convert("foo % n bar"), 4, convert("foo  bar"));
    DoCountTest(convert("foo %+n bar"), 4, convert("foo  bar"));
    DoCountTest(convert("foo %-n bar"), 4, convert("foo  bar"));
    DoCountTest(convert("foo %0n bar"), 4, convert("foo  bar"));
    DoShortCountTest(convert("foo %hn bar"), 4, convert("foo  bar"));
    DoCountTest(convert("foo %ln bar"), 4, convert("foo  bar"));
    DoCountTest(convert("foo %Ln bar"), 4, convert("foo  bar"));
    DoCountTest(convert("foo %I64n bar"), 4, convert("foo  bar"));
    DoCountTest(convert("foo %20.3n bar"), 4, convert("foo  bar"));

    PAL_Terminate();

    free(longStr);
    free(longResult);

    return PASS;
}
