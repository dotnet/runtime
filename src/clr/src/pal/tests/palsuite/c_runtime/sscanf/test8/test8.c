//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test8.c
**
** Purpose:Tests sscanf with unsigned number 
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

    DoNumTest("1234d", "%u", 1234);
    DoNumTest("1234d", "%2u", 12);
    DoNumTest("-1", "%u", -1);
    DoNumTest("0x1234", "%u", 0);
    DoNumTest("012", "%u", 12);
    DoShortNumTest("-1", "%hu", 65535);
    DoShortNumTest("65536", "%hu", 0);
    DoNumTest("-1", "%lu", -1);
    DoNumTest("65536", "%lu", 65536);
    DoNumTest("-1", "%Lu", -1);
    DoNumTest("65536", "%Lu", 65536);
    DoI64NumTest("4294967296", "%I64u", I64(4294967296));

    PAL_Terminate();
    return PASS;
}
