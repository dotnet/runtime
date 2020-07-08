// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test5.c
**
** Purpose:  Tests swscanf with integer numbers
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

    DoNumTest(convert("1234d"), convert("%i"), 1234);
    DoNumTest(convert("1234d"), convert("%2i"), 12);
    DoNumTest(convert("-1"), convert("%i"), -1);
    DoNumTest(convert("0x1234"), convert("%i"), 0x1234);
    DoNumTest(convert("012"), convert("%i"), 10);
    DoShortNumTest(convert("-1"), convert("%hi"), n65535);
    DoShortNumTest(convert("65536"), convert("%hi"), 0);
    DoNumTest(convert("-1"), convert("%li"), -1);
    DoNumTest(convert("65536"), convert("%li"), 65536);
    DoNumTest(convert("-1"), convert("%Li"), -1);
    DoNumTest(convert("65536"), convert("%Li"), 65536);
    DoI64NumTest(convert("4294967296"), convert("%I64i"), I64(4294967296));

    PAL_Terminate();
    return PASS;
}
