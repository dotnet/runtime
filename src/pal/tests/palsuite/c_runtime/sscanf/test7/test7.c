//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test7.c
**
** Purpose:  Tests sscanf with hex numbers (lowercase)
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf.h"

int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoNumTest("1234i", "%x", 0x1234);
    DoNumTest("1234i", "%2x", 0x12);
    DoNumTest("-1", "%x", -1);
    DoNumTest("0x1234", "%x", 0x1234);
    DoNumTest("012", "%x", 0x12);
    DoShortNumTest("-1", "%hx", 65535);
    DoShortNumTest("10000", "%hx", 0);
    DoNumTest("-1", "%lx", -1);
    DoNumTest("10000", "%lx", 65536);
    DoNumTest("-1", "%Lx", -1);
    DoNumTest("10000", "%Lx", 65536);
    DoI64NumTest("100000000", "%I64x", I64(4294967296));

    PAL_Terminate();
    return PASS;
}
