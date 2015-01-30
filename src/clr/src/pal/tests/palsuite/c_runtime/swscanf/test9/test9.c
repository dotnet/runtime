//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test9.c
**
** Purpose: Tests swscanf with characters
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

    DoWCharTest(convert("1234"), convert("%c"), convert("1"), 1);
    DoWCharTest(convert("1234"), convert("%c"), convert("1"), 1);
    DoWCharTest(convert("abc"), convert("%2c"), convert("ab"), 2);
    DoWCharTest(convert(" ab"), convert("%c"), convert(" "), 1);
    DoCharTest(convert("ab"), convert("%hc"), "a", 1);
    DoWCharTest(convert("ab"), convert("%lc"), convert("a"), 1);
    DoWCharTest(convert("ab"), convert("%Lc"), convert("a"), 1);
    DoWCharTest(convert("ab"), convert("%I64c"), convert("a"), 1);

    PAL_Terminate();
    return PASS;
}
