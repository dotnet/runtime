// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test4.c
**
** Purpose:Tests swscanf with decimal numbers
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

    DoNumTest(convert("1234d"), convert("%d"), 1234);
    DoNumTest(convert("1234d"), convert("%2d"), 12);
    DoNumTest(convert("-1"), convert("%d"), -1);
    DoNumTest(convert("0x1234"), convert("%d"), 0);
    DoNumTest(convert("012"), convert("%d"), 12);
    DoShortNumTest(convert("-1"), convert("%hd"), n65535);
    DoShortNumTest(convert("65536"), convert("%hd"), 0);
    DoNumTest(convert("-1"), convert("%ld"), -1);
    DoNumTest(convert("65536"), convert("%ld"), 65536);
    DoNumTest(convert("-1"), convert("%Ld"), -1);
    DoNumTest(convert("65536"), convert("%Ld"), 65536);
    DoI64NumTest(convert("4294967296"), convert("%I64d"), I64(4294967296));

    PAL_Terminate();
    return PASS;
}
