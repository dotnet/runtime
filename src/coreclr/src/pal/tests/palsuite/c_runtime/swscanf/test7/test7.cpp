// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test7.c
**
** Purpose: Test #6 for the swscanf function
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

    DoNumTest(convert("1234d"), convert("%x"), 0x1234d);
    DoNumTest(convert("1234d"), convert("%2x"), 0x12);
    DoNumTest(convert("-1"), convert("%x"), -1);
    DoNumTest(convert("0x1234"), convert("%x"), 0x1234);
    DoNumTest(convert("012"), convert("%x"), 0x12);
    DoShortNumTest(convert("-1"), convert("%hx"), n65535);
    DoShortNumTest(convert("10000"), convert("%hx"), 0);
    DoNumTest(convert("-1"), convert("%lx"), -1);
    DoNumTest(convert("10000"), convert("%lx"), 65536);
    DoNumTest(convert("-1"), convert("%Lx"), -1);
    DoNumTest(convert("10000"), convert("%Lx"), 65536);
    DoI64NumTest(convert("100000000"), convert("%I64x"), I64(4294967296));

    PAL_Terminate();
    return PASS;
}
