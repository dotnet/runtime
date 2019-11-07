// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test8.c
**
** Purpose: Tests swscanf with unsigned numbers
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swscanf.h"


int __cdecl main(int argc, char *argv[])
{
    int n65535 = 65535; /* Walkaround compiler strictness */

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoNumTest(convert("1234d"), convert("%u"), 1234);
    DoNumTest(convert("1234d"), convert("%2u"), 12);
    DoNumTest(convert("-1"), convert("%u"), -1);
    DoNumTest(convert("0x1234"), convert("%u"), 0);
    DoNumTest(convert("012"), convert("%u"), 12);
    DoShortNumTest(convert("-1"), convert("%hu"), n65535);
    DoShortNumTest(convert("65536"), convert("%hu"), 0);
    DoNumTest(convert("-1"), convert("%lu"), -1);
    DoNumTest(convert("65536"), convert("%lu"), 65536);
    DoNumTest(convert("-1"), convert("%Lu"), -1);
    DoNumTest(convert("65536"), convert("%Lu"), 65536);
    DoI64NumTest(convert("4294967296"), convert("%I64u"), I64(4294967296));

    PAL_Terminate();
    return PASS;
}
