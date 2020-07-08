// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test5.c
**
** Purpose: Tests sscanf_s with integer numbers
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf_s.h"

int __cdecl main(int argc, char *argv[])
{
    int n65535 = 65535; /* Walkaround compiler strictness */

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoNumTest("1234d", "%i", 1234);
    DoNumTest("1234d", "%2i", 12);
    DoNumTest("-1", "%i", -1);
    DoNumTest("0x1234", "%i", 0x1234);
    DoNumTest("012", "%i", 10);
    DoShortNumTest("-1", "%hi", n65535);
    DoShortNumTest("65536", "%hi", 0);
    DoNumTest("-1", "%li", -1);
    DoNumTest("65536", "%li", 65536);
    DoNumTest("-1", "%Li", -1);
    DoNumTest("65536", "%Li", 65536);
    DoI64NumTest("4294967296", "%I64i", I64(4294967296));

    PAL_Terminate();
    return PASS;
}
