//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test6.c
**
** Purpose:Tests swscanf with octal numbers
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../swscanf.h"


int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoNumTest(convert("1234d"), convert("%o"), 668);
    DoNumTest(convert("1234d"), convert("%2o"), 10);
    DoNumTest(convert("-1"), convert("%o"), -1);
    DoNumTest(convert("0x1234"), convert("%o"), 0);
    DoNumTest(convert("012"), convert("%o"), 10);
    DoShortNumTest(convert("-1"), convert("%ho"), 65535);
    DoShortNumTest(convert("200000"), convert("%ho"), 0);
    DoNumTest(convert("-1"), convert("%lo"), -1);
    DoNumTest(convert("200000"), convert("%lo"), 65536);
    DoNumTest(convert("-1"), convert("%Lo"), -1);
    DoNumTest(convert("200000"), convert("%Lo"), 65536);
    DoI64NumTest(convert("40000000000"), convert("%I64o"), I64(4294967296));

    PAL_Terminate();
    return PASS;
}
