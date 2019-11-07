// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    int n65535 = 65535; /* Walkaround compiler strictness */

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoNumTest(convert("1234d"), convert("%o"), 668);
    DoNumTest(convert("1234d"), convert("%2o"), 10);
    DoNumTest(convert("-1"), convert("%o"), -1);
    DoNumTest(convert("0x1234"), convert("%o"), 0);
    DoNumTest(convert("012"), convert("%o"), 10);
    DoShortNumTest(convert("-1"), convert("%ho"), n65535);
    DoShortNumTest(convert("200000"), convert("%ho"), 0);
    DoNumTest(convert("-1"), convert("%lo"), -1);
    DoNumTest(convert("200000"), convert("%lo"), 65536);
    DoNumTest(convert("-1"), convert("%Lo"), -1);
    DoNumTest(convert("200000"), convert("%Lo"), 65536);
    DoI64NumTest(convert("40000000000"), convert("%I64o"), I64(4294967296));

    PAL_Terminate();
    return PASS;
}
