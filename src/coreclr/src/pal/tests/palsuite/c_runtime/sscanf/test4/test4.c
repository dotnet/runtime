//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test4.c
**
** Purpose: Tests sscanf with decimal numbers
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

    DoNumTest("1234d", "%d", 1234);
    DoNumTest("1234d", "%2d", 12);
    DoNumTest("-1", "%d", -1);
    DoNumTest("0x1234", "%d", 0);
    DoNumTest("012", "%d", 12);
    DoShortNumTest("-1", "%hd", 65535);
    DoShortNumTest("65536", "%hd", 0);
    DoNumTest("-1", "%ld", -1);
    DoNumTest("65536", "%ld", 65536);
    DoNumTest("-1", "%Ld", -1);
    DoNumTest("65536", "%Ld", 65536);
    DoI64NumTest("4294967296", "%I64d", I64(4294967296));
    
    PAL_Terminate();
    return PASS;
}
